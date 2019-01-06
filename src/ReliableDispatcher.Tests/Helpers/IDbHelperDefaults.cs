namespace ReliableDispatcher.Tests.Helpers
{
    public interface IDbHelperDefaults
    {
        string DefaultDbName { get; }

        string DefaultMdfPath { get; }

        string DefaultServerClause { get; }
    }
}