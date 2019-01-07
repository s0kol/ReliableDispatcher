using Castle.Windsor;
using NUnit.Framework;
using ReliableDispatcher.DataAccess.Migrations;
using ReliableDispatcher.Tests.Helpers;
using ReliableDispatcher.Tests.Samples;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using TestStack.BDDfy;

namespace ReliableDispatcher.Tests.Specs
{
    [TestFixture]
    public class ReliableDispatcherModuleSpec
    {
        private BlockingCollection<IOutboxMessage> _queue;
        private NUnitHelper helper = new NUnitHelper
        {
            DefaultServerClause = @"(LocalDB)\MSSQLLocalDB"
        };
        private string _connectionString;

        [SetUp]
        public void SetUp()
        {
            _queue = new BlockingCollection<IOutboxMessage>(1);

            helper.DbHelper().CreateDatabase(out _connectionString);
            DatabaseMigrator.RunMigrations(_connectionString);
        }

        [Test]
        public void ReliableDispatcherModule_should_dispatch_messages_remained_in_outbox_due_to_a_temporary_failure()
        {
            var container = new WindsorContainer();
            var messageId = Guid.NewGuid();
            Func<IOutboxMessage, bool> handlingStep = message => throw new InvalidOperationException();

            this.Given(_ => AnApplicationWithTheReliableModuleWasStarted(handlingStep, container))
                .When(_ => AMessageFailedToBeDispatched(container, messageId))
                .Then(_ => ItShouldBePickedUpByAnOutboxMonitorAndDispatched(messageId))
                .BDDfy();
        }

        private void ItShouldBePickedUpByAnOutboxMonitorAndDispatched(Guid messageId)
        {
            var message = _queue.Take(new CancellationTokenSource(1000).Token);

            Assert.AreEqual(messageId, message.Id);
        }

        private void AMessageFailedToBeDispatched(IWindsorContainer container, Guid messageId)
        {
            using (var ts = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                var dispatcher = container.Resolve<IReliableDispatcher>();

                dispatcher.EnqueueMessage(messageId, Guid.NewGuid().ToString());

                ts.Complete();
            }
        }

        private void AnApplicationWithTheReliableModuleWasStarted(Func<IOutboxMessage, bool> handlingStep, IWindsorContainer container)
        {
            var application = new SampleApplication();

            application.Configure(config => 
            {
                config.Container = container;
                config.OutboxDatabaseConnectionString = _connectionString;
                config.DispatchPipeline = (message) =>
                {
                    var result = handlingStep(message);

                    if (result)
                    {
                        return config.DispatchPipeline(message);
                    }
                    else
                    {
                        return result;
                    }
                };
            });

            application.Start();
        }

        [Test]
        public void ReliableDispatcherModule_should_give_up_on_failed_messages_after_multiple_retries()
        {
            throw new NotImplementedException();
        }
    }
}
