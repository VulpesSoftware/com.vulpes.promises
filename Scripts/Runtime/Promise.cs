using System;
using System.Collections.Generic;
using System.Linq;

namespace Vulpes.Promises
{
    public struct ResolveHandler
    {
        public Action callback;
        public IRejectable rejectable;
    }

    public struct RejectHandler
    {
        public Action<Exception> callback;
        public IRejectable rejectable;
    }

    public struct ProgressHandler
    {
        public Action<float> callback;
        public IRejectable rejectable;
    }

    /// <summary>
    /// Implements a non-generic C# promise, this is a promise that simply resolves without delivering a value.
    /// https://developer.mozilla.org/en/docs/Web/JavaScript/Reference/Global_Objects/Promise
    /// </summary>
    public class Promise : AbstractPromise, IPromise
    {
        private Exception rejectionException;
        private List<RejectHandler> rejectHandlers;
        private List<ResolveHandler> resolveHandlers;
        private List<ProgressHandler> progressHandlers;

        private static readonly IPromise resolvedPromise = new Promise(PromiseState.Resolved);

        public Promise() : base()
        {
            if (enablePromiseTracking)
            {
                pendingPromises.Add(this);
            }
        }

        public Promise(Action<Action, Action<Exception>> resolver) : base()
        {
            if (enablePromiseTracking)
            {
                pendingPromises.Add(this);
            }
            try
            {
                resolver(Resolve, Reject);
            }
            catch (Exception exception)
            {
                Reject(exception);
            }
        }

        private Promise(PromiseState initialState) : base()
            => State = initialState;

        private void AddRejectHandler(Action<Exception> onRejected, IRejectable rejectable)
        {
            if (rejectHandlers == null)
            {
                rejectHandlers = new();
            }
            rejectHandlers.Add(new()
            {
                callback = onRejected,
                rejectable = rejectable
            });
        }

        private void AddResolveHandler(Action onResolved, IRejectable rejectable)
        {
            if (resolveHandlers == null)
            {
                resolveHandlers = new();
            }
            resolveHandlers.Add(new()
            {
                callback = onResolved,
                rejectable = rejectable
            });
        }

        private void AddProgressHandler(Action<float> onProgress, IRejectable rejectable)
        {
            if (progressHandlers == null)
            {
                progressHandlers = new();
            }
            progressHandlers.Add(new() 
            { 
                callback = onProgress, 
                rejectable = rejectable 
            });
        }

        private void InvokeRejectHandler(Action<Exception> callback, IRejectable rejectable, Exception value)
        {
            try
            {
                callback(value);
            }
            catch (Exception exception)
            {
                rejectable.Reject(exception);
            }
        }

        private void InvokeResolveHandler(Action callback, IRejectable rejectable)
        {
            try
            {
                callback();
            }
            catch (Exception exception)
            {
                rejectable.Reject(exception);
            }
        }

        private void InvokeProgressHandler(Action<float> callback, IRejectable rejectable, in float progress)
        {
            try
            {
                callback(progress);
            }
            catch (Exception exception)
            {
                rejectable.Reject(exception);
            }
        }

        private void ClearHandlers()
        {
            rejectHandlers = null;
            resolveHandlers = null;
            progressHandlers = null;
        }

        private void InvokeRejectHandlers(Exception exception)
        {
            if (rejectHandlers != null)
            {
                for (int i = 0, max = rejectHandlers.Count; i < max; i++)
                {
                    InvokeRejectHandler(rejectHandlers[i].callback, rejectHandlers[i].rejectable, exception);
                }
            }
            ClearHandlers();
        }

        private void InvokeResolveHandlers()
        {
            if (resolveHandlers != null)
            {
                for (int i = 0, max = resolveHandlers.Count; i < max; i++)
                {
                    InvokeResolveHandler(resolveHandlers[i].callback, resolveHandlers[i].rejectable);
                }
            }
            ClearHandlers();
        }

        private void InvokeProgressHandlers(in float progress)
        {
            if (progressHandlers != null)
            {
                for (int i = 0, max = progressHandlers.Count; i < max; i++)
                {
                    InvokeProgressHandler(progressHandlers[i].callback, progressHandlers[i].rejectable, progress);
                }
            }
        }

        public void Reject(Exception exception)
        {
            if (!IsPending)
            {
                throw new PromiseStateException($"Attempt to reject a promise that is already in state: {State}, a promise can only be rejected when it is still in state: {PromiseState.Pending}.");
            }

            rejectionException = exception;
            State = PromiseState.Rejected;

            if (enablePromiseTracking)
            {
                pendingPromises.Remove(this);
            }

            InvokeRejectHandlers(exception);
        }

