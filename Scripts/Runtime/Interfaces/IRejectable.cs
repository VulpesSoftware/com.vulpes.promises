using System;

namespace Vulpes.Promises
{
    public interface IRejectable
    {
        void Reject(Exception exception);
    }
}