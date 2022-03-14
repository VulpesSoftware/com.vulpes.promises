using System;
using System.Collections.Generic;
using System.Linq;

namespace Vulpes.Promises
{
    /// <summary>
    /// Represents a handler invoked when the promise is resolved.
    /// </summary>
    public struct ResolveHandler
    {
        /// <summary>
        /// Callback.
        /// </summary>
        public Action callback;

        /// <summary>
        /// The promise that is rejected when there is an error while invoking the handler.
        /// </summary>
        public IRejectable rejectable;
    }

    /// <summary>
    /// Represents a handler invoked when the promise is rejected.
    /// </summary>
    public struct RejectHandler
    {
        /// <summary>
        /// Callback.
        /// </summary>
        public Action<Exception> callback;

        /// <summary>
        /// The promise that is rejected when there is an error while invoking the handler.
        /// </summary>
        public IRejectable rejectable;
    }

    public struct ProgressHandler
    {
        /// <summary>
        /// Callback.
        /// </summary>
        public Action<float> callback;

        /// <summary>
        /// The promise that is rejected when there is an error while invoking the handler.
        /// </summary>
        public IRejectable rejectable;
    }

    /// <summary>
    /// Implements a non-generic C# promise, this is a promise that simply resolves without delivering a value.
    /// https://developer.mozilla.org/en/docs/Web/JavaScript/Reference/Global_Objects/Promise
    /// </summary>
    public class Promise : AbstractPromise, IPromise
    {
        /// <summary>
        /// The exception when the promise is rejected.
        /// </summary>
        private Exception rejectionException;

        /// <summary>
        /// Error handlers.
        /// </summary>
        private List<RejectHandler> rejectHandlers;

        /// <summary>
        /// Completed handlers that accept no value.
        /// </summary>
        private List<ResolveHandler> resolveHandlers;

        /// <summary>
        /// Progress handlers.
        /// </summary>
        private List<ProgressHandler> progressHandlers;

        public Promise() : base()
        {
            if (enablePromiseTracking)
            {
                PendingPromises.Add(this);
            }
        }

        public Promise(Action<Action, Action<Exception>> resolver) : base()
        {
            if (enablePromiseTracking)
            {
                PendingPromises.Add(this);
            }
            try
            {
                resolver(Resolve, Reject);
            } catch (Exception ex)
            {
                Reject(ex);
            }
        }

        private Promise(PromiseState initialState) : base()
        {
            State = initialState;
        }

        /// <summary>
        /// Add a rejection handler for this promise.
        /// </summary>
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

        /// <summary>
        /// Add a resolve handler for this promise.
        /// </summary>
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

        /// <summary>
        /// Add a progress handler for this promise.
        /// </summary>
        private void AddProgressHandler(Action<float> onProgress, IRejectable rejectable)
        {
            if (progressHandlers == null)
            {
                progressHandlers = new();
            }

            progressHandlers.Add(new() { callback = onProgress, rejectable = rejectable });
        }

        /// <summary>
        /// Invoke a single error handler.
        /// </summary>
        private void InvokeRejectHandler(Action<Exception> callback, IRejectable rejectable, Exception value)
        {
            try
            {
                callback(value);
            } catch (Exception ex)
            {
                rejectable.Reject(ex);
            }
        }

        /// <summary>
        /// Invoke a single resolve handler.
        /// </summary>
        private void InvokeResolveHandler(Action callback, IRejectable rejectable)
        {
            try
            {
                callback();
            } catch (Exception ex)
            {
                rejectable.Reject(ex);
            }
        }

        /// <summary>
        /// Invoke a single progress handler.
        /// </summary>
        private void InvokeProgressHandler(Action<float> callback, IRejectable rejectable, in float progress)
        {
            try
            {
                callback(progress);
            } catch (Exception ex)
            {
                rejectable.Reject(ex);
            }
        }

        /// <summary>
        /// Helper function clear out all handlers after resolution or rejection.
        /// </summary>
        private void ClearHandlers()
        {
            rejectHandlers = null;
            resolveHandlers = null;
            progressHandlers = null;
        }

        /// <summary>
        /// Invoke all reject handlers.
        /// </summary>
        private void InvokeRejectHandlers(Exception ex)
        {
            if (rejectHandlers != null)
            {
                for (int i = 0, maxI = rejectHandlers.Count; i < maxI; i++)
                {
                    InvokeRejectHandler(rejectHandlers[i].callback, rejectHandlers[i].rejectable, ex);
                }
            }

            ClearHandlers();
        }

        /// <summary>
        /// Invoke all resolve handlers.
        /// </summary>
        private void InvokeResolveHandlers()
        {
            if (resolveHandlers != null)
            {
                for (int i = 0, maxI = resolveHandlers.Count; i < maxI; i++)
                {
                    InvokeResolveHandler(resolveHandlers[i].callback, resolveHandlers[i].rejectable);
                }
            }

            ClearHandlers();
        }

        /// <summary>
        /// Invoke all progress handlers.
        /// </summary>
        private void InvokeProgressHandlers(in float progress)
        {
            if (progressHandlers != null)
            {
                for (int i = 0, maxI = progressHandlers.Count; i < maxI; i++)
                {
                    InvokeProgressHandler(progressHandlers[i].callback, progressHandlers[i].rejectable, progress);
                }
            }
        }

        /// <summary>
        /// Reject the promise with an exception.
        /// </summary>
        public void Reject(Exception ex)
        {
            if (State != PromiseState.Pending)
            {
                throw new PromiseStateException(
                    "Attempt to reject a promise that is already in state: " + State
                    + ", a promise can only be rejected when it is still in state: "
                    + PromiseState.Pending
                );
            }

            rejectionException = ex;
            State = PromiseState.Rejected;

            if (enablePromiseTracking)
            {
                PendingPromises.Remove(this);
            }

            InvokeRejectHandlers(ex);
        }

