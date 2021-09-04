namespace Vulpes.Promises
{
    /// <summary>
    /// Interface for a <see cref="IPromise"/> that can be rejected or resolved.
    /// </summary>
    public interface IPendingPromise : IRejectable
    {
        /// <summary>
        /// Resolve the <see cref="IPromise"/>.
        /// </summary>
        void Resolve();
    }

    /// <summary>
    /// Interface for a <see cref="IPromise"/> that can be rejected or resolved with a value of type <see cref="PromisedT"/>.
    /// </summary>
    public interface IPendingPromise<PromisedT> : IRejectable
    {
        /// <summary>
        /// Resolve the <see cref="IPromise"/> with a value of type <see cref="PromisedT"/>.
        /// </summary>
        void Resolve(PromisedT value);
    }
}
