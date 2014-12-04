﻿using System.ComponentModel;
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
        public Guid? TriggerID { get; set;} 
        public string CommonName { get; set;}
        public Guid? TriggerTypeID { get; set;}
        public string JsonMethod { get; set;}
        public Guid? JsonProxyApplicationID { get; set;}
        public Guid? JsonProxyContactID { get; set;}
        public Guid? JsonProxyCompanyID { get; set;}
        public Guid? JsonAuthorizedBy { get; set;}
        public string JsonUsername { get; set;}
        public string JsonPassword { get; set;}
        public string JsonPasswordType { get; set;}
        public string JSON { get; set;}
        public string SystemMethod { get; set;}
        public Guid? ConditionID { get; set;}
        public string ExternalURL { get; set;}
        public string ExternalRequestMethod { get; set;}
        public string ExternalFormType { get; set;}
        public bool? PassThrough { get; set;}
        public Guid? GraphDataTriggerID { get; set;}
        public Guid? GraphDataID { get; set;}
        public Guid? GraphDataGroupTriggerID { get; set;}
        public Guid? GraphDataGroupID { get; set;}
        public bool? MergeProjectData { get; set;}
        public bool? OnEnter { get; set;}
        public bool? OnDataUpdate { get; set;}
        public bool? OnExit { get; set;}
        public bool? RunOnce { get; set;}
        public Guid?[] condition
        {
            get { return new Guid?[] { ConditionID }; }
        }

        public string Error { get; set; }


        public TaskViewModel task { get; set; }

    }



}