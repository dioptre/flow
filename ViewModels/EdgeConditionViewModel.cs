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
    public class EdgeConditionViewModel : ICondition
    {
        public Guid? id { get; set; }
        public bool? OverrideProjectDataWithJsonCustomVars { get; set; }
        public string Condition { get; set; }

        public int? Grouping { get; set; }
        public int? Sequence { get; set; }
        public string JoinedBy { get; set; }
        public Guid? ConditionID { get; set; }
        public Guid? GraphDataRelationID { get; set; }
        
        public string JSON { get; set; }

        public string Error { get; set; }


        public EdgeConditionViewModel edgeCondition { get; set; }

    }

    public interface ICondition
    {
        bool? OverrideProjectDataWithJsonCustomVars { get; set; }
        string Condition { get; set; }
    }


}