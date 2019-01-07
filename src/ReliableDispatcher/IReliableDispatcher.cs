using System;

namespace ReliableDispatcher
{
    public interface IReliableDispatcher
    {
        void EnqueueMessage(Guid messageId, string messageBody);
    }
}