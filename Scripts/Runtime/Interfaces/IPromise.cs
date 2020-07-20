using System;

namespace Vulpes.Promises
{
    public enum PromiseState
    {
        Pending = 0,
        Rejected = 1,
        Resolved = 2,
    }

    public interface IPromise : IPendingPromise
    {
        PromiseState PromiseState { get; }

        IPromise Catch(Action<Exception> akOnRejected);

        void Done();

        void Done(Action akAction);

        IPromise WithName(string asName);

        IPromise Then(Action akAction);

        IPromise Then(Func<IPromise> akPromise);

        IPromise<ConvertedT> Then<ConvertedT>(Func<IPromise<ConvertedT>> akPromise);
    }

    public interface IPromise<PromisedT> : IPendingPromise<PromisedT>
    {
        PromiseState PromiseState { get; }

        IPromise<PromisedT> Catch(Action<Exception> akOnRejected);

        void Done();

        void Done(Action akAction);

        void Done(Action<PromisedT> akAction);

        IPromise<PromisedT> WithName(string asName);

        IPromise Then(Action akAction);

        IPromise<PromisedT> Then(Action<PromisedT> akAction);

        IPromise<PromisedT> Then(Func<IPromise> akPromise);

        IPromise<PromisedT> Then(Func<IPromise<PromisedT>> akPromise);

        IPromise<PromisedT> Then(Func<PromisedT, IPromise<PromisedT>> akPromise);

        IPromise Then(Func<PromisedT, IPromise> akPromise);

        IPromise<ConvertedT> Then<ConvertedT>(Func<PromisedT, ConvertedT> akAction);

        IPromise<ConvertedT> Then<ConvertedT>(Func<PromisedT, IPromise<ConvertedT>> akPromise);
    }
}
