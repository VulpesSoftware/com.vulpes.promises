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

        private LinkedListNode<PromiseTimerWaitPredicate> FindInWaiting(IPromise akPromise)
        {
            for (var node = waitingPromises.First; node != null; node = node.Next)
            {
                if (node.Value.pendingPromise.Id.Equals(akPromise.Id))
                {
                    return node;
                }
            }
            return null;
        }

        private LinkedListNode<PromiseTimerWaitPredicate> RemoveNode(LinkedListNode<PromiseTimerWaitPredicate> akNode)
        {
            var current = akNode;
            akNode = current.Next;
            waitingPromises.Remove(current);
            return akNode;
        }

        private void RepeatPromise(Func<IPromise> akPromise, Promise akPendingPromise, Func<bool> akPredicate)
        {
            akPromise().Then(() =>
            {
                if (akPredicate())
                {
                    akPendingPromise.Resolve();
                } else
                {
                    RepeatPromise(akPromise, akPendingPromise, akPredicate);
                }
            }).Catch(e =>
            {
                akPendingPromise.Reject(e);
            }).Done();
        }

        public void Update(float afDeltaTime)
        {
            currentTime += afDeltaTime;
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

        public IPromise WaitFor(float afSeconds)
        {
            return WaitUntil(t => t.elapsedTime >= afSeconds);
        }

        public IPromise WaitWhile(Func<PromiseTimeData, bool> akPredicate)
        {
            return WaitUntil(t => !akPredicate(t));
        }

        public IPromise WaitUntil(Func<PromiseTimeData, bool> akPredicate)
        {
            var promise = Promise.Create();
            var waitPredicate = new PromiseTimerWaitPredicate()
            {
                startingTime = currentTime,
                startingFrame = currentFrame,
                timeData = new PromiseTimeData(),
                predicate = akPredicate,
                pendingPromise = promise
            };
            waitingPromises.AddLast(waitPredicate);
            return promise;
        }

        public IPromise RepeatUntil(Func<IPromise> akPromise, Func<bool> akPredicate)
        {
            var promiseToResolve = Promise.Create();
            RepeatPromise(akPromise, promiseToResolve, akPredicate);
            return promiseToResolve;
        }

        public bool Cancel(IPromise akPromise)
        {
            var node = FindInWaiting(akPromise);
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
