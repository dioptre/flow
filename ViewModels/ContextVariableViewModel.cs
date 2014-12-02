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
    public class ContextVariableViewModel
    {
        public Guid? id { get; set; }
        public Guid? FormID { get; set; }
        public Guid? GraphDataID { get; set; }
        public string CommonName { get; set; }
       

    }


}