using DbUp;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReliableDispatcher.DataAccess.Migrations
{
    public class DatabaseMigrator
    {
        public static void RunMigrations(string connectionString)
        {
            var result = DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(typeof(DatabaseMigrator).Assembly)
                .LogToConsole()
                .Build()
                .PerformUpgrade();
        }
    }
}