        public void Resolve()
        {
            if (!IsPending)
            {
                throw new PromiseStateException($"Attempt to resolve a promise that is already in state: {State}, a promise can only be resolved when it is still in state: {PromiseState.Pending}.");
            }

            State = PromiseState.Resolved;

            if (enablePromiseTracking)
            {
                pendingPromises.Remove(this);
            }

            InvokeResolveHandlers();
        }

        public void ReportProgress(in float progress)
        {
            if (!IsPending)
            {
                throw new PromiseStateException($"Attempt to report progress on a promise that is already in state: {State}, a promise can only report progress when it is still in state: {PromiseState.Pending}.");
            }

            InvokeProgressHandlers(progress);
        }

        public void Done(Action onResolved, Action<Exception> onRejected)
            => Then(onResolved, onRejected).Catch(exception => PropagateUnhandledException(this, exception));

        public void Done(Action onResolved)
            => Then(onResolved).Catch(exception => PropagateUnhandledException(this, exception));

        public void Done()
        {
            if (IsResolved)
            {
                return;
            }

            Catch(exception => PropagateUnhandledException(this, exception));
        }

        public IPromise WithName(in string name)
        {
            Name = name;
            return this;
        }

        public IPromise Catch(Action<Exception> onRejected)
        {
            if (IsResolved)
            {
                return this;
            }

            IPromise resultPromise = new Promise();
            resultPromise.WithName(Name);

            void resolveHandler() 
                => resultPromise.Resolve();

            void rejectHandler(Exception exception)
            {
                try
                {
                    onRejected(exception);
                    resultPromise.Resolve();
                }
                catch (Exception callbackException)
                {
                    resultPromise.Reject(callbackException);
                }
            }

            ActionHandlers(resultPromise, resolveHandler, rejectHandler);
            ProgressHandlers(resultPromise, progress => resultPromise.ReportProgress(progress));

            return resultPromise;
        }

        public IPromise<TConvertedType> Then<TConvertedType>(Func<IPromise<TConvertedType>> onResolved) 
            => Then(onResolved, null, null);

        public IPromise Then(Func<IPromise> onResolved) 
            => Then(onResolved, null, null);

        public IPromise Then(Action onResolved) 
            => Then(onResolved, null, null);

        public IPromise<TConvertedType> Then<TConvertedType>(Func<IPromise<TConvertedType>> onResolved, Func<Exception, IPromise<TConvertedType>> onRejected) 
            => Then(onResolved, onRejected, null);

        public IPromise Then(Func<IPromise> onResolved, Action<Exception> onRejected) 
            => Then(onResolved, onRejected, null);

        public IPromise Then(Action onResolved, Action<Exception> onRejected) 
            => Then(onResolved, onRejected, null);

        public IPromise<TConvertedType> Then<TConvertedType>(Func<IPromise<TConvertedType>> onResolved, Func<Exception, IPromise<TConvertedType>> onRejected, Action<float> onProgress)
        {
            if (IsResolved)
            {
                try
                {
                    return onResolved();
                }
                catch (Exception ex)
                {
                    return Promise<TConvertedType>.Rejected(ex);
                }
            }

            IPromise<TConvertedType> resultPromise = new Promise<TConvertedType>();
            resultPromise.WithName(Name);

            void ResolveHandler()
            {
                onResolved()
                    .Progress(progress => resultPromise.ReportProgress(progress))
                    .Then(
                        chainedValue => resultPromise.Resolve(chainedValue),
                        exception => resultPromise.Reject(exception)
                    );
            }

            void RejectHandler(Exception exception)
            {
                if (onRejected == null)
                {
                    resultPromise.Reject(exception);
                    return;
                }

                try
                {
                    onRejected(exception)
                        .Then(
                            chainedValue => resultPromise.Resolve(chainedValue),
                            callbackException => resultPromise.Reject(callbackException)
                        );
                }
                catch (Exception callbackEx)
                {
                    resultPromise.Reject(callbackEx);
                }
            }

            ActionHandlers(resultPromise, ResolveHandler, RejectHandler);
            if (onProgress != null)
            {
                ProgressHandlers(this, onProgress);
            }

            return resultPromise;
        }

