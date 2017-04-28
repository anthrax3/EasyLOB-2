﻿using LinqToDB;
using LinqToDB.Data;
using EasyLOB.Activity.Data;

namespace EasyLOB.Activity.Persistence
{
    public class ActivityLINQ2DB : DataConnection
    {
        #region Methods
    
        public ActivityLINQ2DB()
            //: base("Activity")
            : base(EasyLOB.Library.Mvc.MultiTenantHelper.GetConnectionName("Activity")) // !?! Multi-Tenant
        {
            ActivityLINQ2DBMap.ActivityMap(MappingSchema);
            ActivityLINQ2DBMap.ActivityRoleMap(MappingSchema);
        }

        public ITable<EasyLOB.Activity.Data.Activity> Activity
        {
            get { return GetTable<EasyLOB.Activity.Data.Activity>(); }
        }

        public ITable<ActivityRole> ActivityRole
        {
            get { return GetTable<ActivityRole>(); }
        }

        #endregion Methods
    }
}