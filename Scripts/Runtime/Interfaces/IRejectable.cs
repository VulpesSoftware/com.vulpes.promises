using System;

namespace Vulpes.Promises
{
    /// <summary>
    /// Interface for a <see cref="IPromise"/> that can be rejected.
    /// </summary>
    public interface IRejectable
    {
        /// <summary>
        /// ID for the <see cref="IPromise"/>, useful for debugging.
        /// </summary>
        uint Id { get; }

        /// <summary>
        /// Reject the <see cref="IPromise"/> with an Exception.
        /// </summary>
        /// <param name="exception"></param>
        void Reject(Exception exception);
    }
}
