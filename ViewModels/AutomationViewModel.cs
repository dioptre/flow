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
    public class AutomationViewModel : FlowViewModel
    {
        public new Guid? id { get { return _previousStep.ProjectPlanTaskResponseID; } }
        public string TaskName { get { return _previousTask.TaskName; } }
        public Guid? WorkTypeID { get { return _previousTask.WorkTypeID; } }
        public Guid? WorkCompanyID { get { return _previousTask.WorkCompanyID; } }
        public Guid? WorkContactID { get { return _previousTask.WorkContactID; } }
        public Guid? GraphDataGroupID { get { return _previousTask.GraphDataGroupID; } }
        public new Guid? GraphDataID { get { return _previousTask.GraphDataID; } }
        public Guid? ProjectID { get { return _previousStep.ProjectID; } }
        public Guid? ProjectPlanTaskID { get { return _previousStep.ProjectPlanTaskID; } }
        public Guid? ResponsibleCompanyID { get { return _previousStep.ResponsibleCompanyID; } }
        public Guid? ResponsibleContactID { get { return _previousStep.ResponsibleContactID; } }
        public Guid? ActualTaskID { get { return _previousStep.ActualTaskID; } }
        public Guid? ActualWorkTypeID { get { return _previousStep.ActualWorkTypeID; } }
        public Guid? ActualGraphDataGroupID { get { return _previousStep.ActualGraphDataGroupID; } }
        public Guid? ActualGraphDataID { get { return _previousStep.ActualGraphDataID; } }
        public DateTime? Began { get { return _previousStep.Began; } }
        public DateTime? Completed { get { return _previousStep.Completed; } }
        public decimal? Hours { get { return _previousStep.Hours; } }
        public decimal? EstimatedProRataUnits { get { return _previousStep.EstimatedProRataUnits; } }
        public decimal? EstimatedProRataCost { get { return _previousStep.EstimatedProRataCost; } }
        public decimal? EstimatedValue { get { return _previousStep.EstimatedValue; } }
        public decimal? EstimatedDuration { get { return _previousTask.EstimatedDuration; } }
        public Guid? EstimatedDurationUnitID { get { return _previousTask.EstimatedDurationUnitID; } }
        public decimal? EstimatedLabourCosts { get { return _previousTask.EstimatedLabourCosts; } }
        public decimal? EstimatedCapitalCosts { get { return _previousTask.EstimatedCapitalCosts; } }
        public int DefaultPriority { get { return _previousTask.DefaultPriority; } }
        public Guid? PerformanceMetricParameterID { get { return _previousStep.PerformanceMetricParameterID; } }
        public decimal? PerformanceMetricQuantity { get { return _previousStep.PerformanceMetricQuantity; } }
        public decimal? PerformanceMetricContributedPercent { get { return _previousStep.PerformanceMetricContributedPercent; } }
        public decimal? ApprovedProRataUnits { get { return _previousStep.ApprovedProRataUnits; } }
        public decimal? ApprovedProRataCost { get { return _previousStep.ApprovedProRataCost; } }
        public DateTime? Approved { get { return _previousStep.Approved; } }
        public Guid? ApprovedBy { get { return _previousStep.ApprovedBy; } }
        public string Comments { get { return _previousStep.Comments; } }


        [JsonIgnore]
        private Task _previousTask = null;
        [JsonIgnore]
        public Task PreviousTask
        {
            get
            {
                if (_previousTask == null)
                    _previousTask = lookup.Materialize<Task>();
                return _previousTask;
            }
            set { _previousTask = value; }
        }

        [JsonIgnore]
        private ProjectPlanTaskResponse _previousStep = null;
        [JsonIgnore]
        public ProjectPlanTaskResponse PreviousStep
        {
            get
            {
                if (_previousStep == null)
                    _previousStep = lookup.Materialize<ProjectPlanTaskResponse>();
                return _previousStep;
            }
            set { _previousStep = value; }
        }

        [JsonIgnore]
        public ProjectPlanTaskResponse NextStep { get; set; }

        public Guid? PreviousStepID
        {
            get
            {
                if (PreviousStep != null)
                    return PreviousStep.ProjectPlanTaskResponseID;
                else return null;
            }
        }

        public Guid? NextStepID
        {
            get
            {
                if (NextStep != null)
                    return NextStep.ProjectPlanTaskResponseID;
                else return null;
            }
        }

        public string JSON { get; set; }

        public string Error { get; set; }

        public Guid? ProxyApplicationID { get; set; }

        public Guid? ProxyCompanyID { get; set; }

        public Guid? ProxyContactID { get; set; }

        [JsonIgnore]
        public string Username
        {
            get
            {
                string temp;
                if (lookup.TryGetValue("username", out temp))
                    return temp;
                return null;

            }
        }

        [JsonIgnore]
        public string Password
        {
            get
            {
                string temp;
                if (lookup.TryGetValue("password", out temp))
                    return temp;
                return null;

            }
        }


        [JsonIgnore]
        public string Application
        {
            get
            {
                string temp;
                if (lookup.TryGetValue("application", out temp))
                    return temp;
                return null;

            }
        }


        [JsonIgnore]
        public Guid? TriggerID { get; set; }

        [JsonIgnore]
        private Dictionary<string, string> lookup
        {
            get
            {
                //dynamic task = JObject.Parse(new StreamReader(Request.InputStream).ReadToEnd());
                if (_lookup == null)
                    _lookup = JsonConvert.DeserializeObject<Dictionary<string, string>>(JSON);
                if (_lookup == null)
                    _lookup = new Dictionary<string, string>();
                return _lookup;
            }
            set { _lookup = value; }
        }

        [JsonIgnore]
        private Dictionary<string, string> _lookup = null;
    }

    
}