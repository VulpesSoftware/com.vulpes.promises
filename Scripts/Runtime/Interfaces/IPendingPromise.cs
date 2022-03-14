namespace Vulpes.Promises
{
    /// <summary>
    /// Interface for a promise that can be rejected or resolved.
    /// </summary>
    public interface IPendingPromise : IRejectable, IPromiseInfo
    {
        /// <summary>
        /// Resolve the promise with a particular value.
        /// </summary>
        void Resolve();

        /// <summary>
        /// Report progress in a promise.
        /// </summary>
        void ReportProgress(in float progress);
    }

    /// <summary>
    /// Interface for a promise that can be rejected or resolved.
    /// </summary>
    public interface IPendingPromise<PromisedT> : IRejectable, IPromiseInfo
    {
        /// <summary>
        /// Resolve the promise with a particular value.
        /// </summary>
        void Resolve(PromisedT value);

        /// <summary>
        /// Report progress in a promise.
        /// </summary>
        void ReportProgress(in float progress);
    }
}