        public IPromise Then(Func<IPromise> onResolved, Action<Exception> onRejected, Action<float> onProgress)
        {
            if (IsResolved)
            {
                try
                {
                    return onResolved();
                }
                catch (Exception exception)
                {
                    return Rejected(exception);
                }
            }

            IPromise resultPromise = new Promise();
            resultPromise.WithName(Name);

            void ResolveHandler()
            {
                if (onResolved != null)
                {
                    onResolved()
                        .Progress(progress => resultPromise.ReportProgress(progress))
                        .Then(() => resultPromise.Resolve(), exception => resultPromise.Reject(exception));
                } else
                {
                    resultPromise.Resolve();
                }
            }

            void RejectHandler(Exception exception)
            {
                onRejected?.Invoke(exception);
                resultPromise.Reject(exception);
            }

            ActionHandlers(resultPromise, ResolveHandler, RejectHandler);
            if (onProgress != null)
            {
                ProgressHandlers(this, onProgress);
            }

            return resultPromise;
        }

        public IPromise Then(Action onResolved, Action<Exception> onRejected, Action<float> onProgress)
        {
            if (IsResolved)
            {
                try
                {
                    onResolved();
                    return this;
                }
                catch (Exception exception)
                {
                    return Rejected(exception);
                }
            }

            IPromise resultPromise = new Promise();
            resultPromise.WithName(Name);

            void ResolveHandler()
            {
                onResolved?.Invoke();
                resultPromise.Resolve();
            }

            void RejectHandler(Exception exception)
            {
                if (onRejected != null)
                {
                    onRejected(exception);
                    resultPromise.Resolve();
                    return;
                }
                resultPromise.Reject(exception);
            }

            ActionHandlers(resultPromise, ResolveHandler, RejectHandler);
            if (onProgress != null)
            {
                ProgressHandlers(this, onProgress);
            }

            return resultPromise;
        }

        private void ActionHandlers(IRejectable resultPromise, Action resolveHandler, Action<Exception> rejectHandler)
        {
            if (IsResolved)
            {
                InvokeResolveHandler(resolveHandler, resultPromise);
            } else if (IsRejected)
            {
                InvokeRejectHandler(rejectHandler, resultPromise, rejectionException);
            } else
            {
                AddResolveHandler(resolveHandler, resultPromise);
                AddRejectHandler(rejectHandler, resultPromise);
            }
        }

        private void ProgressHandlers(IRejectable resultPromise, Action<float> progressHandler)
        {
            if (IsPending)
            {
                AddProgressHandler(progressHandler, resultPromise);
            }
        }

        public IPromise ThenAll(Func<IEnumerable<IPromise>> chain)
            => Then(() => All(chain()));

        public IPromise<IEnumerable<TConvertedType>> ThenAll<TConvertedType>(Func<IEnumerable<IPromise<TConvertedType>>> chain)
            => Then(() => Promise<TConvertedType>.All(chain()));

        public static IPromise All(params IPromise[] promises)
            => All((IEnumerable<IPromise>)promises);

        public static IPromise All(IEnumerable<IPromise> promises)
        {
            IPromise[] promisesArray = promises.ToArray();
            if (promisesArray.Length == 0)
            {
                return Resolved();
            }

            int remainingCount = promisesArray.Length;
            IPromise resultPromise = new Promise();
            resultPromise.WithName("All");
            float[] progress = new float[remainingCount];

            promisesArray.Each((promise, index) =>
            {
                promise
                    .Progress(v =>
                    {
                        progress[index] = v;
                        if (resultPromise.IsPending)
                        {
                            resultPromise.ReportProgress(progress.Average());
                        }
                    })
                    .Then(() =>
                    {
                        progress[index] = 1.0f;
                        remainingCount--;
                        if (remainingCount <= 0 && resultPromise.IsPending)
                        {
                            resultPromise.Resolve();
                        }
                    })
                    .Catch(ex =>
                    {
                        if (resultPromise.IsPending)
                        {
                            resultPromise.Reject(ex);
                        }
                    })
                    .Done();
            });

            return resultPromise;
        }

        public IPromise ThenSequence(Func<IEnumerable<Func<IPromise>>> chain)
            => Then(() => Sequence(chain()));

        public static IPromise Sequence(params Func<IPromise>[] fns)
            => Sequence((IEnumerable<Func<IPromise>>)fns);

