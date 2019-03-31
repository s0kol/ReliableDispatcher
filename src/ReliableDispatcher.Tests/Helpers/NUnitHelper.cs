using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReliableDispatcher.Tests.Helpers
{
    public class NUnitHelper : IDbHelperDefaults
    {
        public string DefaultDbName { get; set; } 
            = NUnit.Framework.TestContext.CurrentContext.Test.FullName
                .Replace(' ', '_')
                .Replace('.', '_');

        public string DefaultMdfPath { get; set; } = Path.GetTempPath();

        public string DefaultServerClause { get; set; } = @"(localdb)\v11.0";

        public string DefaultApplicationNameClause => Assembly.GetExecutingAssembly().GetName().Name;
    }
}
