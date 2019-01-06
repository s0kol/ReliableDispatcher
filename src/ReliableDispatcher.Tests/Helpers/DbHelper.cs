using System.Data.SqlClient;
using System.IO;

namespace ReliableDispatcher.Tests.Helpers
{
    public class DbHelper
    {
        private const int SqlExceptionDatabaseAlreadyExists = 1801;
        private const int SqlExceptionFileAlreadyExists = 5170;
        private readonly IDbHelperDefaults _defaults;

        public DbHelper(IDbHelperDefaults defaults)
        {
            _defaults = defaults;
        }

        public DbHelper CreateDatabase(out string connectionString, string dbName = null)
        {
            dbName = dbName ?? _defaults.DefaultDbName;

            var mdfPath = Path.Combine(_defaults.DefaultMdfPath, $@"{dbName}_data.mdf");
            var ldfPath = Path.Combine(_defaults.DefaultMdfPath, $@"{dbName}_log.ldf");
            var sql = string.Format($@"
                CREATE DATABASE [{dbName}]
                ON PRIMARY (NAME={dbName}_data, FILENAME = '{mdfPath}')
                LOG ON (NAME={dbName}_log, FILENAME = '{ldfPath}')");

            var connectionStringTemplate = $@"Server={_defaults.DefaultServerClause};Integrated Security=True;Connect Timeout=30";
            
            using (var connection = new SqlConnection(connectionStringTemplate))
            {
                connection.Open();

                try
                {
                    new SqlCommand(sql, connection).ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    if (ex.Number == SqlExceptionDatabaseAlreadyExists || ex.Number == SqlExceptionFileAlreadyExists)
                    {
                        if (ex.Number == SqlExceptionDatabaseAlreadyExists)
                        {
                            new SqlCommand($@"
                            ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                            DROP DATABASE [{dbName}];
                            ", connection).ExecuteNonQuery();
                        }

                        File.Delete(mdfPath);
                        File.Delete(ldfPath);

                        new SqlCommand(sql, connection).ExecuteNonQuery();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            var builder = new SqlConnectionStringBuilder(connectionStringTemplate);

            builder.AttachDBFilename = mdfPath;

            connectionString = builder.ConnectionString;

            return this;
        }


    }
}
