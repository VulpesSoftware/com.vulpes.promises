using System;
using System.Collections.Generic;

namespace Vulpes.Promises
{
    public sealed class Promise : AbstractPromise, IPromise
    {
        private static readonly object staticLock = new object();
        private static uint promiseCounter;
        private static readonly Queue<Promise> pool;
        private readonly List<Action> resolveHandlers;

        public uint Id { get; }

        static Promise()
        {
            pool = new Queue<Promise>(128);
            for (int i = 0; i < 128; i++)
            {
                pool.Enqueue(new Promise());
            }
        }

        private Promise()
        {
            Id = promiseCounter++;
            resolveHandlers = new List<Action>();
        }

        private bool TryResolve()
        {
            try
            {
                foreach (var resolveHandler in resolveHandlers)
                {
                    resolveHandler();
                }
                resolveHandlers.Clear();
                return true;
            }
            catch (Exception e)
            {
                if (!TryReject(e))
                {
                    throw;
                }
            }
            return false;
        }

        public static Promise Create()
        {
            lock (staticLock)
            {
                var promise = pool.Count > 0 ? pool.Dequeue() : new Promise();
                promise.PromiseState = PromiseState.Pending;
                promise.hasBeenRecycled = false;
                return promise;
            }
        }

        internal static Promise Create(Action<Exception> akExceptionHandler)
        {
            var promise = Create();
            promise.exceptionHandler = akExceptionHandler;
            return promise;
        }

        public static IPromise Resolved()
        {
            var promise = Create();
            promise.Resolve();
            return promise;
        }

        public static IPromise Rejected(Exception akException)
        {
            var promise = Create();
            promise.Reject(akException);
            return promise;
        }

        public IPromise WithName(string asName)
        {
            Name = asName;
            return this;
        }

        public void Reject(Exception akException)
        {
            if (PromiseState != PromiseState.Pending)
            {
                throw new PromiseStateException($"Vulpes.Promises.Promise.Reject: Cannot Reject Promise {ToString()} (State: {PromiseState.ToString()})");
            }
            PromiseState = PromiseState.Rejected;
            exception = akException;
            if (TryReject())
            {
                exception = null;
                exceptionHandler = null;
            } else if (isDone)
            {
                throw akException;
            }
        }

        public void Resolve()
        {
            if (PromiseState != PromiseState.Pending)
            {
                throw new PromiseStateException($"Vulpes.Promises.Promise.Resolve: Cannot Resolve Promise {ToString()} (State: {PromiseState.ToString()})");
            }
            PromiseState = PromiseState.Resolved;
            if (!isDone)
            {
                return;
            }
            if (TryResolve())
            {
                Recycle();
            }
        }

        public IPromise Catch(Action<Exception> akOnRejected)
        {
            exceptionHandler = akOnRejected;
            if (PromiseState != PromiseState.Rejected)
            {
                return this;
            }
            if (!TryReject())
            {
                return this;
            }
            Recycle();
            var p = Create();
            p.PromiseState = PromiseState.Rejected;
            return p;
        }

