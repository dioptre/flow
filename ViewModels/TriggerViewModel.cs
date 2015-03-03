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
    public class TriggerViewModel
    {
        public Guid? id { get; set; }
        public Guid? TriggerID { get; set; }
        public string CommonName { get; set; }
        public Guid? TriggerTypeID { get; set; }
        public string JsonMethod { get; set; }
        public Guid? JsonProxyApplicationID { get; set; }
        public Guid? JsonProxyContactID { get; set; }
        public Guid? JsonProxyCompanyID { get; set; }
        public Guid? JsonAuthorizedBy { get; set; }
        public string JsonUsername { get; set; }
        public string JsonPassword { get; set; }
        public string JsonPasswordType { get; set; }
        public string JSON { get; set; }
        public string SystemMethod { get; set; }
        public string ExternalURL { get; set; }
        public string ExternalRequestMethod { get; set; }
        public string ExternalFormType { get; set; }
        public bool? PassThrough { get; set; }
        public int? DelaySeconds { get; set; }
        public int? DelayDays { get; set; }
        public int? DelayWeeks { get; set; }
        public int? DelayMonths { get; set; }
        public int? DelayYears { get; set; }
        public DateTime? DelayUntil { get; set; }

        public int? RepeatAfterDays	{get;set;}
        public int? Repeats	{get;set;}

        public Guid? ConditionID { get; set; }
        public string ConditionJSON { get; set; }
        public bool? OverrideProjectDataWithJsonCustomVars { get; set; }
        public string Condition { get; set; }


        public string Error { get; set; }


        public TriggerViewModel trigger { get; set; }


    }

    public class TriggerGraphViewModel : TriggerViewModel
    {
        public Guid? TriggerGraphID { get; set; }
        public Guid? GraphDataID { get; set; }
        public Guid? GraphDataGroupID { get; set; }
        public bool? MergeProjectData { get; set; }
        public bool? OnEnter { get; set; }
        public bool? OnDataUpdate { get; set; }
        public bool? OnExit { get; set; }

        public TriggerGraphViewModel triggerGraph { get; set; }

        [JsonIgnore]
        public TriggerGraphViewModel[] triggerGraphs { get; set; }
    }



}