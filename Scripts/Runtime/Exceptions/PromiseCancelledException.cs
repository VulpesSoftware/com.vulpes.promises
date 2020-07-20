using System;

namespace Vulpes.Promises
{
    public sealed class PromiseCancelledException : PromiseException
    {
        public PromiseCancelledException() { }

        public PromiseCancelledException(string message) : base(message) { }

        public PromiseCancelledException(string message, Exception inner) : base(message, inner) { }
    }
}
