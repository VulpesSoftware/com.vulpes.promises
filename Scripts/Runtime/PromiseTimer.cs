using System;
using System.Collections.Generic;

namespace Vulpes.Promises
{
    public sealed class PromiseTimer : IPromiseTimer
    {
        internal class PromiseTimerWaitPredicate
        {
            public Func<PromiseTimeData, bool> predicate;
            public float startingTime;
            public IPendingPromise pendingPromise;
            public PromiseTimeData timeData;
            public int startingFrame;
        }

        private float currentTime;
        private int currentFrame;
        private LinkedList<PromiseTimerWaitPredicate> waitingPromises = new LinkedList<PromiseTimerWaitPredicate>();

        private LinkedListNode<PromiseTimerWaitPredicate> FindInWaiting(IPromise promise)
        {
            for (var node = waitingPromises.First; node != null; node = node.Next)
            {
                if (node.Value.pendingPromise.Id.Equals(promise.Id))
                {
                    return node;
                }
            }
            return null;
        }

        private LinkedListNode<PromiseTimerWaitPredicate> RemoveNode(LinkedListNode<PromiseTimerWaitPredicate> node)
        {
            var current = node;
            node = current.Next;
            waitingPromises.Remove(current);
            return node;
        }

        private void RepeatPromise(Func<IPromise> promise, Promise pendingPromise, Func<bool> predicate)
        {
            promise().Then(() =>
            {
                if (predicate())
                {
                    pendingPromise.Resolve();
                } else
                {
                    RepeatPromise(promise, pendingPromise, predicate);
                }
            }).Catch(e =>
            {
                pendingPromise.Reject(e);
            }).Done();
        }

        public void Update(float deltaTime)
        {
            currentTime += deltaTime;
            currentFrame++;

            var nextPromiseNode = waitingPromises.First;
            while (nextPromiseNode != null)
            {
                var nextWaitingPromise = nextPromiseNode.Value;
                var elapsedTime = currentTime - nextWaitingPromise.startingTime;
                nextWaitingPromise.timeData.deltaTime = elapsedTime - nextWaitingPromise.timeData.elapsedTime;
                nextWaitingPromise.timeData.elapsedTime = elapsedTime;
                var elapsedFrames = currentFrame - nextWaitingPromise.startingFrame;
                nextWaitingPromise.timeData.elapsedUpdates = elapsedFrames;

                bool result;
                try
                {
                    result = nextWaitingPromise.predicate(nextWaitingPromise.timeData);
                }
                catch (Exception e)
                {
                    nextWaitingPromise.pendingPromise.Reject(e);
                    nextPromiseNode = RemoveNode(nextPromiseNode);
                    continue;
                }

                if (result)
                {
                    nextWaitingPromise.pendingPromise.Resolve();
                    nextPromiseNode = RemoveNode(nextPromiseNode);
                } else
                {
                    nextPromiseNode = nextPromiseNode.Next;
                }
            }
        }

        public IPromise WaitFor(float seconds)
        {
            return WaitUntil(t => t.elapsedTime >= seconds);
        }

        public IPromise WaitWhile(Func<PromiseTimeData, bool> predicate)
        {
            return WaitUntil(t => !predicate(t));
        }

        public IPromise WaitUntil(Func<PromiseTimeData, bool> predicate)
        {
            var promise = Promise.Create();
            var waitPredicate = new PromiseTimerWaitPredicate()
            {
                startingTime = currentTime,
                startingFrame = currentFrame,
                timeData = new PromiseTimeData(),
                predicate = predicate,
                pendingPromise = promise
            };
            waitingPromises.AddLast(waitPredicate);
            return promise;
        }

        public IPromise RepeatUntil(Func<IPromise> promise, Func<bool> predicate)
        {
            var promiseToResolve = Promise.Create();
            RepeatPromise(promise, promiseToResolve, predicate);
            return promiseToResolve;
        }

        public bool Cancel(IPromise promise)
        {
            var node = FindInWaiting(promise);
            if (node == null)
            {
                return false;
            }
            node.Value.pendingPromise.Reject(new PromiseCancelledException("Vulpes.Promises.PromiseTimer.Cancel: Promise was cancelled by user."));
            waitingPromises.Remove(node);
            return true;
        }
    }
}
