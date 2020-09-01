using System;
using System.Linq;
#if NETCOREAPP3_1
using System.Diagnostics;
using System.Threading.Tasks;
using CentridNet.EFCoreAutoMigrator;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
#else
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
#endif


namespace PGL.EFAutoMigrator
{
#if NETCOREAPP3_1
    public class CfDbContext : DbContext
    {
        public CfDbContext() : this(new DbContextOptions<DbContext>())
        {
        }

        public CfDbContext([NotNull] DbContextOptions options, bool allowDataLoss = false) : base(options)
        {
            ChangeTracker.AutoDetectChangesEnabled = true;

            // Run auto-migration like the old EF6 did
            if (DisableModelDiscovery)
            {
                Database.EnsureCreated();
            }
            else
            {
                new EmptyCfDbContext(options).Dispose();
                AutoMigrateMyDb(this, allowDataLoss).Wait();
            }
        }


        private bool DisableModelDiscovery => GetType().Name == nameof(EmptyCfDbContext);

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLazyLoadingProxies();
            base.OnConfiguring(optionsBuilder);
        }

        /// <summary>
        ///     Adds all CfModelBase sub-classed types as tables to the SQL database. Sets DateTime properties to use
        ///     datetime2 SQL data type columns. Removes all pluralization from sql object naming. Disables EF proxies. Auto
        ///     migrates SQL objects when there CfModelBase source definitions are changed, does not allow changes to incur
        ///     data-loss unless DDD.App.LinkCheck was a debug build.
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (!DisableModelDiscovery)
                foreach (Type type in AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes()
                        .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(CfModelBase)))))
                    modelBuilder.Entity(type).ToTable(type.Name);

            // The .Net DateTime datatype may have values outside the SQL DateTime data type, SQL datetime2 encompasses all .Net DateTime data types
            // TODO: modelBuilder.Properties<DateTime>().Configure(c => c.HasColumnType("datetime2"));

            base.OnModelCreating(modelBuilder);
        }

        private static async Task AutoMigrateMyDb(DbContext dbContext, bool allowDataLoss)
        {
            EFCoreAutoMigrator efCoreAutoMigrator = new EFCoreAutoMigrator(dbContext, new Logger())
                .ShouldAllowDestructive(allowDataLoss);

            MigrationScriptExecutor migrationScriptExecutor = await efCoreAutoMigrator.PrepareMigration();

            // Checking if there are migrations
            if (migrationScriptExecutor.HasMigrations())
                // Migrating
                switch (await migrationScriptExecutor.MigrateDB())
                {
                    case MigrationResult.Migrated:
                        Trace.WriteLine("Completed successfully.");
                        break;
                    case MigrationResult.Noop:
                        Trace.WriteLine("Completed. There was nothing to migrate.");
                        break;
                    case MigrationResult.ErrorMigrating:
                        Trace.WriteLine("Error occurred whilst migrating.");
                        break;
                }
        }

        /// <summary>
        ///     This class is supports creating an empty database when one does not exist.
        /// </summary>
        private class EmptyCfDbContext : CfDbContext
        {
            public EmptyCfDbContext([NotNull] DbContextOptions options) : base(options)
            {
            }
        }
    }

#else
    [DbConfigurationType(typeof(ModelConfiguration))]
    public class CfDbContext : DbContext
    {
        public CfDbContext()
        {
            Configuration.LazyLoadingEnabled = true;
            Configuration.ProxyCreationEnabled = true;
            Configuration.AutoDetectChangesEnabled = true;
            Configuration.ValidateOnSaveEnabled = true;
        }

        /// <summary>
        ///     Adds all CfModelBase sub-classed types as tables to the SQL database. Sets DateTime properties to use
        ///     datetime2 SQL data type columns. Removes all pluralization from sql object naming. Disables EF proxies. Auto
        ///     migrates SQL objects when there CfModelBase source definitions are changed, does not allow changes to incur
        ///     data-loss unless DDD.App.LinkCheck was a debug build.
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            foreach (Type _Type in AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes()
                    .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(CfModelBase)))))
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
    }

#endif
}