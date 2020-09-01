using System.Collections;
using System.Linq;
using System.Reflection;
#if NET40
using System.Data.Entity;
#else
using Microsoft.EntityFrameworkCore;
#endif

namespace PGL.EFAutoMigrator
{
    /// <summary>
    ///     Allows for recursive saves of entities to the database. Saves include insert & updates of existing objects based on
    ///     the <see cref="CfModelBase.Id" />. These extension methods only work with composite type properties; not
    ///     fields. Base type all business objects (data vestals) should stem from.
    /// </summary>
    public abstract class CfModelBase
    {
        public int Id { get; set; }

        /// <summary>
        ///     attaches the objects graph & it's child objects via navigation properties back the the DbContext & executes
        ///     Adds/Updates. This object graph attacher works specifically with CfModelBase & generic lists of them
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="autoSaveChanges">
        ///     should a <see cref="DbContext.SaveChanges()" /> call be issued to persist this to the
        ///     database? When no; you must call the <see cref="DbContext.SaveChanges()" /> manually
        /// </param>
        public void Save(DbContext dbContext, bool autoSaveChanges = true)
        {
            if (Id == default)
            {
#if NET40
                dbContext.Set(GetType()).Add(this);
#else
                dbContext.Add(this);
#endif
            }
            else
            {
                // ReSharper disable once SuggestVarOrType_Elsewhere
                var entry = dbContext.Entry(this);

                if (entry.State == EntityState.Detached)
                {
                    CfModelBase cfModelBase =
#if NET40
                        dbContext.Set(GetType()).Local.AsQueryable().Cast<CfModelBase>().FirstOrDefault(modelBase => modelBase.Id == Id);
#else
                        (CfModelBase) dbContext.Find(GetType(), Id);
#endif

                    if (cfModelBase != null)
                        dbContext.Entry((object) cfModelBase).CurrentValues.SetValues(this);
                    else
                        entry.State = EntityState.Modified;
                }
            }

            foreach (PropertyInfo propertyInfo in GetType().GetProperties())
                if (propertyInfo.GetValue(this, null) != null)
                    if (propertyInfo.PropertyType.IsSubclassOf(typeof(CfModelBase)))
                    {
                        ((CfModelBase) propertyInfo.GetValue(this, null))?.Save(dbContext, false);
                    }
                    else if (propertyInfo.PropertyType.GetInterface("IList") != null)
                    {
                        IList value = (IList) propertyInfo.GetValue(this, null);
                        if (value != null)
                            foreach (CfModelBase linkCheckInfoBase in value.OfType<CfModelBase>())
                                linkCheckInfoBase.Save(dbContext, false);
                    }

            if (autoSaveChanges)
                dbContext.SaveChanges();
        }
    }
}