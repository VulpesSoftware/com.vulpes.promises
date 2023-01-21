using System;
using System.Collections.Generic;

namespace Vulpes.Promises
{
    internal sealed class PredicateWait
    {
        public Func<TimeData, bool> predicate;
        public float timeStarted;
        public IResolvable resolvable;
        public TimeData timeData;
        public uint frameStarted;
    }

    public class PromiseTimer : IPromiseTimer
    {
        private float time;
        private uint frame;
        private readonly LinkedList<PredicateWait> waiting = new();

        public IPromise WaitUntil(Func<TimeData, bool> predicate)
        {
            IPromise promise = Promise.Create();
            PredicateWait wait = new()
            {
                timeStarted = time,
                resolvable = promise,
                timeData = new(),
                predicate = predicate,
                frameStarted = frame
            };
            waiting.AddLast(wait);
            return promise;
        }

        private LinkedListNode<PredicateWait> FindInWaiting(IPromise promise)
        {
            for (LinkedListNode<PredicateWait> node = waiting.First; node != null; node = node.Next)
            {
                if (node.Value.resolvable.Id.Equals(promise.Id))
                {
                    return node;
                }
            }
            return null;
        }

        public IPromise WaitFor(float seconds)
            => WaitUntil(t => t.elapsedTime >= seconds);

        public IPromise WaitWhile(Func<TimeData, bool> predicate)
            => WaitUntil(t => !predicate(t));

        public bool Cancel(IPromise promise)
        {
            LinkedListNode<PredicateWait> node = FindInWaiting(promise);
            if (node == null)
            {
                return false;
            }
            node.Value.resolvable.Reject(new PromiseCancelledException("Promise was cancelled by user."));
            waiting.Remove(node);
            return true;
        }

        private LinkedListNode<PredicateWait> RemoveNode(LinkedListNode<PredicateWait> node)
        {
            LinkedListNode<PredicateWait> currentNode = node;
            node = node.Next;
            waiting.Remove(currentNode);
            return node;
        }

        public void Update(in float deltaTime)
        {
            time += deltaTime;
            frame++;
            LinkedListNode<PredicateWait> node = waiting.First;
            while (node != null)
            {
                PredicateWait wait = node.Value;
                float newElapsedTime = time - wait.timeStarted;
                wait.timeData.deltaTime = newElapsedTime - wait.timeData.elapsedTime;
                wait.timeData.elapsedTime = newElapsedTime;
                uint newElapsedUpdates = frame - wait.frameStarted;
                wait.timeData.elapsedUpdates = newElapsedUpdates;
                bool result;
                try
                {
                    result = wait.predicate(wait.timeData);
                }
                catch (Exception exception)
                {
                    wait.resolvable.Reject(exception);
                    node = RemoveNode(node);
                    continue;
                }
                if (result)
                {
                    wait.resolvable.Resolve();
                    node = RemoveNode(node);
                } else
                {
                    node = node.Next;
                }
            }
        }
    }
}