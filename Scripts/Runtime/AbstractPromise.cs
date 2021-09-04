using System;

namespace Vulpes.Promises
{
    public abstract class AbstractPromise
    {
        protected bool isDone;
        protected bool recycled;
        protected Action<Exception> exceptionHandler;
        protected Exception exception;

        public string Name { get; protected set; }

        [Obsolete("This property is obsolete. Use 'State' instead.", false)]
        public PromiseState PromiseState => State;

        public PromiseState State { get; protected set; }

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

        protected bool TryReject(Exception exception)
        {
            if (this.exception != null)
            {
                throw new PromiseStateException("Vulpes.Promises.AbstractPromise.TryReject: Cannot Reject Promise becuase multiple Exceptions were encountered!");
            }
            this.exception = exception;
            return TryReject();
        }
    }
}
