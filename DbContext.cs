using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;

namespace PGL.EFCFExtensions
{
    [DbConfigurationType(typeof(ModelConfiguration))]
    internal class DbContext : System.Data.Entity.DbContext
    {
        public DbContext()
        {
            Configuration.LazyLoadingEnabled = true;
            Configuration.ProxyCreationEnabled = true;
            Configuration.AutoDetectChangesEnabled = true;
            Configuration.ValidateOnSaveEnabled = true;
        }

        /// <summary>
        ///     Adds all ModelBase sub-classed types as tables to the SQL database. Sets DateTime properties to use
        ///     datetime2 SQL data type columns. Removes all pluralization from sql object naming. Disables EF proxies. Auto
        ///     migrates SQL objects when there ModelBase source definitions are changed, does not allow changes to incur
        ///     data-loss unless DDD.App.LinkCheck was a debug build.
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            foreach (Type _Type in AppDomain.CurrentDomain.GetAssemblies()
                                            .SelectMany(a => a.GetTypes()
                                                              .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(ModelBase)))))
                modelBuilder.RegisterEntityType(_Type);

            //The .Net DateTime datatype may have values outside the SQL DateTime data type, SQL datetime2 encompasses all .Net DateTime data types
            modelBuilder.Properties<DateTime>().Configure(c => c.HasColumnType("datetime2"));

            //TODO:Set some string properties to be a fixed width so column compression can be applied to them

            // reflection is used heavily throughout the solution, names must remain the same between models, poco & sql statements
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Conventions.Remove<PluralizingEntitySetNameConvention>();
            modelBuilder.Conventions.Remove<TableAttributeConvention>();

            base.OnModelCreating(modelBuilder);
        }

        private class ModelConfiguration : DbConfiguration
        {
            public ModelConfiguration()
            {
                SetDatabaseInitializer(new MigrateDatabaseToLatestVersion<DbContext, SqlDbMigrationsConfiguration<DbContext>>(true));
            }
        }

        /// <summary>
        ///     assigns the DocTypeName to the primary table & it's child table's Entity Framework Code First
        ///     __MigrationHistory.ContextKey
        /// </summary>
        private class SqlDbMigrationsConfiguration<TContext> : DbMigrationsConfiguration<TContext>
            where TContext : System.Data.Entity.DbContext
        {
            public SqlDbMigrationsConfiguration()
            {
                AutomaticMigrationsEnabled = true;
                AutomaticMigrationDataLossAllowed = false;
            }
        }
    }
}