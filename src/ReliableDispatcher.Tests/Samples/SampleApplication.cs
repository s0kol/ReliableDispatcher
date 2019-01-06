using ReliableDispatcher.Module;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReliableDispatcher.Tests.Samples
{
    public class SampleApplication
    {
        private readonly ReliableDispatcherModuleConfig _rdmConfig;

        public SampleApplication(ReliableDispatcherModuleConfig rdmConfig = null)
        {
            _rdmConfig = rdmConfig ?? ReliableDispatcherModule.DefaultConfig;
        }

        public void Configure(Action<ReliableDispatcherModuleConfig> configure)
        {
            configure(_rdmConfig);
        }

        public void Start()
        {
            new ReliableDispatcherModule(_rdmConfig).Start();
        }
    }
}
