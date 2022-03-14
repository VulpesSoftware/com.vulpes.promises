namespace Vulpes.Promises
{
    /// <summary>
    /// Used to list information of pending promises.
    /// </summary>
    public interface IPromiseInfo
    {
        /// <summary>
        /// ID of the promise, useful for debugging.
        /// </summary>
        uint Id { get; }

        /// <summary>
        /// Name of the promise, when set, useful for debugging.
        /// </summary>
        string Name { get; }
    }
}
