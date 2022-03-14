using System;
using System.Collections.Generic;

namespace Vulpes.Promises
{
    /// <summary>
    /// A class that wraps a pending promise with it's predicate and time data
    /// </summary>
    internal sealed class PredicateWait
    {
        /// <summary>
        /// Predicate for resolving the promise
        /// </summary>
        public Func<TimeData, bool> predicate;

        /// <summary>
        /// The time the promise was started
        /// </summary>
        public float timeStarted;

        /// <summary>
        /// The pending promise which is an interface for a promise that can be rejected or resolved.
        /// </summary>
        public IPendingPromise pendingPromise;

        /// <summary>
        /// The time data specific to this pending promise. Includes elapsed time and delta time.
        /// </summary>
        public TimeData timeData;

        /// <summary>
        /// The frame the promise was started
        /// </summary>
        public int frameStarted;
    }

    public class PromiseTimer : IPromiseTimer
    {
        /// <summary>
        /// The current running total for time that this PromiseTimer has run for
        /// </summary>
        private float time;

        /// <summary>
        /// The current running total for the amount of frames the PromiseTimer has run for
        /// </summary>
        private int frame;

        /// <summary>
        /// Currently pending promises
        /// </summary>
        private readonly LinkedList<PredicateWait> waiting = new();

        /// <summary>
        /// Resolve the returned promise once the time has elapsed
        /// </summary>
        public IPromise WaitFor(float seconds)
        {
            return WaitUntil(t => t.elapsedTime >= seconds);
        }

        /// <summary>
        /// Resolve the returned promise once the predicate evaluates to false
        /// </summary>
        public IPromise WaitWhile(Func<TimeData, bool> predicate)
        {
            return WaitUntil(t => !predicate(t));
        }

        /// <summary>
        /// Resolve the returned promise once the predicate evalutes to true
        /// </summary>
        public IPromise WaitUntil(Func<TimeData, bool> predicate)
        {
            Promise promise = new();

            PredicateWait wait = new()
            {
                timeStarted = time,
                pendingPromise = promise,
                timeData = new(),
                predicate = predicate,
                frameStarted = frame
            };

            waiting.AddLast(wait);

            return promise;
        }

        public bool Cancel(IPromise promise)
        {
            var node = FindInWaiting(promise);

            if (node == null)
            {
                return false;
            }

            node.Value.pendingPromise.Reject(new PromiseCancelledException("Promise was cancelled by user."));
            waiting.Remove(node);

            return true;
        }

        LinkedListNode<PredicateWait> FindInWaiting(IPromise promise)
        {
            for (var node = waiting.First; node != null; node = node.Next)
            {
                if (node.Value.pendingPromise.Id.Equals(promise.Id))
                {
                    return node;
                }
            }
            return null;
        }

        /// <summary>
        /// Update all pending promises. Must be called for the promises to progress and resolve at all.
        /// </summary>
        public void Update(in float deltaTime)
        {
            time += deltaTime;
            frame++;

            var node = waiting.First;
            while (node != null)
            {
                var wait = node.Value;
                var newElapsedTime = time - wait.timeStarted;
                wait.timeData.deltaTime = newElapsedTime - wait.timeData.elapsedTime;
                wait.timeData.elapsedTime = newElapsedTime;
                var newElapsedUpdates = frame - wait.frameStarted;
                wait.timeData.elapsedUpdates = newElapsedUpdates;

                bool result;
                try
                {
                    result = wait.predicate(wait.timeData);
                } catch (Exception ex)
                {
                    wait.pendingPromise.Reject(ex);
                    node = RemoveNode(node);
                    continue;
                }

                if (result)
                {
                    wait.pendingPromise.Resolve();
                    node = RemoveNode(node);
                } else
                {
                    node = node.Next;
                }
            }
        }

        /// <summary>
        /// Removes the provided node and returns the next node in the list.
        /// </summary>
        private LinkedListNode<PredicateWait> RemoveNode(LinkedListNode<PredicateWait> node)
        {
            var currentNode = node;
            node = node.Next;
            waiting.Remove(currentNode);
            return node;
        }
    }
}
