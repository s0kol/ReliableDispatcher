using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;
using ReliableDispatcher.DataAccess;

namespace ReliableDispatcher
{
    public class ReliableDispatcher : IReliableDispatcher
    {
        internal event EventHandler<Guid> OnMarkingAsDispatched;
        internal event EventHandler<Guid> OnDispatching;

        private readonly Action<IOutboxMessage> _messageHandler;
        
        private readonly IOutboxRepository _outboxRepository;

        public ReliableDispatcher(Action<IOutboxMessage> messageHandler, IOutboxRepository outboxRepository)
        {
            _messageHandler = messageHandler;
            _outboxRepository = outboxRepository;
        }

        public void EnqueueMessage(Guid messageId, string messageBody)
        {
            _outboxRepository.CreateMessage(messageId, messageBody);
        }

        public IEnumerable<Guid> FindMessagesToDispatch(int attemptThreshold = 10)
        {
            return _outboxRepository.GetMessagesToDispatch();
        }

        public void DispatchMessage(Guid messageId)
        {
            using (var ts = new TransactionScope(
                TransactionScopeOption.RequiresNew,
                new TransactionOptions() { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                if (_outboxRepository.IsMessageReadyToBeDispatchedNonBlocking(messageId))
                {
                    OnMarkingAsDispatched?.Invoke(this, messageId);

                    var message = _outboxRepository.GetAndMarkAsDispatchedIfAvailableForDispatching(messageId);

                    try
                    {
                        if (message != null)
                        {
                            OnDispatching?.Invoke(this, messageId);

                            _messageHandler.Invoke(message);
                        }
                    }
                    catch (Exception)
                    {
                        _outboxRepository.RevertMarkingAsDispatchedNonBlocking(messageId);

                        ts.Complete();

                        throw;
                    }
                }
                else
                {
                    if (!_outboxRepository.IsMessageInOutboxNonBlocking(messageId))
                    {
                        throw new InvalidOperationException($"Error while trying to dispatch message [{ messageId }]. Message not in Outbox. Call CreateMessage first.");
                    }
                }

                ts.Complete();
            }
        }
    }
}
