using Dapper;
using NUnit.Framework;
using ReliableDispatcher.DataAccess;
using ReliableDispatcher.DataAccess.Migrations;
using ReliableDispatcher.Tests.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Transactions;
using TestStack.BDDfy;

namespace ReliableDispatcher.Tests.Specs
{
    [TestFixture]
    class ReliableDispatcherSpec
    {
        private string _connectionString;
        private BlockingCollection<IOutboxMessage> _queue;
        private ReliableDispatcher _dispatcher;
        private OutboxRepository _outboxRepository;
        private NUnitHelper helper = new NUnitHelper
        {
            DefaultServerClause = @"(LocalDB)\MSSQLLocalDB"
        };

        [SetUp]
        public void SetUp()
        {
            helper.DbHelper().CreateDatabase(out _connectionString);
            DatabaseMigrator.RunMigrations(_connectionString);

            _queue = new BlockingCollection<IOutboxMessage>(1);
            _outboxRepository = new OutboxRepository(_connectionString);
            _dispatcher = new ReliableDispatcher(message => _queue.Add(message), _outboxRepository);
        }

        [Test]
        public void ReliableDispatcher_should_dispatch_enqueued_messages_after_transaction_has_committed()
        {
            TransactionScope ts = null;
            Guid messageId = default(Guid);

            this.Given(_ => AMessageHasBeenEnqueuedDuringABusinessLogicTransaction(_dispatcher, out ts, out messageId))
                    //.Then(_ => TheMessageShouldBeSavedInTheOutboxTable(messageId))
                .When(_ => TheTransactionCommits(ts))
                    .Then(_ => TheEnqueuedMessageShouldBeDispatchedOnASeparateThread(_queue, messageId))
                .BDDfy();
        }

        [Test]
        public void ReliableDispatcher_shouldnt_dispatch_messages_if_transaction_is_rolledback()
        {
            TransactionScope ts = null;
            Guid messageId = default(Guid);

            this.Given(_ => AMessageHasBeenEnqueuedDuringABusinessLogicTransaction(_dispatcher, out ts, out messageId))
                .When(_ => TheTransactionRollsBack(ts))
                    .Then(_ => TheEnqueuedMessageShouldntBeDispatched(_queue, messageId))
                .BDDfy();
        }

        [Test]
        public void ReliableDispatcher_instances_should_not_block_each_other()
        {
            throw new NotImplementedException();
        }

        private static void TheTransactionRollsBack(TransactionScope ts)
        {
            ts.Dispose();
        }

        private static void TheEnqueuedMessageShouldntBeDispatched(BlockingCollection<IOutboxMessage> queue, Guid messageId)
        {
            Assert.Throws<OperationCanceledException>(() => 
            {
                queue.Take(new CancellationTokenSource(100).Token);
            });
        }

        private void TheEnqueuedMessageShouldBeDispatchedOnASeparateThread(BlockingCollection<IOutboxMessage> queue, Guid messageId)
        {
            var message = queue.Take(new CancellationTokenSource(2000).Token);

            Assert.AreEqual(messageId, message.Id);
        }

        //private static void TheMessageShouldBeSavedInTheOutboxTable(Guid messageId)
        //{
        //    using (var connection = new SqlConnection(_connectionString))
        //    {
        //        Assert.AreEqual(1, connection.Query<int>("SELECT 1 FROM Outbox WITH(NOLOCK) WHERE Id = @messageId AND DispatchedDate IS NULL", new { messageId }).Count());
        //    }
        //}

        private static void TheTransactionCommits(TransactionScope ts)
        {
            ts.Complete();
            ts.Dispose();
        }

        private static void AMessageHasBeenEnqueuedDuringABusinessLogicTransaction(ReliableDispatcher dispatcher, out TransactionScope ts, out Guid messageId)
        {
            ts = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = IsolationLevel.ReadCommitted });

            messageId = Guid.NewGuid();

            dispatcher.EnqueueMessage(messageId, "Message Body");
        }
    }
}
