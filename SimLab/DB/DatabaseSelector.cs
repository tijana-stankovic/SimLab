namespace SimLab.DB;

internal static class DatabaseSelector {
    public static IDatabase Select(DatabaseType databaseType, string connectionString) {
        switch (databaseType) {
            case DatabaseType.PostgreSql:
                return new PostgreSql(connectionString);
            case DatabaseType.Oracle:
                throw new NotSupportedException("Oracle database is not supported yet.");
            case DatabaseType.SqlServer:
                throw new NotSupportedException("SQL Server database is not supported yet.");
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
