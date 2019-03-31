using System;
using System.Collections.Concurrent;
using System.Transactions;

namespace ReliableDispatcher
{
    public class ReliableMessageQueue : IReliableMessageQueue
    {
        private readonly ConcurrentQueue<Guid> _messagesToDispatch;
        private string _currentTransactionId = null;

        private readonly IReliableDispatcher _reliableDispatcher;

        public ReliableMessageQueue(IReliableDispatcher reliableDispatcher)
        {
            _reliableDispatcher = reliableDispatcher ?? throw new ArgumentNullException(nameof(reliableDispatcher));

            _messagesToDispatch = new ConcurrentQueue<Guid>();
        }

        public void EnqueueMessage(Guid messageId, string messageBody)
        {
            EnsureTransaction();

            _reliableDispatcher.EnqueueMessage(messageId, messageBody);

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
            if (e.Transaction.TransactionInformation.Status == TransactionStatus.Committed)
            {
                DispatchPendingMessages();
            }
        }

        private void DispatchPendingMessages()
        {
            Guid messageId;

            while (_messagesToDispatch.TryDequeue(out messageId))
            {
                var messageIdToDispatch = messageId;

                try
                {
                    _reliableDispatcher.DispatchMessage(messageIdToDispatch);
                }
                catch (Exception ex)
                {
                    //TODO: Log message
                }
            }
        }
    }
}
