using System;

namespace ReliableDispatcher
{
    public interface IReliableMessageQueue
    {
        void EnqueueMessage(Guid messageId, string messageBody);
    }
}