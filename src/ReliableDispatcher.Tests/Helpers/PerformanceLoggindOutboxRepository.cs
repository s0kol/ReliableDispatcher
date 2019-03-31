using ReliableDispatcher.DataAccess;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReliableDispatcher.Tests.Helpers
{
    public class PerformanceLoggindOutboxRepository : IOutboxRepository
    {
        private readonly IOutboxRepository _adaptee;
        private readonly Action<string> _logger;

        public PerformanceLoggindOutboxRepository(IOutboxRepository adaptee, Action<string> logger)
        {
            _adaptee = adaptee;
            _logger = logger;
        }

        private void ExecuteAndLogTime(Action action, string logMessage)
        {
            var stopwatch = Stopwatch.StartNew();

            action();

            _logger($"Executing [{logMessage}] \ton thread [{Thread.CurrentThread.ManagedThreadId}] \ttook [{stopwatch.ElapsedMilliseconds}] ms");

        }

        public void CreateMessage(Guid messageId, string messageBody)
        {
            ExecuteAndLogTime(
                () => _adaptee.CreateMessage(messageId, messageBody),
                $"{nameof(CreateMessage)} {nameof(messageId)}:[{messageId}]");
        }

        public IOutboxMessage GetAndMarkAsDispatchedIfAvailableForDispatching(Guid messageId)
        {
            IOutboxMessage result = null;

            ExecuteAndLogTime(
                () => result = _adaptee.GetAndMarkAsDispatchedIfAvailableForDispatching(messageId),
                $"{nameof(GetAndMarkAsDispatchedIfAvailableForDispatching)} {nameof(messageId)}:[{messageId}]");

            return result;
        }

        public IEnumerable<Guid> GetMessagesToDispatch(int attemptThreshold = 10)
        {
            IEnumerable<Guid> result = null;

            ExecuteAndLogTime(
                () => result = _adaptee.GetMessagesToDispatch(attemptThreshold),
                $"{nameof(GetMessagesToDispatch)} {nameof(attemptThreshold)}:[{attemptThreshold}]");

            return result;
        }

        public bool IsMessageInOutboxNonBlocking(Guid messageId)
        {
            bool result = false;

            ExecuteAndLogTime(
                () => result = _adaptee.IsMessageInOutboxNonBlocking(messageId),
                $"{nameof(IsMessageInOutboxNonBlocking)} {nameof(messageId)}:[{messageId}]");

            return result;
        }

        public bool IsMessageReadyToBeDispatchedNonBlocking(Guid messageId)
        {
            bool result = false;

            ExecuteAndLogTime(
                () => result = _adaptee.IsMessageReadyToBeDispatchedNonBlocking(messageId),
                $"{nameof(IsMessageReadyToBeDispatchedNonBlocking)} {nameof(messageId)}:[{messageId}]");

            return result;
        }

        public void RevertMarkingAsDispatchedNonBlocking(Guid messageId)
        {
            ExecuteAndLogTime(
                () => _adaptee.RevertMarkingAsDispatchedNonBlocking(messageId),
                $"{nameof(RevertMarkingAsDispatchedNonBlocking)} {nameof(messageId)}:[{messageId}]");
        }
    }
}
