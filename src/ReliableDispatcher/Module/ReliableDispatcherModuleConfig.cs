using Castle.Windsor;

namespace ReliableDispatcher.Module
{
    public class ReliableDispatcherModuleConfig
    {
        public IWindsorContainer Container { get; set; }

        public string OutboxDatabaseConnectionString { get; set; }
    }
}