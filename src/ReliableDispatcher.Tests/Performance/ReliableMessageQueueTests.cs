using NUnit.Framework;
using ReliableDispatcher.DataAccess;
using ReliableDispatcher.DataAccess.Migrations;
using ReliableDispatcher.Tests.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace ReliableDispatcher.Tests.Performance
{
    [TestFixture]
    public class ReliableMessageQueueTests
    {
        private string _connectionString;
        private NUnitHelper helper = new NUnitHelper
        {
            DefaultServerClause = @"(LocalDB)\MSSQLLocalDB"
        };
        private ConcurrentQueue<string> _log;
        private Action<string> _logger;

        [SetUp]
        public void SetUp()
        {
            helper.DbHelper().CreateDatabase(out _connectionString);
            DatabaseMigrator.RunMigrations(_connectionString);

            _log = new ConcurrentQueue<string>();
            _logger = x =>
            {
                _log.Enqueue(x);
            };

            WarmUpDatabase();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var entry in _log)
            {
                Console.WriteLine(entry);
            }
        }

        [Test]
        [Timeout(2000)]
        public void DispatchingMessages_performs_according_to_baseline()
        {
            // Arrange
            var dispatchersCount = 100;
            var handlerDelay = 1;
            var expectedDuration = 25 + (int)(Math.Ceiling((double)dispatchersCount / Environment.ProcessorCount) * (handlerDelay + 4));
            
            var dispatchedMessages = new ConcurrentQueue<IOutboxMessage>();

            Action<IOutboxMessage> messageHandler = m =>
            {
                dispatchedMessages.Enqueue(m);

                Thread.Sleep(handlerDelay);
            };

            var stopwatch = Stopwatch.StartNew();

            // Act //Assert
            Assert.DoesNotThrow(() =>
            {
                Parallel.For(0, dispatchersCount, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, i =>
                {
                    var queue = new ReliableMessageQueue(
                        new ReliableDispatcher(
                            messageHandler,
                            new PerformanceLoggindOutboxRepository(new OutboxRepository(_connectionString), _logger)));

                    using (var ts = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
                    {
                        queue.EnqueueMessage(Guid.NewGuid(), Guid.NewGuid().ToString());

                        ts.Complete();
                    }
                });
            });

            Assert.True(new OutboxRepository(_connectionString).GetMessagesToDispatch().Count() == 0);

            var totalMilliseconds = stopwatch.ElapsedMilliseconds;
            var summary = $"Expected to be done in [{expectedDuration}] ms, [{totalMilliseconds}] ms elapsed";

            Console.WriteLine(summary);

            Assert.True(expectedDuration >= totalMilliseconds, summary);

            var maxMessageDelay = dispatchedMessages.Max(x => (x.DispatchedDate.Value - x.CreatedDate).TotalMilliseconds);
            var avgMessageDelay = dispatchedMessages.Average(x => (x.DispatchedDate.Value - x.CreatedDate).TotalMilliseconds);

            var maxExpectationMessage = $"Max dispatch delay was [{maxMessageDelay}] ms";
            var avgExpectationMessage = $"Average dispatch delay was [{avgMessageDelay}] ms";

            Console.WriteLine(maxExpectationMessage);
            Console.WriteLine(avgExpectationMessage);

            Assert.True(maxMessageDelay < 15, maxExpectationMessage);
            Assert.True(avgMessageDelay < 3, avgExpectationMessage);
        }

        private void WarmUpDatabase()
        {
            _logger("Warming up database...");

            var repo = new PerformanceLoggindOutboxRepository(new OutboxRepository(_connectionString), _logger);

            using (var ts = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                var msgId = Guid.NewGuid();

                repo.CreateMessage(msgId, "Warmup");
                repo.IsMessageReadyToBeDispatchedNonBlocking(msgId);
                repo.GetAndMarkAsDispatchedIfAvailableForDispatching(msgId);

                ts.Complete();
            }

            _logger("done");
        }
    }
}
