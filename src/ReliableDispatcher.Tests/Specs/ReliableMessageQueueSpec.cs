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
    class ReliableMessageQueueSpec
    {
        private string _connectionString;
        private BlockingCollection<IOutboxMessage> _destinationQueue;
        private ReliableMessageQueue _dispatchQueue;
        
        private NUnitHelper helper = new NUnitHelper
        {
            DefaultServerClause = @"(LocalDB)\MSSQLLocalDB"
        };

        [SetUp]
        public void SetUp()
        {
            helper.DbHelper().CreateDatabase(out _connectionString);
            DatabaseMigrator.RunMigrations(_connectionString);

            _destinationQueue = new BlockingCollection<IOutboxMessage>(1);
            _dispatchQueue = new ReliableMessageQueue(
                new ReliableDispatcher(message => _destinationQueue.Add(message),
                new OutboxRepository(_connectionString)));
        }

        [Test]
        public void ReliableMessageQueue_should_dispatch_enqueued_messages_after_transaction_has_committed()
        {
            TransactionScope ts = null;
            Guid messageId = default(Guid);

            this.Given(_ => AMessageHasBeenEnqueuedDuringABusinessLogicTransaction(_dispatchQueue, out ts, out messageId))
                    //.Then(_ => TheMessageShouldBeSavedInTheOutboxTable(messageId))
                .When(_ => TheTransactionCommits(ts))
                    .Then(_ => TheEnqueuedMessageShouldBeDispatchedOnASeparateThread(_destinationQueue, messageId))
                .BDDfy();
        }

        [Test]
        public void ReliableMessageQueue_shouldnt_dispatch_messages_if_transaction_is_rolledback()
        {
            TransactionScope ts = null;
            Guid messageId = default(Guid);

            this.Given(_ => AMessageHasBeenEnqueuedDuringABusinessLogicTransaction(_dispatchQueue, out ts, out messageId))
                .When(_ => TheTransactionRollsBack(ts))
                    .Then(_ => TheEnqueuedMessageShouldntBeDispatched(_destinationQueue, messageId))
                .BDDfy();
        }

        [Test]
        public void ReliableMessageQueue_instances_should_not_block_each_other()
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

        private static void AMessageHasBeenEnqueuedDuringABusinessLogicTransaction(ReliableMessageQueue queue, out TransactionScope ts, out Guid messageId)
        {
            ts = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions() { IsolationLevel = IsolationLevel.ReadCommitted });

            messageId = Guid.NewGuid();

            queue.EnqueueMessage(messageId, "Message Body");
        }
    }
}
