using System;

namespace Vulpes.Promises
{
    /// <summary>
    /// Time data specific to a particular pending promise.
    /// </summary>
    public struct TimeData
    {
        /// <summary>
        /// The amount of time that has elapsed since the pending promise started running
        /// </summary>
        public float elapsedTime;

        /// <summary>
        /// The amount of time since the last time the pending promise was updated.
        /// </summary>
        public float deltaTime;

        /// <summary>
        /// The amount of times that update has been called since the pending promise started running
        /// </summary>
        public int elapsedUpdates;
    }

    public interface IPromiseTimer
    {
        /// <summary>
        /// Resolve the returned promise once the time has elapsed
        /// </summary>
        IPromise WaitFor(float seconds);

        /// <summary>
        /// Resolve the returned promise once the predicate evaluates to true
        /// </summary>
        IPromise WaitUntil(Func<TimeData, bool> predicate);

        /// <summary>
        /// Resolve the returned promise once the predicate evaluates to false
        /// </summary>
        IPromise WaitWhile(Func<TimeData, bool> predicate);

        /// <summary>
        /// Update all pending promises. Must be called for the promises to progress and resolve at all.
        /// </summary>
        void Update(in float deltaTime);

        /// <summary>
        /// Cancel a waiting promise and reject it immediately.
        /// </summary>
        bool Cancel(IPromise promise);
    }
}
