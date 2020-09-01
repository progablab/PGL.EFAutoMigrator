#if NET40
using System.Data.Entity;

namespace PGL.EFAutoMigrator
{
    internal class ModelConfiguration : DbConfiguration
    {
        public ModelConfiguration()
        {
            SetDatabaseInitializer(new MigrateDatabaseToLatestVersion<CfDbContext, SqlDbMigrationsConfiguration<CfDbContext>>(true));
        }
    }
}
#endif