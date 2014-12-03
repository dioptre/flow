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
    public class TaskViewModel 
    {
        public Guid? id { get; set; }
        
        public Guid? TaskID { get; set;}
        public string TaskName { get; set;}
        public Guid? WorkTypeID { get; set;}
        public Guid? WorkCompanyID { get; set;}
        public Guid? WorkContactID { get; set;}
        public Guid? GraphDataGroupID { get; set;}
        public Guid? GraphDataID { get; set;}
        public int DefaultPriority { get; set;}
        public decimal? EstimatedDuration { get; set;}
        public Guid? EstimatedDurationUnitID { get; set;}
        public decimal? EstimatedLabourCosts { get; set;}
        public decimal? EstimatedCapitalCosts { get; set;}
        public decimal? EstimatedValue { get; set; }
        public decimal? EstimatedIntangibleValue { get; set; }
        public decimal? EstimatedRevenue { get; set;}
        public Guid? PerformanceMetricParameterID { get; set;}
        public decimal? PerformanceMetricQuantity { get; set; }
        public string Comment { get; set;}
       
        
        public string JSON { get; set; }

        public string Error { get; set; }


        public TaskViewModel task { get; set; }

    }



}