using ReliableDispatcher.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ReliableDispatcher
{
    public class DispatchWorker
    {
        private readonly IReliableDispatcher _dispatcher;

        public DispatchWorker(IReliableDispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public void Start(int outboxMonitoringIntervalMilliseconds = 10000)
        {
            new Thread(() =>
            {
                var autoResetEvent = new AutoResetEvent(true);

                while (true)
                {
                    try
                    {
                        autoResetEvent.WaitOne(outboxMonitoringIntervalMilliseconds);

                        var messageIds = _dispatcher.FindMessagesToDispatch();

                        if (messageIds.Any())
                        {
                            foreach (var messageId in messageIds)
                            {
                                _dispatcher.DispatchMessage(messageId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }).Start();
        }
    }
}
