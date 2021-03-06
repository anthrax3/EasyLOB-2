﻿using System;
using System.Collections.Generic;
using System.Linq;
using EasyLOB.Data;
using EasyLOB.Library;

namespace EasyLOB.Identity.Data
{
    public partial class RoleDTO : ZDTOBase<RoleDTO, Role>
    {
        #region Properties
               
        public virtual string Id { get; set; }
               
        public virtual string Name { get; set; }
               
        public virtual string Discriminator { get; set; }

        #endregion Properties

        #region Methods
        
        public RoleDTO()
        {
            Id = LibraryDefaults.Default_String;
            Name = LibraryDefaults.Default_String;
            Discriminator = LibraryDefaults.Default_String;
            LookupText = null;
        }
        
        public RoleDTO(
            string id,
            string name,
            string discriminator
        )
        {
            Id = id;
            Name = name;
            Discriminator = discriminator;
            LookupText = null;
        }

        public RoleDTO(IZDataBase data)
        {
            FromData(data);
        }
        
        #endregion Methods

        #region Methods ZDTOBase

        public override Func<RoleDTO, Role> GetDataSelector()
        {
            return x => new Role
            (
                x.Id,
                x.Name,
                x.Discriminator
            );
        }

        public override Func<Role, RoleDTO> GetDTOSelector()
        {
            return x => new RoleDTO
            (
                x.Id,
                x.Name,
                x.Discriminator
            );
        }

        public override void FromData(IZDataBase data)
        {
            if (data != null)
            {
                Role role = (Role)data;
                RoleDTO dto = (new List<Role> { role })
                    .Select(GetDTOSelector())
                    .SingleOrDefault();
                dto.LookupText = role.LookupText;

                LibraryHelper.Clone(dto, this);
            }
        }

        public override IZDataBase ToData()
        {
            return (new List<RoleDTO> { this })
                .Select(GetDataSelector())
                .SingleOrDefault();
        }

        #endregion Methods ZDTOBase
    }
}
