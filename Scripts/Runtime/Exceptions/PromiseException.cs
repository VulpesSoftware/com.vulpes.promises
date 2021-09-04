using System;

namespace Vulpes.Promises
{
    /// <summary>
    /// Serves as a base class for <see cref="Exception"/>s within the <see cref="Promises"/> namespace.
    /// </summary>
    public class PromiseException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PromiseException"/> class.
        /// </summary>
        public PromiseException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PromiseException"/> class with a specified error message.
        /// </summary>
        public PromiseException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PromiseException"/> class with a specified error message 
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        public PromiseException(string message, Exception inner) : base(message, inner) { }
    }
}