        public static IPromise Sequence(IEnumerable<Func<IPromise>> fns)
        {
            IPromise promise = new Promise();

            int count = 0;

            fns.Aggregate(
                Resolved(),
                (prevPromise, fn) =>
                {
                    int itemSequence = count;
                    count++;

                    return prevPromise
                            .Then(() =>
                            {
                                float sliceLength = 1.0f / count;
                                promise.ReportProgress(sliceLength * itemSequence);
                                return fn();
                            })
                            .Progress(progress =>
                            {
                                float sliceLength = 1.0f / count;
                                promise.ReportProgress(sliceLength * (progress + itemSequence));
                            });
                }
            )
            .Then(promise.Resolve)
            .Catch(promise.Reject);

            return promise;
        }

        public IPromise ThenRace(Func<IEnumerable<IPromise>> chain)
            => Then(() => Race(chain()));

        public IPromise<TConvertedType> ThenRace<TConvertedType>(Func<IEnumerable<IPromise<TConvertedType>>> chain)
            => Then(() => Promise<TConvertedType>.Race(chain()));

        public static IPromise Race(params IPromise[] promises)
            => Race((IEnumerable<IPromise>)promises);

        public static IPromise Race(IEnumerable<IPromise> promises)
        {
            IPromise[] promisesArray = promises.ToArray();
            if (promisesArray.Length == 0)
            {
                throw new InvalidOperationException("At least 1 input promise must be provided for Race.");
            }

            IPromise resultPromise = new Promise();
            resultPromise.WithName("Race");

            float[] progress = new float[promisesArray.Length];

            promisesArray.Each((promise, index) =>
            {
                promise
                    .Progress(v =>
                    {
                        progress[index] = v;
                        resultPromise.ReportProgress(progress.Max());
                    })
                    .Catch(ex =>
                    {
                        if (resultPromise.IsPending)
                        {
                            resultPromise.Reject(ex);
                        }
                    })
                    .Then(() =>
                    {
                        if (resultPromise.IsPending)
                        {
                            resultPromise.Resolve();
                        }
                    })
                    .Done();
            });

            return resultPromise;
        }

        public static IPromise Create()
            => new Promise();

        public static IPromise Resolved() 
            => resolvedPromise;

        public static IPromise Rejected(Exception exception)
        {
            IPromise promise = new Promise(PromiseState.Rejected)
            {
                rejectionException = exception
            };
            return promise;
        }

        public IPromise Finally(Action onComplete)
        {
            if (IsResolved)
            {
                try
                {
                    onComplete();
                    return this;
                }
                catch (Exception exception)
                {
                    return Rejected(exception);
                }
            }

            IPromise promise = new Promise();
            promise.WithName(Name);

            Then(promise.Resolve);
            Catch(exception => {
                try
                {
                    onComplete();
                    promise.Reject(exception);
                }
                catch (Exception e)
                {
                    promise.Reject(e);
                }
            });

            return promise.Then(onComplete);
        }

        public IPromise ContinueWith(Func<IPromise> onComplete)
        {
            IPromise promise = new Promise();
            promise.WithName(Name);

            Then(promise.Resolve);
            Catch(exception => promise.Resolve());

            return promise.Then(onComplete);
        }

        public IPromise<TConvertedType> ContinueWith<TConvertedType>(Func<IPromise<TConvertedType>> onComplete)
        {
            IPromise promise = new Promise();
            promise.WithName(Name);

            Then(promise.Resolve);
            Catch(exception => promise.Resolve());

            return promise.Then(onComplete);
        }

        public IPromise Progress(Action<float> onProgress)
        {
            if (IsPending && onProgress != null)
            {
                ProgressHandlers(this, onProgress);
            }
            return this;
        }
    }

    /// <summary>
    /// Implements a non-generic C# promise.
    /// https://developer.mozilla.org/en/docs/Web/JavaScript/Reference/Global_Objects/Promise
    /// </summary>
    public class Promise<TPromisedType> : AbstractPromise, IPromise<TPromisedType>
    {
        private Exception rejectionException;
        private TPromisedType resolveValue;
        private List<RejectHandler> rejectHandlers;
        private List<ProgressHandler> progressHandlers;
        private List<Action<TPromisedType>> resolveCallbacks;
        private List<IRejectable> resolveRejectables;

        public Promise() : base()
        {
            if (enablePromiseTracking)
            {
                pendingPromises.Add(this);
            }
        }

        public Promise(Action<Action<TPromisedType>, Action<Exception>> resolver) : base()
        {
            if (enablePromiseTracking)
            {
                pendingPromises.Add(this);
            }
            try
            {
                resolver(Resolve, Reject);
            }
            catch (Exception ex)
            {
                Reject(ex);
            }
        }

