using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace EXPEDIT.Flow.ViewModels
{
    [Flags]
    public enum SecurityType : uint
    {
        BlackList=0x01,
        WhiteList=0x2,
    }

    [JsonObject]
    public class SecurityViewModel 
    {
        public uint? securityTypeID { get; set; }
        public SecurityType? securityType { get; set; }
        public SecurityViewModel security { get; set; }
        public Guid? id { get; set; }
        public Guid? OwnerContactID { get; set; }
        public Guid? OwnerCompanyID { get; set; }
        public string OwnerTableType { get; set; }
        [JsonIgnore]
        public Guid? OwnerReferenceID { get; set; }
        public Guid? ReferenceID { get { return OwnerReferenceID; } set { OwnerReferenceID = value; } }
        public string ReferenceName { get; set; }
        public Guid? AccessorCompanyID { get; set; }
        public string AccessorCompanyName { get; set; }
        public Guid? AccessorContactID { get; set; }
        public string AccessorContactName { get; set; }
        public Guid? AccessorRoleID { get; set; }
        public string AccessorRoleName { get; set; }
        public Guid? AccessorProjectID { get; set; }
        public string AccessorProjectName { get; set; }
        public DateTime? Updated { get; set; }
        public bool? CanCreate { get; set; }
        public bool? CanRead { get; set; }
        public bool? CanUpdate { get; set; }
        public bool? CanDelete { get; set; }

    }

}