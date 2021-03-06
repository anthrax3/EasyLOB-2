using EasyLOB.Application;
//using EasyLOB.AuditTrail;
using EasyLOB.Data;
//using EasyLOB.Log;
//using EasyLOB.Security;

namespace EasyLOB.Activity.Application
{
    public class ActivityGenericApplication<TEntity> : GenericApplication<TEntity>, IActivityGenericApplication<TEntity>
        where TEntity : class, IZDataBase
    {
        #region Methods

        public ActivityGenericApplication(IActivityUnitOfWork unitOfWork, IDIManager diManager)
            : base(unitOfWork, diManager)
        {
        }

        #endregion Methods
    }
}