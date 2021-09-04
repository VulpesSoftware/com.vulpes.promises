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
                promise.State = PromiseState.Pending;
                promise.recycled = false;
                return promise;
            }
        }

        internal static Promise Create(Action<Exception> exceptionHandler)
        {
            var promise = Create();
            promise.exceptionHandler = exceptionHandler;
            return promise;
        }

        public static IPromise Resolved()
        {
            var promise = Create();
            promise.Resolve();
            return promise;
        }

        public static IPromise Rejected(Exception exception)
        {
            var promise = Create();
            promise.Reject(exception);
            return promise;
        }

        public IPromise WithName(string name)
        {
            Name = name;
            return this;
        }

        public void Reject(Exception exception)
        {
            if (State != PromiseState.Pending)
            {
                throw new PromiseStateException($"Vulpes.Promises.Promise.Reject: Cannot Reject Promise {ToString()} (State: {State})");
            }
            State = PromiseState.Rejected;
            this.exception = exception;
            if (TryReject())
            {
                this.exception = null;
                exceptionHandler = null;
            } else if (isDone)
            {
                throw exception;
            }
        }

        public void Resolve()
        {
            if (State != PromiseState.Pending)
            {
                throw new PromiseStateException($"Vulpes.Promises.Promise.Resolve: Cannot Resolve Promise {ToString()} (State: {State})");
            }
            State = PromiseState.Resolved;
            if (!isDone)
            {
                return;
            }
            if (TryResolve())
            {
                Recycle();
            }
        }

        public IPromise Catch(Action<Exception> onRejected)
        {
            exceptionHandler = onRejected;
            if (State != PromiseState.Rejected)
            {
                return this;
            }
            if (!TryReject())
            {
                return this;
            }
            Recycle();
            var p = Create();
            p.State = PromiseState.Rejected;
            return p;
        }

        public void Done()
        {
            isDone = true;
            switch (State)
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

        public void Done(Action onResolved)
        {
            resolveHandlers.Add(onResolved);
            Done();
        }

        public IPromise Then(Action onResolved)
        {
            var p = Create(exceptionHandler);
            Done(() =>
            {
                onResolved();
                p.Resolve();
            });
            return p;
        }

        public IPromise Then(Func<IPromise> onResolved)
        {
            var p = Create(exceptionHandler);
            Done(() =>
            {
                onResolved().Catch(exceptionHandler).Done(p.Resolve);
            });
            return p;
        }

        public IPromise<ConvertedT> Then<ConvertedT>(Func<IPromise<ConvertedT>> onResolved)
        {
            var p = Promise<ConvertedT>.Create(exceptionHandler);
            Done(() =>
            {
                onResolved().Catch(exceptionHandler).Done(v => p.Resolve(v));
            });
            return p;
        }
        
        protected internal override void Recycle()
        {
            recycled = true;
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
                promise.State = PromiseState.Pending;
                promise.recycled = false;
                return promise;
            }
        }

        internal static Promise<PromisedT> Create(Action<Exception> exceptionHandler)
        {
            var promise = Create();
            promise.exceptionHandler = exceptionHandler;
            return promise;
        }

        public static IPromise<PromisedT> Resolved(PromisedT value)
        {
            var promise = Create();
            promise.Resolve(value);
            return promise;
        }

        public static IPromise<PromisedT> Rejected(Exception exception)
        {
            var promise = Create();
            promise.Reject(exception);
            return promise;
        }

        public IPromise<PromisedT> WithName(string name)
        {
            Name = name;
            return this;
        }

        public void Reject(Exception exception)
        {
            if (State != PromiseState.Pending)
            {
                throw new PromiseStateException($"Vulpes.Promises.Promise.Reject: Cannot Reject Promise {ToString()} (State: {State})");
            }
            State = PromiseState.Rejected;
            this.exception = exception;
            if (TryReject())
            {
                this.exception = null;
                exceptionHandler = null;
            } else if (isDone)
            {
                throw exception;
            }
        }

        public void Resolve(PromisedT value)
        {
            if (State != PromiseState.Pending)
            {
                throw new PromiseStateException($"Vulpes.Promises.Promise.Resolve: Cannot Resolve Promise {ToString()} (State: {State})");
            }
            resolveValue = value;
            State = PromiseState.Resolved;
            if (!isDone)
            {
                return;
            }
            if (TryResolve())
            {
                Recycle();
            }
        }

        public IPromise<PromisedT> Catch(Action<Exception> onRejected)
        {
            exceptionHandler = onRejected;
            if (State != PromiseState.Rejected)
            {
                return this;
            }
            if (!TryReject())
            {
                return this;
            }
            Recycle();
            var p = Create();
            p.State = PromiseState.Rejected;
            return p;
        }

        public void Done()
        {
            isDone = true;
            switch (State)
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

        public void Done(Action onResolved)
        {
            resolveHandlers.Add(v => { onResolved(); });
            Done();
        }

        public void Done(Action<PromisedT> onResolved)
        {
            resolveHandlers.Add(onResolved);
            Done();
        }

        public IPromise Then(Action onResolved)
        {
            var p = Promise.Create(exceptionHandler);
            Done(v =>
            {
                try
                {
                    onResolved();
                    p.Resolve();
                }
                catch (Exception e)
                {
                    p.Reject(e);
                }
            });
            return p;
        }

        public IPromise<PromisedT> Then(Action<PromisedT> onResolved)
        {
            var p = Create(exceptionHandler);
            Done(v =>
            {
                try
                {
                    onResolved(v);
                    p.Resolve(v);
                }
                catch (Exception e)
                {
                    p.Reject(e);
                }
            });
            return p;
        }

        public IPromise Then(Func<PromisedT, IPromise> onResolved)
        {
            var p = Promise.Create(exceptionHandler);
            Done(v =>
            {
                try
                {
                    onResolved(v).Done(p.Resolve);
                }
                catch (Exception e)
                {
                    p.Reject(e);
                }
            });
            return p;
        }

        public IPromise<PromisedT> Then(Func<IPromise> onResolved)
        {
            var p = Create(exceptionHandler);
            Done(v =>
            {
                try
                {
                    onResolved().Done(() => { p.Resolve(v); });
                }
                catch (Exception e)
                {
                    p.Reject(e);
                }
            });
            return p;
        }

        public IPromise<PromisedT> Then(Func<IPromise<PromisedT>> onResolved)
        {
            var p = Create(exceptionHandler);
            Done(v =>
            {
                try
                {
                    onResolved().Done(p.Resolve);
                }
                catch (Exception e)
                {
                    p.Reject(e);
                }
            });
            return p;
        }

        public IPromise<PromisedT> Then(Func<PromisedT, IPromise<PromisedT>> onResolved)
        {
            var p = Create(exceptionHandler);
            Done(v =>
            {
                try
                {
                    onResolved(v).Done(p.Resolve);
                }
                catch (Exception e)
                {
                    p.Reject(e);
                }
            });
            return p;
        }
        
        public IPromise<ConvertedT> Then<ConvertedT>(Func<PromisedT, ConvertedT> onResolved)
        {
            var p = Promise<ConvertedT>.Create(exceptionHandler);
            Done(v =>
            {
                try
                {
                    p.Resolve(onResolved(v));
                }
                catch (Exception e)
                {
                    p.Reject(e);
                }
            });
            return p;
        }

        public IPromise<ConvertedT> Then<ConvertedT>(Func<PromisedT, IPromise<ConvertedT>> onResolved)
        {
            var p = Promise<ConvertedT>.Create(exceptionHandler);
            Done(v =>
            {
                try
                {
                    onResolved(v).Done(p.Resolve);
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
            if (recycled)
            {
                return;
            }
            recycled = true;
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
