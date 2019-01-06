using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Transactions;
using ReliableDispatcher.DataAccess;

namespace ReliableDispatcher
{
    public class ReliableDispatcher
    {
        internal event EventHandler<Guid> OnMarkingAsDispatched;
        internal event EventHandler<Guid> OnDispatching;

        private readonly Action<IOutboxMessage> _messageHandler;
        private readonly ConcurrentQueue<Guid> _messagesToDispatch;
        private readonly OutboxRepository _outboxRepository;
        private string _currentTransactionId = null;

        public ReliableDispatcher(Action<IOutboxMessage> messageHandler, OutboxRepository outboxRepository)
        {
            _messageHandler = messageHandler;
            _outboxRepository = outboxRepository;

            _messagesToDispatch = new ConcurrentQueue<Guid>();
        }

        public void EnqueueMessage(Guid messageId, string messageBody)
        {
            EnsureTransaction();

            _outboxRepository.CreateMessage(messageId, messageBody);

            _messagesToDispatch.Enqueue(messageId);
        }

        private void EnsureTransaction()
        {
            if (_currentTransactionId == null)
            {
                _currentTransactionId = Transaction.Current.TransactionInformation.LocalIdentifier;

                Transaction.Current.TransactionCompleted += Current_TransactionCompleted;
            }

            if (_currentTransactionId != Transaction.Current.TransactionInformation.LocalIdentifier)
            {
                throw new InvalidOperationException("This Dispatcher is attached to a different transaction");
            }
        }

        private void Current_TransactionCompleted(object sender, TransactionEventArgs e)
        {
            DispatchPendingMessages();
        }

        private void DispatchMessage(Guid messageId)
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
                        throw new InvalidOperationException($"Error while trying to dispatch message [{ messageId }]. Message not in Outbox. Call AddMessage first.");
                    }
                }

                ts.Complete();
            }
        }

        private void DispatchPendingMessages()
        {
            Guid messageId;

            while (_messagesToDispatch.TryDequeue(out messageId))
            {
                var messageIdToDispatch = messageId;

                Task.Run(() => {
                    DispatchMessage(messageIdToDispatch); });
            }
        }
    }
}
