using System;

namespace Vulpes.Promises
{
    /// <summary>
    /// Base class for <see cref="Promises"/> exceptions.
    /// </summary>
    public class PromiseException : Exception
    {
        public PromiseException() { }

        public PromiseException(string message) : base(message) { }

        public PromiseException(string message, Exception inner) : base(message, inner) { }
    }
}
