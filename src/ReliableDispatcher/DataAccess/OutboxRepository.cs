using Dapper;
using System;
using System.Data.SqlClient;
using System.Linq;

namespace ReliableDispatcher.DataAccess
{
    public class OutboxRepository
    {
        private readonly string _connectionString;

        public OutboxRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public void CreateMessage(Guid messageId, string messageBody)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Execute(
                    "INSERT INTO Outbox(Id, Body) " +
                    "VALUES(@messageId, @messageBody)",
                    new { messageId, messageBody });
            }
        }

        public IOutboxMessage GetAndMarkAsDispatchedIfAvailableForDispatching(Guid messageId)
        {
            const int LockRequestTimeoutPeriodExceededSqlErrorNumber = 1222;
            const string query = "UPDATE Outbox WITH(NOWAIT) " +
                "SET DispatchedDate = getdate(), " +
                "   DispatchAttempts = DispatchAttempts + 1 " +
                "OUTPUT inserted.* " +
                "WHERE Id = @messageId AND DispatchedDate IS NULL";
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var result = connection
                        .Query<OutboxMessage>(query, new { messageId })
                        .Cast<OutboxMessage?>()
                        .SingleOrDefault();

                    return result;
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number != LockRequestTimeoutPeriodExceededSqlErrorNumber)
                {
                    throw;
                }
            }

            return null;
        }

        public void RevertMarkingAsDispatchedNonBlocking(Guid messageId)
        {
            const int LockRequestTimeoutPeriodExceededSqlErrorNumber = 1222;
            const string query = "UPDATE Outbox WITH(NOWAIT) " +
                "SET DispatchedDate = NULL " +
                "WHERE Id = @messageId";

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Execute(query, new { messageId });
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number != LockRequestTimeoutPeriodExceededSqlErrorNumber)
                {
                    throw;
                }
            }
        }

        public bool IsMessageReadyToBeDispatchedNonBlocking(Guid messageId)
        {
            const string query = "SELECT 1 " +
                    "FROM Outbox WITH(NOLOCK) " +
                    "WHERE Id = @messageId " +
                    "AND DispatchedDate IS NULL";

            using (var connection = new SqlConnection(_connectionString))
            {
                return connection
                    .QuerySingleOrDefault(query, new { messageId }) != null;
            }
        }

        public bool IsMessageInOutboxNonBlocking(Guid messageId)
        {
            const string query = "SELECT 1 " +
                    "FROM Outbox WITH(NOLOCK) " +
                    "WHERE Id = @messageId ";

            using (var connection = new SqlConnection(_connectionString))
            {
                var result = connection
                    .QuerySingleOrDefault(query, new { messageId });

                return result != null;
            }
        }
    }
}
