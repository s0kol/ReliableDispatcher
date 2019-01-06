using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReliableDispatcher.Tests.Specs
{
    [TestFixture]
    public class ReliableDispatcherModuleSpec
    {
        [Test]
        public void ReliableDispatcherModule_should_dispatch_messages_remained_in_outbox_due_to_temporary_a_failure()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void ReliableDispatcherModule_should_give_up_on_failed_messages_after_multiple_retries()
        {
            throw new NotImplementedException();
        }
    }
}
