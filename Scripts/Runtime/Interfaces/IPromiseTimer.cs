using System;

namespace Vulpes.Promises
{
    public struct PromiseTimeData
    {
        public float elapsedTime;
        public float deltaTime;
        public int elapsedUpdates;
    }

    public interface IPromiseTimer 
    {
        IPromise WaitFor(float afSeconds);
        
        IPromise WaitUntil(Func<PromiseTimeData, bool> akPredicate);
        
        IPromise WaitWhile(Func<PromiseTimeData, bool> akPredicate);

        IPromise RepeatUntil(Func<IPromise> akPromise, Func<bool> akPredicate);

        bool Cancel(IPromise akPromise);

        void Update(float afDeltaTime);
    }
}
