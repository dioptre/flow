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
        public new Guid? id { get { return PreviousStep.ProjectPlanTaskResponseID; } }
        public string TaskName { get { return PreviousTask.TaskName; } }
        public Guid? WorkTypeID { get { return PreviousTask.WorkTypeID; } }
        public Guid? WorkCompanyID { get { return PreviousTask.WorkCompanyID; } }
        public Guid? WorkContactID { get { return PreviousTask.WorkContactID; } }
        public Guid? GraphDataGroupID { get { return TaskID ?? PreviousStep.ActualGraphDataGroupID; } }
        public new Guid? GraphDataID { get { return PreviousTask.GraphDataID ?? PreviousStep.ActualGraphDataID; } }
        public Guid? ProjectID { get { return PreviousStep.ProjectID; } }
        public Guid? ProjectPlanTaskID { get { return PreviousStep.ProjectPlanTaskID; } }
        public Guid? ResponsibleCompanyID { get { return PreviousStep.ResponsibleCompanyID; } }
        public Guid? ResponsibleContactID { get { return PreviousStep.ResponsibleContactID; } }
        public Guid? ActualTaskID { get { return PreviousStep.ActualTaskID; } }
        public Guid? ActualWorkTypeID { get { return PreviousStep.ActualWorkTypeID; } }
        public Guid? ActualGraphDataGroupID { get { return PreviousStep.ActualGraphDataGroupID; } }
        public Guid? ActualGraphDataID { get { return PreviousStep.ActualGraphDataID; } }
        public DateTime? Began { get { return PreviousStep.Began; } }
        public DateTime? Completed { get { return PreviousStep.Completed; } }
        public decimal? Hours { get { return PreviousStep.Hours; } }
        public decimal? EstimatedProRataUnits { get { return PreviousStep.EstimatedProRataUnits; } }
        public decimal? EstimatedProRataCost { get { return PreviousStep.EstimatedProRataCost; } }
        public decimal? EstimatedValue { get { return PreviousStep.EstimatedValue ?? PreviousTask.EstimatedValue; } }
        public decimal? EstimatedDuration { get { return PreviousTask.EstimatedDuration; } }
        public Guid? EstimatedDurationUnitID { get { return PreviousTask.EstimatedDurationUnitID; } }
        public decimal? EstimatedLabourCosts { get { return PreviousTask.EstimatedLabourCosts; } }
        public decimal? EstimatedCapitalCosts { get { return PreviousTask.EstimatedCapitalCosts; } }
        public int DefaultPriority { get { return PreviousTask.DefaultPriority; } }
        public Guid? PerformanceMetricParameterID { get { return PreviousStep.PerformanceMetricParameterID; } }
        public decimal? PerformanceMetricQuantity { get { return PreviousStep.PerformanceMetricQuantity; } }
        public decimal? PerformanceMetricContributedPercent { get { return PreviousStep.PerformanceMetricContributedPercent; } }
        public decimal? ApprovedProRataUnits { get { return PreviousStep.ApprovedProRataUnits; } }
        public decimal? ApprovedProRataCost { get { return PreviousStep.ApprovedProRataCost; } }
        public DateTime? Approved { get { return PreviousStep.Approved; } }
        public Guid? ApprovedBy { get { return PreviousStep.ApprovedBy; } }
        public string Comments { get { return PreviousStep.Comments; } }

        public Guid? VersionAntecedentID { get { return PreviousStep.VersionAntecedentID; } }
        public Guid? VersionUpdatedBy { get { return PreviousStep.VersionUpdatedBy; } }
        public Guid? VersionOwnerContactID { get { return PreviousStep.VersionOwnerContactID; } }
        public Guid? VersionOwnerCompanyID { get { return PreviousStep.VersionOwnerCompanyID; } }
        public DateTime? VersionUpdated { get { return PreviousStep.VersionUpdated; } }


        public Guid? TaskID { get { return (PreviousTask.TaskID == Guid.Empty) ? default(Guid?) : PreviousTask.TaskID; } }

        public bool? IncludeContent { get; set; }

        public Guid? Project { get { return ProjectID; } }

        public long? Row { get; set; }
        public int? TotalRows { get; set; }
        public int? Score { get; set; }
        public string ProjectName { get; set; }
        public string ProjectCode { get; set; }
        public string GraphDataGroupName { get; set; }
        public string GraphName { get { return label; } set { label = value; } }
        public string GraphContent { get { return content; } set { content = value; } }
        public string LastEditedBy { get; set; }

        [JsonIgnore]
        private WorkflowInstance _previousWorkflowInstance = null;
        [JsonIgnore]
        public WorkflowInstance PreviousWorkflowInstance
        {
            get
            {
                if (_previousWorkflowInstance == null)
                    _previousWorkflowInstance = lookup.Materialize<WorkflowInstance>();
                return _previousWorkflowInstance;
            }
            set { _previousWorkflowInstance = value; }
        }


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
                if (_lookup == null && !string.IsNullOrWhiteSpace(JSON))
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