        public void Done()
        {
            isDone = true;
            switch (PromiseState)
            {
                case PromiseState.Pending:
                    return;
                case PromiseState.Rejected:
                    if (!TryReject() && exception != null)
                    {
                        throw exception;
                    }
                    Recycle();
                    break;
                case PromiseState.Resolved:
                    TryResolve();
                    Recycle();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Done(Action akAction)
        {
            resolveHandlers.Add(akAction);
            Done();
        }

        public IPromise Then(Action akAction)
        {
            var p = Create(exceptionHandler);
            Done(() =>
            {
                akAction();
                p.Resolve();
            });
            return p;
        }

        public IPromise Then(Func<IPromise> akPromise)
        {
            var p = Create(exceptionHandler);
            Done(() =>
            {
                akPromise().Catch(exceptionHandler).Done(p.Resolve);
            });
            return p;
        }

        public IPromise<ConvertedT> Then<ConvertedT>(Func<IPromise<ConvertedT>> akPromise)
        {
            var p = Promise<ConvertedT>.Create(exceptionHandler);
            Done(() =>
            {
                akPromise().Catch(exceptionHandler).Done(v => p.Resolve(v));
            });
            return p;
        }
        
        protected internal override void Recycle()
        {
            hasBeenRecycled = true;
            Name = string.Empty;
            isDone = false;
            resolveHandlers.Clear();
            exceptionHandler = null;
            exception = null;
            lock (staticLock)
            {
                pool.Enqueue(this);
            }
        }
    }

    public sealed class Promise<PromisedT> : AbstractPromise, IPromise<PromisedT>
    {
        private static readonly object staticLock = new object();
        private static uint promiseCounter;
        private static readonly Queue<Promise<PromisedT>> pool;
        private readonly List<Action<PromisedT>> resolveHandlers;
        private PromisedT resolveValue;

        public uint Id { get; }

        static Promise()
        {
            pool = new Queue<Promise<PromisedT>>(32);
            for (int i = 0; i < 32; i++)
            {
                pool.Enqueue(new Promise<PromisedT>());
            }
        }

        private Promise()
        {
            Id = promiseCounter++;
            resolveHandlers = new List<Action<PromisedT>>();
        }

        private bool TryResolve()
        {
            try
            {
                foreach (var resolveHandler in resolveHandlers)
                {
                    resolveHandler(resolveValue);
                }
                resolveHandlers.Clear();
                return true;
            }
            catch (Exception e)
            {
                if (!TryReject(e))
                {
                    throw;
                }
            }
            return false;
        }

        public static Promise<PromisedT> Create()
        {
            lock (staticLock)
            {
                var promise = pool.Count > 0 ? pool.Dequeue() : new Promise<PromisedT>();
                promise.PromiseState = PromiseState.Pending;
                promise.hasBeenRecycled = false;
                return promise;
            }
        }

        internal static Promise<PromisedT> Create(Action<Exception> akExceptionHandler)
        {
            var promise = Create();
            promise.exceptionHandler = akExceptionHandler;
            return promise;
        }

        public static IPromise<PromisedT> Resolved(PromisedT akValue)
        {
            var promise = Create();
            promise.Resolve(akValue);
            return promise;
        }

        public static IPromise<PromisedT> Rejected(Exception akException)
        {
            var promise = Create();
            promise.Reject(akException);
            return promise;
        }

        public IPromise<PromisedT> WithName(string asName)
        {
            Name = asName;
            return this;
        }

        public void Reject(Exception akException)
        {
            if (PromiseState != PromiseState.Pending)
            {
                throw new PromiseStateException($"Vulpes.Promises.Promise.Reject: Cannot Reject Promise {ToString()} (State: {PromiseState.ToString()})");
            }
            PromiseState = PromiseState.Rejected;
            exception = akException;
            if (TryReject())
            {
                exception = null;
                exceptionHandler = null;
            } else if (isDone)
            {
                throw akException;
            }
        }

        public void Resolve(PromisedT akValue)
        {
            if (PromiseState != PromiseState.Pending)
            {
                throw new PromiseStateException($"Vulpes.Promises.Promise.Resolve: Cannot Resolve Promise {ToString()} (State: {PromiseState.ToString()})");
            }
            resolveValue = akValue;
            PromiseState = PromiseState.Resolved;
            if (!isDone)
            {
                return;
            }
            if (TryResolve())
            {
                Recycle();
            }
        }

        public IPromise<PromisedT> Catch(Action<Exception> akOnRejected)
        {
            exceptionHandler = akOnRejected;
            if (PromiseState != PromiseState.Rejected)
            {
                return this;
            }
            if (!TryReject())
            {
                return this;
            }
            Recycle();
            var p = Create();
            p.PromiseState = PromiseState.Rejected;
            return p;
        }

        public void Done()
        {
            isDone = true;
            switch (PromiseState)
            {
                case PromiseState.Pending:
                    return;
                case PromiseState.Rejected:
                    if (!TryReject() && exception != null)
                    {
                        throw exception;
                    }
                    Recycle();
                    break;
                case PromiseState.Resolved:
                    TryResolve();
                    Recycle();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Done(Action akAction)
        {
            resolveHandlers.Add(v => { akAction(); });
            Done();
        }

        public void Done(Action<PromisedT> akAction)
        {
            resolveHandlers.Add(akAction);
            Done();
        }

        public IPromise Then(Action akAction)
        {
            var p = Promise.Create(exceptionHandler);
            Done(v =>
            {
                try
                {
                    akAction();
                    p.Resolve();
                }
                catch (Exception e)
                {
                    p.Reject(e);
                }
            });
            return p;
        }

        public IPromise<PromisedT> Then(Action<PromisedT> akAction)
        {
            var p = Create(exceptionHandler);
            Done(v =>
            {
                try
                {
                    akAction(v);
                    p.Resolve(v);
                }
                catch (Exception e)
                {
                    p.Reject(e);
                }
            });
            return p;
        }

        public IPromise Then(Func<PromisedT, IPromise> akPromise)
        {
            var p = Promise.Create(exceptionHandler);
            Done(v =>
            {
                try
                {
                    akPromise(v).Done(p.Resolve);
                }
                catch (Exception e)
                {
                    p.Reject(e);
                }
            });
            return p;
        }

        public IPromise<PromisedT> Then(Func<IPromise> akPromise)
        {
            var p = Create(exceptionHandler);
            Done(v =>
            {
                try
                {
                    akPromise().Done(() => { p.Resolve(v); });
                }
                catch (Exception e)
                {
                    p.Reject(e);
                }
            });
            return p;
        }

        public IPromise<PromisedT> Then(Func<IPromise<PromisedT>> akPromise)
        {
            var p = Create(exceptionHandler);
            Done(v =>
            {
                try
                {
                    akPromise().Done(p.Resolve);
                }
                catch (Exception e)
                {
                    p.Reject(e);
                }
            });
            return p;
        }

        public IPromise<PromisedT> Then(Func<PromisedT, IPromise<PromisedT>> akPromise)
        {
            var p = Create(exceptionHandler);
            Done(v =>
            {
                try
                {
                    akPromise(v).Done(p.Resolve);
                }
                catch (Exception e)
                {
                    p.Reject(e);
                }
            });
            return p;
        }
        
        public IPromise<ConvertedT> Then<ConvertedT>(Func<PromisedT, ConvertedT> akAction)
        {
            var p = Promise<ConvertedT>.Create(exceptionHandler);
            Done(v =>
            {
                try
                {
                    p.Resolve(akAction(v));
                }
                catch (Exception e)
                {
                    p.Reject(e);
                }
            });
            return p;
        }

        public IPromise<ConvertedT> Then<ConvertedT>(Func<PromisedT, IPromise<ConvertedT>> akPromise)
        {
            var p = Promise<ConvertedT>.Create(exceptionHandler);
            Done(v =>
            {
                try
                {
                    akPromise(v).Done(p.Resolve);
                }
                catch (Exception e)
                {
                    p.Reject(e);
                }
            });
            return p;
        }

        protected internal override void Recycle()
        {
            if (hasBeenRecycled)
            {
                return;
            }
            hasBeenRecycled = true;
            Name = string.Empty;
            resolveValue = default;
            isDone = false;
            resolveHandlers.Clear();
            exceptionHandler = null;
            exception = null;
            lock (staticLock)
            {
                pool.Enqueue(this);
            }
        }
    }
}
