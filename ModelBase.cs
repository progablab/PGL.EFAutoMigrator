using System.Collections;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reflection;

namespace PGL.EFCFExtensions
{
    /// <summary>
    ///     Allows for recursive saves of entities to the database. Saves include insert & updates of existing objects based on
    ///     the <see cref="ModelBase.Id" />. These extension methods only work with composite type properties; not fields. Base
    ///     type all business objects (data vestals) should stem from.
    /// </summary>
    public abstract class ModelBase
    {
        public int Id { get; set; }

        internal ModelBase attachedEntity(System.Data.Entity.DbContext dbContext)
        {
            return set(dbContext)
                .Local.AsQueryable()
                .Cast<ModelBase>()
                .FirstOrDefault(m => m.Id == Id);
        }

        internal DbEntityEntry entry(System.Data.Entity.DbContext dbContext)
        {
            return dbContext.Entry(this);
        }

        /// <summary>
        ///     attaches the objects graph & it's child objects via navigation properties back the the DBContext & executes
        ///     Adds/Updates. This object graph attacher works specifically with ModelBase & generic lists of them
        /// </summary>
        /// <param name="o"></param>
        /// <param name="dbContext"></param>
        /// <param name="autoSaveChanges">
        ///     should a <see cref="System.Data.Entity.DbContext.SaveChanges" /> call be issued to persist this to the
        ///     database? When no; you must call the <see cref="System.Data.Entity.DbContext.SaveChanges" /> manually
        /// </param>
        public void Save(System.Data.Entity.DbContext dbContext, bool autoSaveChanges = true)
        {
            if (Id == default(int))
            {
                set(dbContext)
                    .Add(this);
            }
            else
            {
                if (entry(dbContext)
                    .State == EntityState.Detached)
                    if (attachedEntity(dbContext) != null)
                        dbContext.Entry(attachedEntity(dbContext))
                            .CurrentValues.SetValues(this);
                    else
                        entry(dbContext)
                            .State = EntityState.Modified;
            }

            foreach (PropertyInfo _PropertyInfo in GetType()
                .GetProperties())
                if (_PropertyInfo.GetValue(this,
                    null) != null)
                    if (_PropertyInfo.PropertyType.IsSubclassOf(typeof(ModelBase)))
                        ((ModelBase) _PropertyInfo.GetValue(this,
                            null)).Save(dbContext,
                            false);
                    else if (_PropertyInfo.PropertyType.GetInterface("IList") != null)
                        foreach (ModelBase _LinkCheckInfoBase in ((IList) _PropertyInfo.GetValue(this,
                            null)).OfType<ModelBase>())
                            _LinkCheckInfoBase.Save(dbContext,
                                false);


            if (autoSaveChanges)
                dbContext.SaveChanges();
        }

        internal DbSet set(System.Data.Entity.DbContext dbContext)
        {
            return dbContext.Set(GetType());
        }
    }
}