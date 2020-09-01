#if NET40
using System.Data.Entity;
using System.Data.Entity.Migrations;

namespace PGL.EFAutoMigrator
{
    /// <summary>
    ///     assigns the DocTypeName to the primary table & it's child table's Entity Framework Code First
    ///     __MigrationHistory.ContextKey
    /// </summary>
    internal class SqlDbMigrationsConfiguration<TContext> : DbMigrationsConfiguration<TContext>
        where TContext : DbContext
    {
        public SqlDbMigrationsConfiguration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = false;
        }
    }
}
#endif