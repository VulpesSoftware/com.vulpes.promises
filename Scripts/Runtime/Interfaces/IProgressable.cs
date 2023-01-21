namespace Vulpes.Promises
{
    public interface IProgressable
    {
        void ReportProgress(in float progress);
    }
}