using System;

namespace ReliableDispatcher.DataAccess
{
    internal struct OutboxMessage : IOutboxMessage
    {
        public Guid Id { get; set; }
        public string Body { get; set; }
        public DateTime? DispatchedDate { get; set; }
        public int DispatchAttempts { get; set; }
    }
}
