namespace ReliableDispatcher.Tests.Helpers
{
    public static class NUnitHelperExtensions
    {
        public static DbHelper DbHelper(this IDbHelperDefaults dbHelperDefaults)
        {
            return new DbHelper(dbHelperDefaults);
        }
    }
}
