using System;

namespace ReliableDispatcher
{
    public interface IOutboxMessage
    {
        Guid Id { get; }
        string Body { get; }
        DateTime CreatedDate { get; }
        DateTime? DispatchedDate { get; }
        int DispatchAttempts { get; }
    }
}
