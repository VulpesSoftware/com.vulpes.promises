using System;

namespace Vulpes.Promises
{
    /// <summary>
    /// This <see cref="Exception"/> is thrown upon cancellation of a <see cref="IPromise"/>.
    /// </summary>
    public sealed class PromiseCancelledException : PromiseException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PromiseCancelledException"/> class.
        /// </summary>
        public PromiseCancelledException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PromiseCancelledException"/> class with a specified error message.
        /// </summary>
        public PromiseCancelledException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PromiseCancelledException"/> class with a specified error message 
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        public PromiseCancelledException(string message, Exception inner) : base(message, inner) { }
    }
}
