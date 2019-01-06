using Castle.Windsor;
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
            Container = new WindsorContainer(),
            OutboxDatabaseConnectionString = ConfigurationManager.ConnectionStrings[nameof(ReliableDispatcherModuleConfig.OutboxDatabaseConnectionString)].ConnectionString
        };

        public void Start()
        {

        }
    }
}
