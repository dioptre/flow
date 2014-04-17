using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EXPEDIT.License.Models
{
    public class LinqModels
    {
        public class MinimumGraphData
        {
            public Guid? GraphDataID {get;set;}
            public Guid? VersionAntecedentID { get; set; }
            public Guid? VersionOwnerCompanyID { get; set; }
            public Guid? VersionOwnerContactID { get; set; }
        }
    }
}