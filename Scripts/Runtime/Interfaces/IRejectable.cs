using System;

namespace Vulpes.Promises
{
    public interface IRejectable
    {
        uint Id { get; }

        void Reject(Exception akException);
    }
}