        private Promise(PromiseState initialState) : base()
            => State = initialState;

        private void AddRejectHandler(Action<Exception> onRejected, IRejectable rejectable)
        {
            if (rejectHandlers == null)
            {
                rejectHandlers = new();
            }
            rejectHandlers.Add(new() { callback = onRejected, rejectable = rejectable });
        }

        private void AddResolveHandler(Action<TPromisedType> onResolved, IRejectable rejectable)
        {
            if (resolveCallbacks == null)
            {
                resolveCallbacks = new();
            }
            if (resolveRejectables == null)
            {
                resolveRejectables = new();
            }
            resolveCallbacks.Add(onResolved);
            resolveRejectables.Add(rejectable);
        }

        private void AddProgressHandler(Action<float> onProgress, IRejectable rejectable)
        {
            if (progressHandlers == null)
            {
                progressHandlers = new();
            }
            progressHandlers.Add(new() { callback = onProgress, rejectable = rejectable });
        }

        private void InvokeHandler<T>(Action<T> callback, IRejectable rejectable, T value)
        {
            try
            {
                callback(value);
            }
            catch (Exception ex)
            {
                rejectable.Reject(ex);
            }
        }

        private void ClearHandlers()
        {
            rejectHandlers = null;
            resolveCallbacks = null;
            resolveRejectables = null;
            progressHandlers = null;
        }

        private void InvokeRejectHandlers(Exception ex)
        {
            if (rejectHandlers != null)
            {
                for (int i = 0, max = rejectHandlers.Count; i < max; i++)
                {
                    InvokeHandler(rejectHandlers[i].callback, rejectHandlers[i].rejectable, ex);
                }
            }
            ClearHandlers();
        }

        private void InvokeResolveHandlers(TPromisedType value)
        {
            if (resolveCallbacks != null)
            {
                for (int i = 0, max = resolveCallbacks.Count; i < max; i++)
                {
                    InvokeHandler(resolveCallbacks[i], resolveRejectables[i], value);
                }
            }
            ClearHandlers();
        }

        private void InvokeProgressHandlers(in float progress)
        {
            if (progressHandlers != null)
            {
                for (int i = 0, max = progressHandlers.Count; i < max; i++)
                {
                    InvokeHandler(progressHandlers[i].callback, progressHandlers[i].rejectable, progress);
                }
            }
        }

        public void Reject(Exception ex)
        {
            if (!IsPending)
            {
                throw new PromiseStateException($"Attempt to reject a promise that is already in state: {State}, a promise can only be rejected when it is still in state: {PromiseState.Pending}.");
            }
            rejectionException = ex;
            State = PromiseState.Rejected;
            if (enablePromiseTracking)
            {
                pendingPromises.Remove(this);
            }
            InvokeRejectHandlers(ex);
        }

        public void Resolve(TPromisedType value)
        {
            if (!IsPending)
            {
                throw new PromiseStateException($"Attempt to resolve a promise that is already in state: {State}, a promise can only be resolved when it is still in state: {PromiseState.Pending}.");
            }
            resolveValue = value;
            State = PromiseState.Resolved;
            if (enablePromiseTracking)
            {
                pendingPromises.Remove(this);
            }
            InvokeResolveHandlers(value);
        }

        public void ReportProgress(in float progress)
        {
            if (!IsPending)
            {
                throw new PromiseStateException($"Attempt to report progress on a promise that is already in state: {State}, a promise can only report progress when it is still in state: {PromiseState.Pending}.");
            }
            InvokeProgressHandlers(progress);
        }

        public void Done(Action<TPromisedType> onResolved, Action<Exception> onRejected)
            => Then(onResolved, onRejected).Catch(ex => PropagateUnhandledException(this, ex));

        public void Done(Action<TPromisedType> onResolved)
            => Then(onResolved).Catch(ex => PropagateUnhandledException(this, ex));

        public void Done()
        {
            if (IsResolved)
            {
                return;
            }
            Catch(ex => PropagateUnhandledException(this, ex));
        }

        public IPromise<TPromisedType> WithName(in string name)
        {
            Name = name;
            return this;
        }

        public IPromise Catch(Action<Exception> onRejected)
        {
            if (IsResolved)
            {
                return Promise.Resolved();
            }
            IPromise resultPromise = new Promise();
            resultPromise.WithName(Name);

            void ResolveHandler(TPromisedType _) 
                => resultPromise.Resolve();

            void RejectHandler(Exception exception)
            {
                try
                {
                    onRejected(exception);
                    resultPromise.Resolve();
                }
                catch (Exception e)
                {
                    resultPromise.Reject(e);
                }
            }

            ActionHandlers(resultPromise, ResolveHandler, RejectHandler);
            ProgressHandlers(resultPromise, v => resultPromise.ReportProgress(v));

            return resultPromise;
        }

