using System;
using System.Collections.Generic;

namespace Vulpes.Promises
{
    public class ExceptionEventArgs : EventArgs
    {
        internal ExceptionEventArgs(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; private set; }
    }

    public abstract class AbstractPromise : IPromiseInfo
    {
        protected readonly uint id;

        public uint Id => id;

        public string Name { get; protected set; }

        public PromiseState State { get; protected set; }

        protected static uint nextPromiseId;

        internal static uint NextId() => nextPromiseId++;

        public static bool enablePromiseTracking = false;

        protected static EventHandler<ExceptionEventArgs> unhandlerException;

        public static event EventHandler<ExceptionEventArgs> UnhandledException
        {
            add => unhandlerException += value;
            remove => unhandlerException -= value;
        }

        internal static readonly HashSet<IPromiseInfo> pendingPromises = new();

        public bool IsPending => State == PromiseState.Pending;

        public bool IsRejected => State == PromiseState.Rejected;

        public bool IsResolved => State == PromiseState.Resolved;

        public static IEnumerable<IPromiseInfo> GetPendingPromises() 
            => pendingPromises;

        internal static void PropagateUnhandledException(object sender, Exception ex) 
            => unhandlerException?.Invoke(sender, new(ex));

        public AbstractPromise()
        {
            State = PromiseState.Pending;
            id = NextId();
        }
    }
}