using System;

namespace Vulpes.Promises
{
    public abstract class PromiseException : Exception
    {
        public PromiseException() { }

        public PromiseException(string message) : base(message) { }

        public PromiseException(string message, Exception inner) : base(message, inner) { }
    }
}