        public IPromise<TPromisedType> Catch(Func<Exception, TPromisedType> onRejected)
        {
            if (IsResolved)
            {
                return this;
            }

            IPromise<TPromisedType> resultPromise = new Promise<TPromisedType>();
            resultPromise.WithName(Name);

            void ResolveHandler(TPromisedType v) 
                => resultPromise.Resolve(v);

            void RejectHandler(Exception ex)
            {
                try
                {
                    resultPromise.Resolve(onRejected(ex));
                }
                catch (Exception cbEx)
                {
                    resultPromise.Reject(cbEx);
                }
            }

            ActionHandlers(resultPromise, ResolveHandler, RejectHandler);
            ProgressHandlers(resultPromise, v => resultPromise.ReportProgress(v));

            return resultPromise;
        }

        public IPromise<ConvertedT> Then<ConvertedT>(Func<TPromisedType, IPromise<ConvertedT>> onResolved) 
            => Then(onResolved, null, null);

        public IPromise Then(Func<TPromisedType, IPromise> onResolved) 
            => Then(onResolved, null, null);

        public IPromise Then(Action<TPromisedType> onResolved) 
            => Then(onResolved, null, null);

        public IPromise<TConvertedType> Then<TConvertedType>(Func<TPromisedType, IPromise<TConvertedType>> onResolved, Func<Exception, IPromise<TConvertedType>> onRejected) 
            => Then(onResolved, onRejected, null);

        public IPromise Then(Func<TPromisedType, IPromise> onResolved, Action<Exception> onRejected) 
            => Then(onResolved, onRejected, null);

        public IPromise Then(Action<TPromisedType> onResolved, Action<Exception> onRejected) 
            => Then(onResolved, onRejected, null);

        public IPromise<TConvertedType> Then<TConvertedType>(Func<TPromisedType, IPromise<TConvertedType>> onResolved, Func<Exception, IPromise<TConvertedType>> onRejected, Action<float> onProgress)
        {
            if (IsResolved)
            {
                try
                {
                    return onResolved(resolveValue);
                }
                catch (Exception ex)
                {
                    return Promise<TConvertedType>.Rejected(ex);
                }
            }

            IPromise<TConvertedType> resultPromise = new Promise<TConvertedType>();
            resultPromise.WithName(Name);

            void ResolveHandler(TPromisedType v)
            {
                onResolved(v)
                    .Progress(progress => resultPromise.ReportProgress(progress))
                    .Then(
                        chainedValue => resultPromise.Resolve(chainedValue),
                        ex => resultPromise.Reject(ex)
                    );
            }

            void RejectHandler(Exception ex)
            {
                if (onRejected == null)
                {
                    resultPromise.Reject(ex);
                    return;
                }

                try
                {
                    onRejected(ex)
                        .Then(
                            chainedValue => resultPromise.Resolve(chainedValue),
                            callbackEx => resultPromise.Reject(callbackEx)
                        );
                }
                catch (Exception callbackEx)
                {
                    resultPromise.Reject(callbackEx);
                }
            }

            ActionHandlers(resultPromise, ResolveHandler, RejectHandler);
            if (onProgress != null)
            {
                ProgressHandlers(this, onProgress);
            }

            return resultPromise;
        }

        public IPromise Then(Func<TPromisedType, IPromise> onResolved, Action<Exception> onRejected, Action<float> onProgress)
        {
            if (IsResolved)
            {
                try
                {
                    return onResolved(resolveValue);
                }
                catch (Exception ex)
                {
                    return Promise.Rejected(ex);
                }
            }

            IPromise resultPromise = new Promise();
            resultPromise.WithName(Name);

            void ResolveHandler(TPromisedType v)
            {
                if (onResolved != null)
                {
                    onResolved(v)
                        .Progress(progress => resultPromise.ReportProgress(progress))
                        .Then(
                            () => resultPromise.Resolve(),
                            ex => resultPromise.Reject(ex)
                        );
                } else
                {
                    resultPromise.Resolve();
                }
            }

            void RejectHandler(Exception ex)
            {
                onRejected?.Invoke(ex);
                resultPromise.Reject(ex);
            }

            ActionHandlers(resultPromise, ResolveHandler, RejectHandler);
            if (onProgress != null)
            {
                ProgressHandlers(this, onProgress);
            }

            return resultPromise;
        }

