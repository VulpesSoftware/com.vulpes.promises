using System;

namespace Vulpes.Promises
{
    /// <summary>
    /// This <see cref="Exception"/> is thrown when attempting to change the
    /// state of a <see cref="IPromise"/> to an invalid <see cref="PromiseState"/>.
    /// </summary>
    public sealed class PromiseStateException : PromiseException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PromiseStateException"/> class.
        /// </summary>
        public PromiseStateException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PromiseStateException"/> class with a specified error message.
        /// </summary>
        public PromiseStateException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PromiseStateException"/> class with a specified error message 
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        public PromiseStateException(string message, Exception inner) : base(message, inner) { }
    }
}
