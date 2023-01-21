using System;

namespace Vulpes.Promises
{
    public struct TimeData
    {
        public float elapsedTime;
        public float deltaTime;
        public uint elapsedUpdates;
    }

    public interface IPromiseTimer
    {
        IPromise WaitFor(float seconds);

        IPromise WaitUntil(Func<TimeData, bool> predicate);

        IPromise WaitWhile(Func<TimeData, bool> predicate);

        void Update(in float deltaTime);

        bool Cancel(IPromise promise);
    }
}