        public IPromise Then(Action<TPromisedType> onResolved, Action<Exception> onRejected, Action<float> onProgress)
        {
            if (IsResolved)
            {
                try
                {
                    onResolved(resolveValue);
                    return Promise.Resolved();
                }
                catch (Exception ex)
                {
                    return Promise.Rejected(ex);
                }
            }

            IPromise resultPromise = new Promise();
            resultPromise.WithName(Name);

            void ResolveHandler(TPromisedType v)
            {
                onResolved?.Invoke(v);
                resultPromise.Resolve();
            }

            void RejectHandler(Exception ex)
            {
                onRejected?.Invoke(ex);
                resultPromise.Reject(ex);
            }

            ActionHandlers(resultPromise, ResolveHandler, RejectHandler);
            if (onProgress != null)
            {
                ProgressHandlers(this, onProgress);
            }

            return resultPromise;
        }

        public IPromise<TConvertedType> Then<TConvertedType>(Func<TPromisedType, TConvertedType> transform) 
            => Then(value => Promise<TConvertedType>.Resolved(transform(value)));

        private void ActionHandlers(IRejectable resultPromise, Action<TPromisedType> resolveHandler, Action<Exception> rejectHandler)
        {
            if (IsResolved)
            {
                InvokeHandler(resolveHandler, resultPromise, resolveValue);
            } else if (IsRejected)
            {
                InvokeHandler(rejectHandler, resultPromise, rejectionException);
            } else
            {
                AddResolveHandler(resolveHandler, resultPromise);
                AddRejectHandler(rejectHandler, resultPromise);
            }
        }

        private void ProgressHandlers(IRejectable resultPromise, Action<float> progressHandler)
        {
            if (IsPending)
            {
                AddProgressHandler(progressHandler, resultPromise);
            }
        }

        public static IPromise<T> First<T>(params Func<IPromise<T>>[] fns) 
            => First((IEnumerable<Func<IPromise<T>>>)fns);

        public static IPromise<T> First<T>(IEnumerable<Func<IPromise<T>>> fns)
        {
            IPromise<T> promise = new Promise<T>();
            int count = 0;

            fns.Aggregate(
                Promise<T>.Rejected(null),
                (prevPromise, fn) =>
                {
                    int itemSequence = count;
                    count++;

                    IPromise<T> newPromise = new Promise<T>();
                    prevPromise
                        .Progress(v =>
                        {
                            float sliceLength = 1.0f / count;
                            promise.ReportProgress(sliceLength * (v + itemSequence));
                        })
                        .Then(newPromise.Resolve)
                        .Catch(ex =>
                        {
                            float sliceLength = 1.0f / count;
                            promise.ReportProgress(sliceLength * itemSequence);

                            fn()
                                .Then(value => newPromise.Resolve(value))
                                .Catch(newPromise.Reject)
                                .Done()
                            ;
                        })
                    ;
                    return newPromise;
                })
            .Then(value => promise.Resolve(value))
            .Catch(exception =>
            {
                promise.ReportProgress(1.0f);
                promise.Reject(exception);
            });

            return promise;
        }

        public IPromise<IEnumerable<TConvertedType>> ThenAll<TConvertedType>(Func<TPromisedType, IEnumerable<IPromise<TConvertedType>>> chain) 
            => Then(value => Promise<TConvertedType>.All(chain(value)));

        public IPromise ThenAll(Func<TPromisedType, IEnumerable<IPromise>> chain) 
            => Then(value => Promise.All(chain(value)));

        public static IPromise<IEnumerable<TPromisedType>> All(params IPromise<TPromisedType>[] promises) 
            => All((IEnumerable<IPromise<TPromisedType>>)promises);

