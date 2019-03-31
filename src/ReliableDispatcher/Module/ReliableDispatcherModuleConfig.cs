using Castle.Windsor;
using System;

namespace ReliableDispatcher.Module
{
    public class ReliableDispatcherModuleConfig
    {
        public IWindsorContainer Container { get; set; }

        public string OutboxDatabaseConnectionString { get; set; }
        public Func<IOutboxMessage, bool> DispatchPipeline { get; set; }
        public DispatchWorkerConfig DispatchWorker { get; set; }

        public class DispatchWorkerConfig
        {
            public TimeSpan OutboxMonitoringInterval { get; set; }
        }
    }
}