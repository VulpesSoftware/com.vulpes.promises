using System;

namespace Vulpes.Promises
{
    /// <summary>
    /// Time data specific to a particular pending <see cref="IPromise"/>.
    /// </summary>
    public struct PromiseTimeData
    {
        public float elapsedTime;
        public float deltaTime;
        public int elapsedUpdates;
    }

    /// <summary>
    /// Interface for a <see cref="IPromiseTimer"/> that can return <see cref="IPromise"/>s that resolve over time or when conditions are met.
    /// </summary>
    public interface IPromiseTimer 
    {
        /// <summary>
        /// Resolve the returned <see cref="IPromise"/> once the time has elapsed.
        /// </summary>
        IPromise WaitFor(float seconds);

        /// <summary>
        /// Resolve the returned <see cref="IPromise"/> once the predicate evaluates to true.
        /// </summary>
        IPromise WaitUntil(Func<PromiseTimeData, bool> predicate);

        /// <summary>
        /// Resolve the returned <see cref="IPromise"/> once the predicate evaluates to false.
        /// </summary>
        IPromise WaitWhile(Func<PromiseTimeData, bool> predicate);

        /// <summary>
        /// Repeats the <see cref="IPromise"/> until the predicate evaluates to true.
        /// </summary>
        IPromise RepeatUntil(Func<IPromise> promise, Func<bool> predicate);

        /// <summary>
        /// Cancel a waiting <see cref="IPromise"/> and reject it immediately.
        /// </summary>
        bool Cancel(IPromise promise);

        /// <summary>
        /// Update all pending <see cref="IPromise"/>s. Must be called for the <see cref="IPromise"/>s to progress and resolve at all.
        /// </summary>
        void Update(float deltaTime);
    }
}