        /// <summary>
        /// Resolve the promise with a particular value.
        /// </summary>
        public void Resolve()
        {
            if (State != PromiseState.Pending)
            {
                throw new PromiseStateException(
                    "Attempt to resolve a promise that is already in state: " + State
                    + ", a promise can only be resolved when it is still in state: "
                    + PromiseState.Pending
                );
            }

            State = PromiseState.Resolved;

            if (enablePromiseTracking)
            {
                PendingPromises.Remove(this);
            }

            InvokeResolveHandlers();
        }


        /// <summary>
        /// Report progress on the promise.
        /// </summary>
        public void ReportProgress(in float progress)
        {
            if (State != PromiseState.Pending)
            {
                throw new PromiseStateException(
                    "Attempt to report progress on a promise that is already in state: "
                    + State + ", a promise can only report progress when it is still in state: "
                    + PromiseState.Pending
                );
            }

            InvokeProgressHandlers(progress);
        }


        /// <summary>
        /// Completes the promise. 
        /// onResolved is called on successful completion.
        /// onRejected is called on error.
        /// </summary>
        public void Done(Action onResolved, Action<Exception> onRejected)
        {
            Then(onResolved, onRejected)
                .Catch(ex =>
                    PropagateUnhandledException(this, ex)
                );
        }

        /// <summary>
        /// Completes the promise. 
        /// onResolved is called on successful completion.
        /// Adds a default error handler.
        /// </summary>
        public void Done(Action onResolved)
        {
            Then(onResolved)
                .Catch(ex =>
                    PropagateUnhandledException(this, ex)
                );
        }

        /// <summary>
        /// Complete the promise. Adds a defualt error handler.
        /// </summary>
        public void Done()
        {
            if (State == PromiseState.Resolved)
            {
                return;
            }

            Catch(ex => PropagateUnhandledException(this, ex));
        }

        /// <summary>
        /// Set the name of the promise, useful for debugging.
        /// </summary>
        public IPromise WithName(in string name)
        {
            Name = name;
            return this;
        }

        /// <summary>
        /// Handle errors for the promise. 
        /// </summary>
        public IPromise Catch(Action<Exception> onRejected)
        {
            if (State == PromiseState.Resolved)
            {
                return this;
            }

            var resultPromise = new Promise();
            resultPromise.WithName(Name);

            void resolveHandler() => resultPromise.Resolve();

            void rejectHandler(Exception ex)
            {
                try
                {
                    onRejected(ex);
                    resultPromise.Resolve();
                }
                catch (Exception callbackException)
                {
                    resultPromise.Reject(callbackException);
                }
            }

            ActionHandlers(resultPromise, resolveHandler, rejectHandler);
            ProgressHandlers(resultPromise, v => resultPromise.ReportProgress(v));

            return resultPromise;
        }

        /// <summary>
        /// Add a resolved callback that chains a value promise (optionally converting to a different value type).
        /// </summary>
        public IPromise<ConvertedT> Then<ConvertedT>(Func<IPromise<ConvertedT>> onResolved) => Then(onResolved, null, null);

        /// <summary>
        /// Add a resolved callback that chains a non-value promise.
        /// </summary>
        public IPromise Then(Func<IPromise> onResolved) => Then(onResolved, null, null);

        /// <summary>
        /// Add a resolved callback.
        /// </summary>
        public IPromise Then(Action onResolved) => Then(onResolved, null, null);

        /// <summary>
        /// Add a resolved callback and a rejected callback.
        /// The resolved callback chains a value promise (optionally converting to a different value type).
        /// </summary>
        public IPromise<ConvertedT> Then<ConvertedT>(Func<IPromise<ConvertedT>> onResolved, Func<Exception, IPromise<ConvertedT>> onRejected) => Then(onResolved, onRejected, null);

        /// <summary>
        /// Add a resolved callback and a rejected callback.
        /// The resolved callback chains a non-value promise.
        /// </summary>
        public IPromise Then(Func<IPromise> onResolved, Action<Exception> onRejected) => Then(onResolved, onRejected, null);

        /// <summary>
        /// Add a resolved callback and a rejected callback.
        /// </summary>
        public IPromise Then(Action onResolved, Action<Exception> onRejected) => Then(onResolved, onRejected, null);

        /// <summary>
        /// Add a resolved callback, a rejected callback and a progress callback.
        /// The resolved callback chains a value promise (optionally converting to a different value type).
        /// </summary>
        public IPromise<ConvertedT> Then<ConvertedT>(
            Func<IPromise<ConvertedT>> onResolved,
            Func<Exception, IPromise<ConvertedT>> onRejected,
            Action<float> onProgress)
        {
            if (State == PromiseState.Resolved)
            {
                try
                {
                    return onResolved();
                } catch (Exception ex)
                {
                    return Promise<ConvertedT>.Rejected(ex);
                }
            }

            // This version of the function must supply an onResolved.
            // Otherwise there is now way to get the converted value to pass to the resulting promise.

            var resultPromise = new Promise<ConvertedT>();
            resultPromise.WithName(Name);

            void resolveHandler()
            {
                onResolved()
                    .Progress(progress => resultPromise.ReportProgress(progress))
                    .Then(
                        // Should not be necessary to specify the arg type on the next line, but Unity (mono) has an internal compiler error otherwise.
                        chainedValue => resultPromise.Resolve(chainedValue),
                        ex => resultPromise.Reject(ex)
                    );
            }

            void rejectHandler(Exception ex)
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
                } catch (Exception callbackEx)
                {
                    resultPromise.Reject(callbackEx);
                }
            }

            ActionHandlers(resultPromise, resolveHandler, rejectHandler);
            if (onProgress != null)
            {
                ProgressHandlers(this, onProgress);
            }

