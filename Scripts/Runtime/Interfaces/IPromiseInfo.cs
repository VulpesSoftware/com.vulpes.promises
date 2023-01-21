namespace Vulpes.Promises
{
    public enum PromiseState : int
    {
        Pending,
        Rejected,
        Resolved,
    }

    public interface IPromiseInfo
    {
        uint Id { get; }

        string Name { get; }

        PromiseState State { get; }

        bool IsPending { get; }

        bool IsRejected { get; }

        bool IsResolved { get; }
    }
}