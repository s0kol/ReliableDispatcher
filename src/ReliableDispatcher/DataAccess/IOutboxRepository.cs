using System;

namespace ReliableDispatcher.DataAccess
{
    public interface IOutboxRepository
    {
        void CreateMessage(Guid messageId, string messageBody);
        IOutboxMessage GetAndMarkAsDispatchedIfAvailableForDispatching(Guid messageId);
        bool IsMessageInOutboxNonBlocking(Guid messageId);
        bool IsMessageReadyToBeDispatchedNonBlocking(Guid messageId);
        void RevertMarkingAsDispatchedNonBlocking(Guid messageId);
    }
}