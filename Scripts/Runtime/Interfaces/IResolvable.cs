namespace Vulpes.Promises
{
    public interface IResolvable : IPromiseInfo, IRejectable, IProgressable
    {
        void Resolve();
    }

    public interface IResolvable<TPromisedType> : IPromiseInfo, IRejectable, IProgressable
    {
        void Resolve(TPromisedType value);
    }
}