        public static IPromise<IEnumerable<TPromisedType>> All(IEnumerable<IPromise<TPromisedType>> promises)
        {
            IPromise<TPromisedType>[] promisesArray = promises.ToArray();
            if (promisesArray.Length == 0)
            {
                return Promise<IEnumerable<TPromisedType>>.Resolved(Enumerable.Empty<TPromisedType>());
            }

            int remainingCount = promisesArray.Length;
            TPromisedType[] results = new TPromisedType[remainingCount];
            float[] progress = new float[remainingCount];
            IPromise<IEnumerable<TPromisedType>> resultPromise = new Promise<IEnumerable<TPromisedType>>();
            resultPromise.WithName("All");

            promisesArray.Each((promise, index) =>
            {
                promise
                    .Progress(v =>
                    {
                        progress[index] = v;
                        if (resultPromise.IsPending)
                        {
                            resultPromise.ReportProgress(progress.Average());
                        }
                    })
                    .Then(result =>
                    {
                        progress[index] = 1.0f;
                        results[index] = result;
                        remainingCount--;
                        if (remainingCount <= 0 && resultPromise.IsPending)
                        {
                            resultPromise.Resolve(results);
                        }
                    })
                    .Catch(ex =>
                    {
                        if (resultPromise.IsPending)
                        {
                            resultPromise.Reject(ex);
                        }
                    })
                    .Done();
            });

            return resultPromise;
        }

        public IPromise<TConvertedType> ThenRace<TConvertedType>(Func<TPromisedType, IEnumerable<IPromise<TConvertedType>>> chain) 
            => Then(value => Promise<TConvertedType>.Race(chain(value)));

        public IPromise ThenRace(Func<TPromisedType, IEnumerable<IPromise>> chain) 
            => Then(value => Promise.Race(chain(value)));

        public static IPromise<TPromisedType> Race(params IPromise<TPromisedType>[] promises) 
            => Race((IEnumerable<IPromise<TPromisedType>>)promises);

        public static IPromise<TPromisedType> Race(IEnumerable<IPromise<TPromisedType>> promises)
        {
            IPromise<TPromisedType>[] promisesArray = promises.ToArray();
            if (promisesArray.Length == 0)
            {
                throw new InvalidOperationException("At least 1 input promise must be provided for Race.");
            }

            IPromise<TPromisedType> resultPromise = new Promise<TPromisedType>();
            resultPromise.WithName("Race");

            float[] progress = new float[promisesArray.Length];

            promisesArray.Each((promise, index) =>
            {
                promise
                    .Progress(v =>
                    {
                        if (resultPromise.IsPending)
                        {
                            progress[index] = v;
                            resultPromise.ReportProgress(progress.Max());
                        }
                    })
                    .Then(result =>
                    {
                        if (resultPromise.IsPending)
                        {
                            resultPromise.Resolve(result);
                        }
                    })
                    .Catch(ex =>
                    {
                        if (resultPromise.IsPending)
                        {
                            resultPromise.Reject(ex);
                        }
                    })
                    .Done();
            });

            return resultPromise;
        }

        public static IPromise<TPromisedType> Create()
            => new Promise<TPromisedType>();

        public static IPromise<TPromisedType> Resolved(TPromisedType promisedValue)
        {
            IPromise<TPromisedType> promise = new Promise<TPromisedType>(PromiseState.Resolved)
            {
                resolveValue = promisedValue
            };
            return promise;
        }

        public static IPromise<TPromisedType> Rejected(Exception exception)
        {
            IPromise<TPromisedType> promise = new Promise<TPromisedType>(PromiseState.Rejected)
            {
                rejectionException = exception
            };
            return promise;
        }

        public IPromise<TPromisedType> Finally(Action onComplete)
        {
            if (IsResolved)
            {
                try
                {
                    onComplete();
                    return this;
                }
                catch (Exception exception)
                {
                    return Rejected(exception);
                }
            }

            IPromise<TPromisedType> promise = new Promise<TPromisedType>();
            promise.WithName(Name);

            Then(promise.Resolve);
            Catch(exception => {
                try
                {
                    onComplete();
                    promise.Reject(exception);
                }
                catch (Exception e)
                {
                    promise.Reject(e);
                }
            });

            return promise.Then(v =>
            {
                onComplete();
                return v;
            });
        }

        public IPromise ContinueWith(Func<IPromise> onComplete)
        {
            IPromise promise = new Promise();
            promise.WithName(Name);
            Then(x => promise.Resolve());
            Catch(e => promise.Resolve());
            return promise.Then(onComplete);
        }

        public IPromise<TConvertedType> ContinueWith<TConvertedType>(Func<IPromise<TConvertedType>> onComplete)
        {
            IPromise promise = new Promise();
            promise.WithName(Name);
            Then(x => promise.Resolve());
            Catch(e => promise.Resolve());
            return promise.Then(onComplete);
        }

        public IPromise<TPromisedType> Progress(Action<float> onProgress)
        {
            if (IsPending && onProgress != null)
            {
                ProgressHandlers(this, onProgress);
            }
            return this;
        }
    }
}