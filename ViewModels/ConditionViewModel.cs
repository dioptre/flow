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
    public class ConditionViewModel 
    {
        public Guid? id { get; set; }
        public bool? OverrideProjectDataWithJsonCustomVars { get; set; }
        public string Precondition { get; set; }

       
        
        public string JSON { get; set; }

        public string Error { get; set; }


        public ConditionViewModel condition { get; set; }

    }



}