using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NKD.Module.BusinessObjects;
using NKD.Helpers;

namespace EXPEDIT.Flow.ViewModels
{

    [JsonObject]
    public class ProjectViewModel
    {
        public Guid? id { get; set; }
        public string ProjectName { get; set; }
        public string ProjectCode { get; set; }
        public Guid? ClientCompanyID { get; set; }
        public Guid? ClientContactID { get; set; }
        public Guid[] ProjectData { get; set; }
        public Guid[] Steps { get; set; }

        public string JSON { get; set; }

        public string Error { get; set; }

    }

    
}