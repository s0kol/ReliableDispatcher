using Castle.MicroKernel.Registration;
using Castle.Windsor;
using ReliableDispatcher.DataAccess;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;

namespace ReliableDispatcher.Module
{
    public class ReliableDispatcherModule
    {
        private ReliableDispatcherModuleConfig _config;

        public ReliableDispatcherModule(ReliableDispatcherModuleConfig config)
        {
            _config = config;
        }

        public static ReliableDispatcherModuleConfig DefaultConfig => new ReliableDispatcherModuleConfig
        {
            DispatchPipeline = message => throw new InvalidOperationException("No handlers registered in the DispatchPipeline"),
            Container = new WindsorContainer(),
            OutboxDatabaseConnectionString = ConfigurationManager
                .ConnectionStrings[nameof(ReliableDispatcherModuleConfig.OutboxDatabaseConnectionString)]?.ConnectionString
        };

        public void Start()
        {
            _config.Container.Register(
                Component.For<IOutboxRepository>()
                    .ImplementedBy<OutboxRepository>()
                    .DependsOn(Dependency.OnValue("connectionString", _config.OutboxDatabaseConnectionString)));

            Action<IOutboxMessage> handler = message => _config.DispatchPipeline(message);

            _config.Container.Register(
                Component.For<IReliableDispatcher>()
                    .ImplementedBy<ReliableDispatcher>()
                    .DependsOn(Dependency.OnValue<Action<IOutboxMessage>>(handler)));
        }
    }
}
