using System;

namespace Vulpes.Promises
{
    /// <summary>
    /// Specifies the state of a <see cref="IPromise"/>.
    /// </summary>
    public enum PromiseState : int
    {
        /// <summary>
        /// The <see cref="IPromise"/> is currently pending.
        /// </summary>
        Pending,
        /// <summary>
        /// The <see cref="IPromise"/> has been rejected.
        /// </summary>
        Rejected,
        /// <summary>
        /// The <see cref="IPromise"/> has been resolved.
        /// </summary>
        Resolved,
    }

    /// <summary>
    /// Implements a C# Promise.
    /// https://developer.mozilla.org/en/docs/Web/JavaScript/Reference/Global_Objects/Promise
    /// </summary>
    public interface IPromise : IPendingPromise
    {
        [Obsolete("This property is obsolete. Use 'State' instead.", false)]
        PromiseState PromiseState { get; }

        /// <summary>
        /// Tracks the current state of the <see cref="IPromise"/>.
        /// </summary>
        PromiseState State { get; }

        /// <summary>
        /// Name of the <see cref="IPromise"/>, when set, useful for debugging.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Handles errors for the <see cref="IPromise"/>.
        /// </summary>
        IPromise Catch(Action<Exception> onRejected);

        /// <summary>
        /// Complete the <see cref="IPromise"/>.
        /// </summary>
        void Done();

        /// <summary>
        /// Complete the <see cref="IPromise"/>, onResolved is called on successful completion.
        /// </summary>
        void Done(Action onResolved);

        /// <summary>
        /// Set the name of the <see cref="IPromise"/>, useful for debugging.
        /// </summary>
        IPromise WithName(string name);

        /// <summary>
        /// Add a resolved callback.
        /// </summary>
        IPromise Then(Action onResolved);

        /// <summary>
        /// Add a resolved callback.
        /// </summary>
        IPromise Then(Func<IPromise> onResolved);

        /// <summary>
        /// Add a resolved callback.
        /// </summary>
        IPromise<ConvertedT> Then<ConvertedT>(Func<IPromise<ConvertedT>> onResolved);
    }

    /// <summary>
    /// Implements a C# Promise.
    /// https://developer.mozilla.org/en/docs/Web/JavaScript/Reference/Global_Objects/Promise
    /// </summary>
    public interface IPromise<PromisedT> : IPendingPromise<PromisedT>
    {
        /// <summary>
        /// Tracks the current state of the <see cref="IPromise{PromisedT}"/>.
        /// </summary>
        PromiseState State { get; }

        /// <summary>
        /// Name of the <see cref="IPromise{PromisedT}"/>, when set, useful for debugging.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Handles errors for the <see cref="IPromise{PromisedT}"/>.
        /// </summary>
        IPromise<PromisedT> Catch(Action<Exception> onRejected);

        /// <summary>
        /// Complete the <see cref="IPromise{PromisedT}"/>.
        /// </summary>
        void Done();

        /// <summary>
        /// Complete the <see cref="IPromise{PromisedT}"/>, onResolved is called on successful completion.
        /// </summary>
        void Done(Action onResolved);

        /// <summary>
        /// Complete the <see cref="IPromise{PromisedT}"/>, onResolved is called on successful completion.
        /// </summary>
        void Done(Action<PromisedT> onResolved);

        /// <summary>
        /// Set the name of the <see cref="IPromise{PromisedT}"/>, useful for debugging.
        /// </summary>
        IPromise<PromisedT> WithName(string name);

        /// <summary>
        /// Add a resolved callback.
        /// </summary>
        IPromise Then(Action onResolved);

        /// <summary>
        /// Add a resolved callback.
        /// </summary>
        IPromise<PromisedT> Then(Action<PromisedT> onResolved);

        /// <summary>
        /// Add a resolved callback.
        /// </summary>
        IPromise<PromisedT> Then(Func<IPromise> onResolved);

        /// <summary>
        /// Add a resolved callback.
        /// </summary>
        IPromise<PromisedT> Then(Func<IPromise<PromisedT>> onResolved);

        /// <summary>
        /// Add a resolved callback.
        /// </summary>
        IPromise<PromisedT> Then(Func<PromisedT, IPromise<PromisedT>> onResolved);

        /// <summary>
        /// Add a resolved callback.
        /// </summary>
        IPromise Then(Func<PromisedT, IPromise> onResolved);

        /// <summary>
        /// Add a resolved callback.
        /// </summary>
        IPromise<ConvertedT> Then<ConvertedT>(Func<PromisedT, ConvertedT> onResolved);

        /// <summary>
        /// Add a resolved callback.
        /// </summary>
        IPromise<ConvertedT> Then<ConvertedT>(Func<PromisedT, IPromise<ConvertedT>> onResolved);
    }
}
