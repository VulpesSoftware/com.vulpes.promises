using System;
using System.Collections.Generic;

namespace Vulpes.Promises
{
    /// <summary>
    /// Specifies the state of a promise.
    /// </summary>
    public enum PromiseState : int
    {
        /// <summary>The promise is in-flight.</summary>
        Pending,
        /// <summary>The promise has been rejected.</summary>
        Rejected,
        /// <summary>The promise has been resolved.</summary>
        Resolved,
    };

    /// <summary>
    /// Arguments to the UnhandledError event.
    /// </summary>
    public class ExceptionEventArgs : EventArgs
    {
        internal ExceptionEventArgs(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; private set; }
    }

    /// <summary>
    /// Abstract base promise class for shared functionality.
    /// </summary>
    public abstract class AbstractPromise : IPromiseInfo
    {
        protected readonly uint id;

        public uint Id => id;

        public string Name { get; protected set; }

        /// <summary>
        /// Tracks the current state of the promise.
        /// </summary>
        public PromiseState State { get; protected set; }

        /// <summary>
        /// Id for the next promise that is created.
        /// </summary>
        protected static uint nextPromiseId;

        /// <summary>
        /// Increments the ID counter and gives us the ID for the next promise.
        /// </summary>
        internal static uint NextId() => nextPromiseId++;

        /// <summary>
        /// Set to true to enable tracking of promises.
        /// </summary>
        public static bool enablePromiseTracking = false;

        protected static EventHandler<ExceptionEventArgs> unhandlerException;

        /// <summary>
        /// Event raised for unhandled errors.
        /// For this to work you have to complete your promises with a call to Done().
        /// </summary>
        public static event EventHandler<ExceptionEventArgs> UnhandledException
        {
            add => unhandlerException += value;
            remove => unhandlerException -= value;
        }

        /// <summary>
        /// Information about pending promises.
        /// </summary>
        internal static readonly HashSet<IPromiseInfo> PendingPromises = new();

        /// <summary>
        /// Information about pending promises, useful for debugging.
        /// This is only populated when 'EnablePromiseTracking' is set to true.
        /// </summary>
        public static IEnumerable<IPromiseInfo> GetPendingPromises() => PendingPromises;

        /// <summary>
        /// Raises the UnhandledException event.
        /// </summary>
        internal static void PropagateUnhandledException(object sender, Exception ex) => unhandlerException?.Invoke(sender, new ExceptionEventArgs(ex));

        public AbstractPromise()
        {
            State = PromiseState.Pending;
            id = NextId();
        }
    }
}
