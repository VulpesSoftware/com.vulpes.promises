using System;

namespace Vulpes.Promises
{
    /// <summary>
    /// Exception thrown when an operation is performed on a promise that results in its cancellation.
    /// </summary>
    public sealed class PromiseCancelledException : PromiseException
    {
        public PromiseCancelledException() { }

        public PromiseCancelledException(string message) : base(message) { }

        public PromiseCancelledException(string message, Exception inner) : base(message, inner) { }
    }
}
