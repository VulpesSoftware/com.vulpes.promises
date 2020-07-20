using System;

namespace Vulpes.Promises
{
    public abstract class AbstractPromise 
    {
        protected bool isDone;
        protected bool hasBeenRecycled;
        protected Action<Exception> exceptionHandler;
        protected Exception exception;

        public string Name { get; protected set; }

        public PromiseState PromiseState { get; protected set; }

        protected internal abstract void Recycle();

        protected bool TryReject()
        {
            if (exception == null || exceptionHandler == null)
            {
                return false;
            }
            exceptionHandler(exception);
            return true;
        }

        protected bool TryReject(Exception akException)
        {
            if (exception != null)
            {
                throw new PromiseStateException("Vulpes.Promises.AbstractPromise.TryReject: Cannot Reject Promise becuase multiple Exceptions were encountered!");
            }
            exception = akException;
            return TryReject();
        }
    }
}
