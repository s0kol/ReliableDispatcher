using System;
using System.Collections.Generic;

namespace ReliableDispatcher
{
    public interface IReliableDispatcher
    {
        void EnqueueMessage(Guid messageId, string messageBody);

        void DispatchMessage(Guid messageId);

        IEnumerable<Guid> FindMessagesToDispatch(int attemptThreshold = 10);
    }
}