            return resultPromise;
        }

        /// <summary>
        /// Add a resolved callback, a rejected callback and a progress callback.
        /// The resolved callback chains a non-value promise.
        /// </summary>
        public IPromise Then(Func<IPromise> onResolved, Action<Exception> onRejected, Action<float> onProgress)
        {
            if (State == PromiseState.Resolved)
            {
                try
                {
                    return onResolved();
                } catch (Exception ex)
                {
                    return Rejected(ex);
                }
            }

            var resultPromise = new Promise();
            resultPromise.WithName(Name);

            void resolveHandler()
            {
                if (onResolved != null)
                {
                    onResolved()
                        .Progress(progress => resultPromise.ReportProgress(progress))
                        .Then(() => resultPromise.Resolve(), ex => resultPromise.Reject(ex));
                } else
                {
                    resultPromise.Resolve();
                }
            }

            void rejectHandler(Exception ex)
            {
                onRejected?.Invoke(ex);
                resultPromise.Reject(ex);
            }

            ActionHandlers(resultPromise, resolveHandler, rejectHandler);
            if (onProgress != null)
            {
                ProgressHandlers(this, onProgress);
            }

            return resultPromise;
        }

        /// <summary>
        /// Add a resolved callback, a rejected callback and a progress callback.
        /// </summary>
        public IPromise Then(Action onResolved, Action<Exception> onRejected, Action<float> onProgress)
        {
            if (State == PromiseState.Resolved)
            {
                try
                {
                    onResolved();
                    return this;
                }
                catch (Exception ex)
                {
                    return Rejected(ex);
                }
            }

            var resultPromise = new Promise();
            resultPromise.WithName(Name);

            void resolveHandler()
            {
                onResolved?.Invoke();
                resultPromise.Resolve();
            }

            void rejectHandler(Exception ex)
            {
                if (onRejected != null)
                {
                    onRejected(ex);
                    resultPromise.Resolve();
                    return;
                }
                resultPromise.Reject(ex);
            }

            ActionHandlers(resultPromise, resolveHandler, rejectHandler);
            if (onProgress != null)
            {
                ProgressHandlers(this, onProgress);
            }

            return resultPromise;
        }

        /// <summary>
        /// Helper function to invoke or register resolve/reject handlers.
        /// </summary>
        private void ActionHandlers(IRejectable resultPromise, Action resolveHandler, Action<Exception> rejectHandler)
        {
            if (State == PromiseState.Resolved)
            {
                InvokeResolveHandler(resolveHandler, resultPromise);
            } else if (State == PromiseState.Rejected)
            {
                InvokeRejectHandler(rejectHandler, resultPromise, rejectionException);
            } else
            {
                AddResolveHandler(resolveHandler, resultPromise);
                AddRejectHandler(rejectHandler, resultPromise);
            }
        }

        /// <summary>
        /// Helper function to invoke or register progress handlers.
        /// </summary>
        private void ProgressHandlers(IRejectable resultPromise, Action<float> progressHandler)
        {
            if (State == PromiseState.Pending)
            {
                AddProgressHandler(progressHandler, resultPromise);
            }
        }

        /// <summary>
        /// Chain an enumerable of promises, all of which must resolve.
        /// The resulting promise is resolved when all of the promises have resolved.
        /// It is rejected as soon as any of the promises have been rejected.
        /// </summary>
        public IPromise ThenAll(Func<IEnumerable<IPromise>> chain)
        {
            return Then(() => All(chain()));
        }

        /// <summary>
        /// Chain an enumerable of promises, all of which must resolve.
        /// Converts to a non-value promise.
        /// The resulting promise is resolved when all of the promises have resolved.
        /// It is rejected as soon as any of the promises have been rejected.
        /// </summary>
        public IPromise<IEnumerable<ConvertedT>> ThenAll<ConvertedT>(Func<IEnumerable<IPromise<ConvertedT>>> chain)
        {
            return Then(() => Promise<ConvertedT>.All(chain()));
        }

        /// <summary>
        /// Returns a promise that resolves when all of the promises in the enumerable argument have resolved.
        /// Returns a promise of a collection of the resolved results.
        /// </summary>
        public static IPromise All(params IPromise[] promises)
        {
            return All((IEnumerable<IPromise>)promises); // Cast is required to force use of the other All function.
        }

        /// <summary>
        /// Returns a promise that resolves when all of the promises in the enumerable argument have resolved.
        /// Returns a promise of a collection of the resolved results.
        /// </summary>
        public static IPromise All(IEnumerable<IPromise> promises)
        {
            var promisesArray = promises.ToArray();
            if (promisesArray.Length == 0)
            {
                return Resolved();
            }

            var remainingCount = promisesArray.Length;
            var resultPromise = new Promise();
            resultPromise.WithName("All");
            var progress = new float[remainingCount];

            for (int i = 0; i < promisesArray.Length; i++)
            {
                promisesArray[i]
                    .Progress(v =>
                    {
                        progress[i] = v;
                        if (resultPromise.State == PromiseState.Pending)
                        {
                            resultPromise.ReportProgress(progress.Average());
                        }
                    })
                    .Then(() =>
                    {
                        progress[i] = 1.0f;
                        remainingCount--;
                        if (remainingCount <= 0 && resultPromise.State == PromiseState.Pending)
                        {
                            // This will never happen if any of the promises errorred.
                            resultPromise.Resolve();
                        }
                    })
                    .Catch(ex =>
                    {
                        if (resultPromise.State == PromiseState.Pending)
                        {
                            // If a promise errorred and the result promise is still pending, reject it.
                            resultPromise.Reject(ex);
                        }
                    })
                    .Done();
            }

            return resultPromise;
        }

        /// <summary>
        /// Chain a sequence of operations using promises.
        /// Reutrn a collection of functions each of which starts an async operation and yields a promise.
        /// Each function will be called and each promise resolved in turn.
        /// The resulting promise is resolved after each promise is resolved in sequence.
        /// </summary>
        public IPromise ThenSequence(Func<IEnumerable<Func<IPromise>>> chain)
        {
            return Then(() => Sequence(chain()));
        }

        /// <summary>
        /// Chain a number of operations using promises.
        /// Takes a number of functions each of which starts an async operation and yields a promise.
        /// </summary>
        public static IPromise Sequence(params Func<IPromise>[] fns)
        {
            return Sequence((IEnumerable<Func<IPromise>>)fns);
        }

        /// <summary>
        /// Chain a sequence of operations using promises.
        /// Takes a collection of functions each of which starts an async operation and yields a promise.
        /// </summary>
        public static IPromise Sequence(IEnumerable<Func<IPromise>> fns)
        {
            var promise = new Promise();

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
                                var sliceLength = 1.0f / count;
                                promise.ReportProgress(sliceLength * itemSequence);
                                return fn();
                            })
                            .Progress(v =>
                            {
                                var sliceLength = 1.0f / count;
                                promise.ReportProgress(sliceLength * (v + itemSequence));
                            })
                    ;
                }
            )
            .Then(promise.Resolve)
            .Catch(promise.Reject);

            return promise;
        }

        /// <summary>
        /// Takes a function that yields an enumerable of promises.
        /// Returns a promise that resolves when the first of the promises has resolved.
        /// </summary>
        public IPromise ThenRace(Func<IEnumerable<IPromise>> chain)
        {
            return Then(() => Race(chain()));
        }

        /// <summary>
        /// Takes a function that yields an enumerable of promises.
        /// Converts to a value promise.
        /// Returns a promise that resolves when the first of the promises has resolved.
        /// </summary>
        public IPromise<ConvertedT> ThenRace<ConvertedT>(Func<IEnumerable<IPromise<ConvertedT>>> chain)
        {
            return Then(() => Promise<ConvertedT>.Race(chain()));
        }

        /// <summary>
        /// Returns a promise that resolves when the first of the promises in the enumerable argument have resolved.
        /// Returns the value from the first promise that has resolved.
        /// </summary>
        public static IPromise Race(params IPromise[] promises)
        {
            return Race((IEnumerable<IPromise>)promises); // Cast is required to force use of the other function.
        }

        /// <summary>
        /// Returns a promise that resolves when the first of the promises in the enumerable argument have resolved.
        /// Returns the value from the first promise that has resolved.
        /// </summary>
        public static IPromise Race(IEnumerable<IPromise> promises)
        {
            var promisesArray = promises.ToArray();
            if (promisesArray.Length == 0)
            {
                throw new InvalidOperationException("At least 1 input promise must be provided for Race");
            }

            var resultPromise = new Promise();
            resultPromise.WithName("Race");

            var progress = new float[promisesArray.Length];

            for (int i = 0; i < promisesArray.Length; i++)
            {
                promisesArray[i]
                    .Progress(v =>
                    {
                        progress[i] = v;
                        resultPromise.ReportProgress(progress.Max());
                    })
                    .Catch(ex =>
                    {
                        if (resultPromise.State == PromiseState.Pending)
                        {
                            // If a promise errorred and the result promise is still pending, reject it.
                            resultPromise.Reject(ex);
                        }
                    })
                    .Then(() =>
                    {
                        if (resultPromise.State == PromiseState.Pending)
                        {
                            resultPromise.Resolve();
                        }
                    })
                    .Done();
            }

            return resultPromise;
        }

        /// <summary>
        /// Creates and returns a new promise.
        /// </summary>
        public static IPromise Create()
        {
            return new Promise();
        }

        private static readonly IPromise resolvedPromise = new Promise(PromiseState.Resolved);

        /// <summary>
        /// Convert a simple value directly into a resolved promise.
        /// </summary>
        public static IPromise Resolved() => resolvedPromise;

        /// <summary>
        /// Convert an exception directly into a rejected promise.
        /// </summary>
        public static IPromise Rejected(Exception ex)
        {
            var promise = new Promise(PromiseState.Rejected)
            {
                rejectionException = ex
            };
            return promise;
        }

        public IPromise Finally(Action onComplete)
        {
            if (State == PromiseState.Resolved)
            {
                try
                {
                    onComplete();
                    return this;
                }
                catch (Exception ex)
                {
                    return Rejected(ex);
                }
            }

            var promise = new Promise();
            promise.WithName(Name);

            Then(promise.Resolve);
            Catch(e => {
                try
                {
                    onComplete();
                    promise.Reject(e);
                }
                catch (Exception ne)
                {
                    promise.Reject(ne);
                }
            });

            return promise.Then(onComplete);
        }

        public IPromise ContinueWith(Func<IPromise> onComplete)
        {
            var promise = new Promise();
            promise.WithName(Name);

            Then(promise.Resolve);
            Catch(e => promise.Resolve());

            return promise.Then(onComplete);
        }

        public IPromise<ConvertedT> ContinueWith<ConvertedT>(Func<IPromise<ConvertedT>> onComplete)
        {
            var promise = new Promise();
            promise.WithName(Name);

            Then(promise.Resolve);
            Catch(e => promise.Resolve());

            return promise.Then(onComplete);
        }

        public IPromise Progress(Action<float> onProgress)
        {
            if (State == PromiseState.Pending && onProgress != null)
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
    public class Promise<PromisedT> : AbstractPromise, IPromise<PromisedT>
    {
        /// <summary>
        /// The exception when the promise is rejected.
        /// </summary>
        private Exception rejectionException;

        /// <summary>
        /// The value when the promises is resolved.
        /// </summary>
        private PromisedT resolveValue;

        /// <summary>
        /// Error handler.
        /// </summary>
        private List<RejectHandler> rejectHandlers;

        /// <summary>
        /// Progress handlers.
        /// </summary>
        private List<ProgressHandler> progressHandlers;

        /// <summary>
        /// Completed handlers that accept a value.
        /// </summary>
        private List<Action<PromisedT>> resolveCallbacks;
        private List<IRejectable> resolveRejectables;

        public Promise() : base()
        {
            if (enablePromiseTracking)
            {
                PendingPromises.Add(this);
            }
        }

        public Promise(Action<Action<PromisedT>, Action<Exception>> resolver) : base()
        {
            if (enablePromiseTracking)
            {
                PendingPromises.Add(this);
            }
            try
            {
                resolver(Resolve, Reject);
            } catch (Exception ex)
            {
                Reject(ex);
            }
        }

        private Promise(PromiseState initialState) : base()
        {
            State = initialState;
        }

        /// <summary>
        /// Add a rejection handler for this promise.
        /// </summary>
        private void AddRejectHandler(Action<Exception> onRejected, IRejectable rejectable)
        {
            if (rejectHandlers == null)
            {
                rejectHandlers = new List<RejectHandler>();
            }

            rejectHandlers.Add(new RejectHandler { callback = onRejected, rejectable = rejectable });
        }

        /// <summary>
        /// Add a resolve handler for this promise.
        /// </summary>
        private void AddResolveHandler(Action<PromisedT> onResolved, IRejectable rejectable)
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

        /// <summary>
        /// Add a progress handler for this promise.
        /// </summary>
        private void AddProgressHandler(Action<float> onProgress, IRejectable rejectable)
        {
            if (progressHandlers == null)
            {
                progressHandlers = new();
            }

            progressHandlers.Add(new() { callback = onProgress, rejectable = rejectable });
        }

        /// <summary>
        /// Invoke a single handler.
        /// </summary>
        private void InvokeHandler<T>(Action<T> callback, IRejectable rejectable, T value)
        {
            try
            {
                callback(value);
            } catch (Exception ex)
            {
                rejectable.Reject(ex);
            }
        }

        /// <summary>
        /// Helper function clear out all handlers after resolution or rejection.
        /// </summary>
        private void ClearHandlers()
        {
            rejectHandlers = null;
            resolveCallbacks = null;
            resolveRejectables = null;
            progressHandlers = null;
        }

        /// <summary>
        /// Invoke all reject handlers.
        /// </summary>
        private void InvokeRejectHandlers(Exception ex)
        {
            if (rejectHandlers != null)
            {
                for (int i = 0, maxI = rejectHandlers.Count; i < maxI; i++)
                {
                    InvokeHandler(rejectHandlers[i].callback, rejectHandlers[i].rejectable, ex);
                }
            }

            ClearHandlers();
        }

        /// <summary>
        /// Invoke all resolve handlers.
        /// </summary>
        private void InvokeResolveHandlers(PromisedT value)
        {
            if (resolveCallbacks != null)
            {
                for (int i = 0, maxI = resolveCallbacks.Count; i < maxI; i++)
                {
                    InvokeHandler(resolveCallbacks[i], resolveRejectables[i], value);
                }
            }

            ClearHandlers();
        }

        /// <summary>
        /// Invoke all progress handlers.
        /// </summary>
        private void InvokeProgressHandlers(in float progress)
        {
            if (progressHandlers != null)
            {
                for (int i = 0, maxI = progressHandlers.Count; i < maxI; i++)
                {
                    InvokeHandler(progressHandlers[i].callback, progressHandlers[i].rejectable, progress);
                }
            }
        }

        /// <summary>
        /// Reject the promise with an exception.
        /// </summary>
        public void Reject(Exception ex)
        {
            if (State != PromiseState.Pending)
            {
                throw new PromiseStateException(
                    "Attempt to reject a promise that is already in state: " + State
                    + ", a promise can only be rejected when it is still in state: "
                    + PromiseState.Pending
                );
            }

            rejectionException = ex;
            State = PromiseState.Rejected;

            if (enablePromiseTracking)
            {
                PendingPromises.Remove(this);
            }

            InvokeRejectHandlers(ex);
        }

        /// <summary>
        /// Resolve the promise with a particular value.
        /// </summary>
        public void Resolve(PromisedT value)
        {
            if (State != PromiseState.Pending)
            {
                throw new PromiseStateException(
                    "Attempt to resolve a promise that is already in state: " + State
                    + ", a promise can only be resolved when it is still in state: "
                    + PromiseState.Pending
                );
            }

            resolveValue = value;
            State = PromiseState.Resolved;

            if (enablePromiseTracking)
            {
                PendingPromises.Remove(this);
            }

            InvokeResolveHandlers(value);
        }

        /// <summary>
        /// Report progress on the promise.
        /// </summary>
        public void ReportProgress(in float progress)
        {
            if (State != PromiseState.Pending)
            {
                throw new PromiseStateException(
                    "Attempt to report progress on a promise that is already in state: "
                    + State + ", a promise can only report progress when it is still in state: "
                    + PromiseState.Pending
                );
            }

            InvokeProgressHandlers(progress);
        }

        /// <summary>
        /// Completes the promise. 
        /// onResolved is called on successful completion.
        /// onRejected is called on error.
        /// </summary>
        public void Done(Action<PromisedT> onResolved, Action<Exception> onRejected)
        {
            Then(onResolved, onRejected)
                .Catch(ex =>
                    PropagateUnhandledException(this, ex)
                );
        }

        /// <summary>
        /// Completes the promise. 
        /// onResolved is called on successful completion.
        /// Adds a default error handler.
        /// </summary>
        public void Done(Action<PromisedT> onResolved)
        {
            Then(onResolved)
                .Catch(ex =>
                    PropagateUnhandledException(this, ex)
                );
        }

        /// <summary>
        /// Complete the promise. Adds a default error handler.
        /// </summary>
        public void Done()
        {
            if (State == PromiseState.Resolved)
            {
                return;
            }

            Catch(ex =>
                PropagateUnhandledException(this, ex)
            );
        }

        /// <summary>
        /// Set the name of the promise, useful for debugging.
        /// </summary>
        public IPromise<PromisedT> WithName(in string name)
        {
            Name = name;
            return this;
        }

        /// <summary>
        /// Handle errors for the promise. 
        /// </summary>
        public IPromise Catch(Action<Exception> onRejected)
        {
            if (State == PromiseState.Resolved)
            {
                return Promise.Resolved();
            }

            var resultPromise = new Promise();
            resultPromise.WithName(Name);

            void resolveHandler(PromisedT _) => resultPromise.Resolve();

            void rejectHandler(Exception ex)
            {
                try
                {
                    onRejected(ex);
                    resultPromise.Resolve();
                } catch (Exception cbEx)
                {
                    resultPromise.Reject(cbEx);
                }
            }

            ActionHandlers(resultPromise, resolveHandler, rejectHandler);
            ProgressHandlers(resultPromise, v => resultPromise.ReportProgress(v));

            return resultPromise;
        }

        /// <summary>
        /// Handle errors for the promise.
        /// </summary>
        public IPromise<PromisedT> Catch(Func<Exception, PromisedT> onRejected)
        {
            if (State == PromiseState.Resolved)
            {
                return this;
            }

            var resultPromise = new Promise<PromisedT>();
            resultPromise.WithName(Name);

            void resolveHandler(PromisedT v) => resultPromise.Resolve(v);

            void rejectHandler(Exception ex)
            {
                try
                {
                    resultPromise.Resolve(onRejected(ex));
                } catch (Exception cbEx)
                {
                    resultPromise.Reject(cbEx);
                }
            }

            ActionHandlers(resultPromise, resolveHandler, rejectHandler);
            ProgressHandlers(resultPromise, v => resultPromise.ReportProgress(v));

            return resultPromise;
        }

        /// <summary>
        /// Add a resolved callback that chains a value promise (optionally converting to a different value type).
        /// </summary>
        public IPromise<ConvertedT> Then<ConvertedT>(Func<PromisedT, IPromise<ConvertedT>> onResolved) => Then(onResolved, null, null);

        /// <summary>
        /// Add a resolved callback that chains a non-value promise.
        /// </summary>
        public IPromise Then(Func<PromisedT, IPromise> onResolved) => Then(onResolved, null, null);

        /// <summary>
        /// Add a resolved callback.
        /// </summary>
        public IPromise Then(Action<PromisedT> onResolved) => Then(onResolved, null, null);

        /// <summary>
        /// Add a resolved callback and a rejected callback.
        /// The resolved callback chains a value promise (optionally converting to a different value type).
        /// </summary>
        public IPromise<ConvertedT> Then<ConvertedT>(Func<PromisedT, IPromise<ConvertedT>> onResolved, Func<Exception, IPromise<ConvertedT>> onRejected) => Then(onResolved, onRejected, null);

        /// <summary>
        /// Add a resolved callback and a rejected callback.
        /// The resolved callback chains a non-value promise.
        /// </summary>
        public IPromise Then(Func<PromisedT, IPromise> onResolved, Action<Exception> onRejected) => Then(onResolved, onRejected, null);

        /// <summary>
        /// Add a resolved callback and a rejected callback.
        /// </summary>
        public IPromise Then(Action<PromisedT> onResolved, Action<Exception> onRejected) => Then(onResolved, onRejected, null);

        /// <summary>
        /// Add a resolved callback, a rejected callback and a progress callback.
        /// The resolved callback chains a value promise (optionally converting to a different value type).
        /// </summary>
        public IPromise<ConvertedT> Then<ConvertedT>(Func<PromisedT, IPromise<ConvertedT>> onResolved, Func<Exception, IPromise<ConvertedT>> onRejected, Action<float> onProgress)
        {
            if (State == PromiseState.Resolved)
            {
                try
                {
                    return onResolved(resolveValue);
                } catch (Exception ex)
                {
                    return Promise<ConvertedT>.Rejected(ex);
                }
            }

            // This version of the function must supply an onResolved.
            // Otherwise there is now way to get the converted value to pass to the resulting promise.

            var resultPromise = new Promise<ConvertedT>();
            resultPromise.WithName(Name);

            void resolveHandler(PromisedT v)
            {
                onResolved(v)
                    .Progress(progress => resultPromise.ReportProgress(progress))
                    .Then(
                        // Should not be necessary to specify the arg type on the next line, but Unity (mono) has an internal compiler error otherwise.
                        chainedValue => resultPromise.Resolve(chainedValue),
                        ex => resultPromise.Reject(ex)
                    );
            }

            void rejectHandler(Exception ex)
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
                } catch (Exception callbackEx)
                {
                    resultPromise.Reject(callbackEx);
                }
            }

            ActionHandlers(resultPromise, resolveHandler, rejectHandler);
            if (onProgress != null)
            {
                ProgressHandlers(this, onProgress);
            }

            return resultPromise;
        }

        /// <summary>
        /// Add a resolved callback, a rejected callback and a progress callback.
        /// The resolved callback chains a non-value promise.
        /// </summary>
        public IPromise Then(Func<PromisedT, IPromise> onResolved, Action<Exception> onRejected, Action<float> onProgress)
        {
            if (State == PromiseState.Resolved)
            {
                try
                {
                    return onResolved(resolveValue);
                } catch (Exception ex)
                {
                    return Promise.Rejected(ex);
                }
            }

            var resultPromise = new Promise();
            resultPromise.WithName(Name);

            void resolveHandler(PromisedT v)
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

            void rejectHandler(Exception ex)
            {
                onRejected?.Invoke(ex);
                resultPromise.Reject(ex);
            }

            ActionHandlers(resultPromise, resolveHandler, rejectHandler);
            if (onProgress != null)
            {
                ProgressHandlers(this, onProgress);
            }

            return resultPromise;
        }

        /// <summary>
        /// Add a resolved callback, a rejected callback and a progress callback.
        /// </summary>
        public IPromise Then(Action<PromisedT> onResolved, Action<Exception> onRejected, Action<float> onProgress)
        {
            if (State == PromiseState.Resolved)
            {
                try
                {
                    onResolved(resolveValue);
                    return Promise.Resolved();
                } catch (Exception ex)
                {
                    return Promise.Rejected(ex);
                }
            }

            var resultPromise = new Promise();
            resultPromise.WithName(Name);

            void resolveHandler(PromisedT v)
            {
                onResolved?.Invoke(v);
                resultPromise.Resolve();
            }

            void rejectHandler(Exception ex)
            {
                onRejected?.Invoke(ex);
                resultPromise.Reject(ex);
            }

            ActionHandlers(resultPromise, resolveHandler, rejectHandler);
            if (onProgress != null)
            {
                ProgressHandlers(this, onProgress);
            }

            return resultPromise;
        }

        /// <summary>
        /// Return a new promise with a different value.
        /// May also change the type of the value.
        /// </summary>
        public IPromise<ConvertedT> Then<ConvertedT>(Func<PromisedT, ConvertedT> transform) => Then(value => Promise<ConvertedT>.Resolved(transform(value)));

        /// <summary>
        /// Helper function to invoke or register resolve/reject handlers.
        /// </summary>
        private void ActionHandlers(IRejectable resultPromise, Action<PromisedT> resolveHandler, Action<Exception> rejectHandler)
        {
            if (State == PromiseState.Resolved)
            {
                InvokeHandler(resolveHandler, resultPromise, resolveValue);
            } else if (State == PromiseState.Rejected)
            {
                InvokeHandler(rejectHandler, resultPromise, rejectionException);
            } else
            {
                AddResolveHandler(resolveHandler, resultPromise);
                AddRejectHandler(rejectHandler, resultPromise);
            }
        }

        /// <summary>
        /// Helper function to invoke or register progress handlers.
        /// </summary>
        private void ProgressHandlers(IRejectable resultPromise, Action<float> progressHandler)
        {
            if (State == PromiseState.Pending)
            {
                AddProgressHandler(progressHandler, resultPromise);
            }
        }

        /// <summary>
        /// Chain a number of operations using promises.
        /// Returns the value of the first promise that resolves, or otherwise the exception thrown by the last operation.
        /// </summary>
        public static IPromise<T> First<T>(params Func<IPromise<T>>[] fns) => First((IEnumerable<Func<IPromise<T>>>)fns);

        /// <summary>
        /// Chain a number of operations using promises.
        /// Returns the value of the first promise that resolves, or otherwise the exception thrown by the last operation.
        /// </summary>
        public static IPromise<T> First<T>(IEnumerable<Func<IPromise<T>>> fns)
        {
            var promise = new Promise<T>();

            int count = 0;

            fns.Aggregate(
                Promise<T>.Rejected(null),
                (prevPromise, fn) =>
                {
                    int itemSequence = count;
                    count++;

                    var newPromise = new Promise<T>();
                    prevPromise
                        .Progress(v =>
                        {
                            var sliceLength = 1.0f / count;
                            promise.ReportProgress(sliceLength * (v + itemSequence));
                        })
                        .Then(newPromise.Resolve)
                        .Catch(ex =>
                        {
                            var sliceLength = 1.0f / count;
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
            .Catch(ex =>
            {
                promise.ReportProgress(1.0f);
                promise.Reject(ex);
            });

            return promise;
        }

        /// <summary>
        /// Chain an enumerable of promises, all of which must resolve.
        /// Returns a promise for a collection of the resolved results.
        /// The resulting promise is resolved when all of the promises have resolved.
        /// It is rejected as soon as any of the promises have been rejected.
        /// </summary>
        public IPromise<IEnumerable<ConvertedT>> ThenAll<ConvertedT>(Func<PromisedT, IEnumerable<IPromise<ConvertedT>>> chain) => Then(value => Promise<ConvertedT>.All(chain(value)));

        /// <summary>
        /// Chain an enumerable of promises, all of which must resolve.
        /// Converts to a non-value promise.
        /// The resulting promise is resolved when all of the promises have resolved.
        /// It is rejected as soon as any of the promises have been rejected.
        /// </summary>
        public IPromise ThenAll(Func<PromisedT, IEnumerable<IPromise>> chain) => Then(value => Promise.All(chain(value)));

        /// <summary>
        /// Returns a promise that resolves when all of the promises in the enumerable argument have resolved.
        /// Returns a promise of a collection of the resolved results.
        /// </summary>
        public static IPromise<IEnumerable<PromisedT>> All(params IPromise<PromisedT>[] promises) => All((IEnumerable<IPromise<PromisedT>>)promises); // Cast is required to force use of the other All function.

        /// <summary>
        /// Returns a promise that resolves when all of the promises in the enumerable argument have resolved.
        /// Returns a promise of a collection of the resolved results.
        /// </summary>
        public static IPromise<IEnumerable<PromisedT>> All(IEnumerable<IPromise<PromisedT>> promises)
        {
            var promisesArray = promises.ToArray();
            if (promisesArray.Length == 0)
            {
                return Promise<IEnumerable<PromisedT>>.Resolved(Enumerable.Empty<PromisedT>());
            }

            var remainingCount = promisesArray.Length;
            var results = new PromisedT[remainingCount];
            var progress = new float[remainingCount];
            var resultPromise = new Promise<IEnumerable<PromisedT>>();
            resultPromise.WithName("All");

            for (int i = 0; i < promisesArray.Length; i++)
            {
                promisesArray[i]
                    .Progress(v =>
                    {
                        progress[i] = v;
                        if (resultPromise.State == PromiseState.Pending)
                        {
                            resultPromise.ReportProgress(progress.Average());
                        }
                    })
                    .Then(result =>
                    {
                        progress[i] = 1.0f;
                        results[i] = result;
                        remainingCount--;
                        if (remainingCount <= 0 && resultPromise.State == PromiseState.Pending)
                        {
                            // This will never happen if any of the promises errorred.
                            resultPromise.Resolve(results);
                        }
                    })
                    .Catch(ex =>
                    {
                        if (resultPromise.State == PromiseState.Pending)
                        {
                            // If a promise errorred and the result promise is still pending, reject it.
                            resultPromise.Reject(ex);
                        }
                    })
                    .Done();
            }

            return resultPromise;
        }

        /// <summary>
        /// Takes a function that yields an enumerable of promises.
        /// Returns a promise that resolves when the first of the promises has resolved.
        /// Yields the value from the first promise that has resolved.
        /// </summary>
        public IPromise<ConvertedT> ThenRace<ConvertedT>(Func<PromisedT, IEnumerable<IPromise<ConvertedT>>> chain) => Then(value => Promise<ConvertedT>.Race(chain(value)));

        /// <summary>
        /// Takes a function that yields an enumerable of promises.
        /// Converts to a non-value promise.
        /// Returns a promise that resolves when the first of the promises has resolved.
        /// Yields the value from the first promise that has resolved.
        /// </summary>
        public IPromise ThenRace(Func<PromisedT, IEnumerable<IPromise>> chain) => Then(value => Promise.Race(chain(value)));

        /// <summary>
        /// Returns a promise that resolves when the first of the promises in the enumerable argument have resolved.
        /// Returns the value from the first promise that has resolved.
        /// </summary>
        public static IPromise<PromisedT> Race(params IPromise<PromisedT>[] promises) => Race((IEnumerable<IPromise<PromisedT>>)promises); // Cast is required to force use of the other function.

        /// <summary>
        /// Returns a promise that resolves when the first of the promises in the enumerable argument have resolved.
        /// Returns the value from the first promise that has resolved.
        /// </summary>
        public static IPromise<PromisedT> Race(IEnumerable<IPromise<PromisedT>> promises)
        {
            var promisesArray = promises.ToArray();
            if (promisesArray.Length == 0)
            {
                throw new InvalidOperationException(
                    "At least 1 input promise must be provided for Race"
                );
            }

            var resultPromise = new Promise<PromisedT>();
            resultPromise.WithName("Race");

            var progress = new float[promisesArray.Length];

            for (int i = 0; i < promisesArray.Length; i++)
            {
                promisesArray[i]
                    .Progress(v =>
                    {
                        if (resultPromise.State == PromiseState.Pending)
                        {
                            progress[i] = v;
                            resultPromise.ReportProgress(progress.Max());
                        }
                    })
                    .Then(result =>
                    {
                        if (resultPromise.State == PromiseState.Pending)
                        {
                            resultPromise.Resolve(result);
                        }
                    })
                    .Catch(ex =>
                    {
                        if (resultPromise.State == PromiseState.Pending)
                        {
                            // If a promise errorred and the result promise is still pending, reject it.
                            resultPromise.Reject(ex);
                        }
                    })
                    .Done();
            }

            return resultPromise;
        }

        /// <summary>
        /// Creates and returns a new promise.
        /// </summary>
        public static IPromise<PromisedT> Create()
        {
            return new Promise<PromisedT>();
        }

        /// <summary>
        /// Convert a simple value directly into a resolved promise.
        /// </summary>
        public static IPromise<PromisedT> Resolved(PromisedT promisedValue)
        {
            var promise = new Promise<PromisedT>(PromiseState.Resolved)
            {
                resolveValue = promisedValue
            };
            return promise;
        }

        /// <summary>
        /// Convert an exception directly into a rejected promise.
        /// </summary>
        public static IPromise<PromisedT> Rejected(Exception ex)
        {
            var promise = new Promise<PromisedT>(PromiseState.Rejected)
            {
                rejectionException = ex
            };
            return promise;
        }

        public IPromise<PromisedT> Finally(Action onComplete)
        {
            if (State == PromiseState.Resolved)
            {
                try
                {
                    onComplete();
                    return this;
                } catch (Exception ex)
                {
                    return Rejected(ex);
                }
            }

            var promise = new Promise<PromisedT>();
            promise.WithName(Name);

            Then(promise.Resolve);
            Catch(e => {
                try
                {
                    onComplete();
                    promise.Reject(e);
                } catch (Exception ne)
                {
                    promise.Reject(ne);
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
            var promise = new Promise();
            promise.WithName(Name);

            Then(x => promise.Resolve());
            Catch(e => promise.Resolve());

            return promise.Then(onComplete);
        }

        public IPromise<ConvertedT> ContinueWith<ConvertedT>(Func<IPromise<ConvertedT>> onComplete)
        {
            var promise = new Promise();
            promise.WithName(Name);

            Then(x => promise.Resolve());
            Catch(e => promise.Resolve());

            return promise.Then(onComplete);
        }

        public IPromise<PromisedT> Progress(Action<float> onProgress)
        {
            if (State == PromiseState.Pending && onProgress != null)
            {
                ProgressHandlers(this, onProgress);
            }
            return this;
        }
    }
}
