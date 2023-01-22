using System;
using System.Collections.Generic;

namespace Vulpes.Promises
{
    public interface IPromise : IResolvable
    {
        IPromise WithName(in string name);

        #region Done

        void Done(Action onResolved);

        void Done(Action onResolved, Action<Exception> onRejected);

        void Done();

        #endregion

        IPromise Catch(Action<Exception> onRejected);

        #region Then

        IPromise<TConvertedType> Then<TConvertedType>(Func<IPromise<TConvertedType>> onResolved, Func<Exception, IPromise<TConvertedType>> onRejected);

        IPromise Then(Func<IPromise> onResolved);

        IPromise Then(Action onResolved);

        IPromise Then(Func<IPromise> onResolved, Action<Exception> onRejected);

        IPromise Then(Action onResolved, Action<Exception> onRejected);

        IPromise<TConvertedType> Then<TConvertedType>(Func<IPromise<TConvertedType>> onResolved);

        IPromise<TConvertedType> Then<TConvertedType>(Func<IPromise<TConvertedType>> onResolved, Func<Exception, IPromise<TConvertedType>> onRejected, Action<float> onProgress);

        IPromise Then(Func<IPromise> onResolved, Action<Exception> onRejected, Action<float> onProgress);

        IPromise Then(Action onResolved, Action<Exception> onRejected, Action<float> onProgress);

        #endregion

        #region ThenAll

        IPromise ThenAll(Func<IEnumerable<IPromise>> chain);

        IPromise<IEnumerable<TConvertedType>> ThenAll<TConvertedType>(Func<IEnumerable<IPromise<TConvertedType>>> chain);

        #endregion

        #region ThenSequence

        IPromise ThenSequence(Func<IEnumerable<Func<IPromise>>> chain);

        IPromise ThenSequence(params Func<IPromise>[] chain);

        #endregion

        #region ThenRace

        IPromise ThenRace(Func<IEnumerable<IPromise>> chain);

        IPromise<TConvertedType> ThenRace<TConvertedType>(Func<IEnumerable<IPromise<TConvertedType>>> chain);

        #endregion

        IPromise Finally(Action onComplete);

        #region ContinueWith

        IPromise ContinueWith(Func<IPromise> onResolved);

        IPromise<TConvertedType> ContinueWith<TConvertedType>(Func<IPromise<TConvertedType>> onComplete);

        #endregion

        IPromise Progress(Action<float> onProgress);
    }

    public interface IPromise<TPromisedType> : IResolvable<TPromisedType>
    {
        IPromise<TPromisedType> WithName(in string name);

        #region Done

        void Done(Action<TPromisedType> onResolved, Action<Exception> onRejected);

        void Done(Action<TPromisedType> onResolved);

        void Done();

        #endregion

        #region Catch

        IPromise Catch(Action<Exception> onRejected);

        IPromise<TPromisedType> Catch(Func<Exception, TPromisedType> onRejected);

        #endregion

        #region Then

        IPromise<TConvertedType> Then<TConvertedType>(Func<TPromisedType, IPromise<TConvertedType>> onResolved);

        IPromise Then(Func<TPromisedType, IPromise> onResolved);

        IPromise Then(Action<TPromisedType> onResolved);

        IPromise<TConvertedType> Then<TConvertedType>(Func<TPromisedType, IPromise<TConvertedType>> onResolved, Func<Exception, IPromise<TConvertedType>> onRejected);

        IPromise Then(Func<TPromisedType, IPromise> onResolved, Action<Exception> onRejected);

        IPromise Then(Action<TPromisedType> onResolved, Action<Exception> onRejected);

        IPromise<TConvertedType> Then<TConvertedType>(Func<TPromisedType, IPromise<TConvertedType>> onResolved, Func<Exception, IPromise<TConvertedType>> onRejected, Action<float> onProgress);

        IPromise Then(Func<TPromisedType, IPromise> onResolved, Action<Exception> onRejected, Action<float> onProgress);

        IPromise Then(Action<TPromisedType> onResolved, Action<Exception> onRejected, Action<float> onProgress);

        IPromise<TConvertedType> Then<TConvertedType>(Func<TPromisedType, TConvertedType> transform);

        #endregion

        #region ThenAll

        IPromise<IEnumerable<TConvertedType>> ThenAll<TConvertedType>(Func<TPromisedType, IEnumerable<IPromise<TConvertedType>>> chain);

        IPromise ThenAll(Func<TPromisedType, IEnumerable<IPromise>> chain);

        #endregion

        #region ThenRace

        IPromise<TConvertedType> ThenRace<TConvertedType>(Func<TPromisedType, IEnumerable<IPromise<TConvertedType>>> chain);

        IPromise ThenRace(Func<TPromisedType, IEnumerable<IPromise>> chain);

        #endregion

        IPromise<TPromisedType> Finally(Action onComplete);

        #region ContinueWith

        IPromise ContinueWith(Func<IPromise> onResolved);

        IPromise<TConvertedType> ContinueWith<TConvertedType>(Func<IPromise<TConvertedType>> onComplete);

        #endregion

        IPromise<TPromisedType> Progress(Action<float> onProgress);
    }
}