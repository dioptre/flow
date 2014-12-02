using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using JetBrains.Annotations;
using Orchard;
using System.Security.Principal;
using EXPEDIT.Flow.ViewModels;

using Jint;
using System.Text;
using System.Security.Cryptography;
using System.Transactions;
using NKD.Module.BusinessObjects;
using NKD.Services;
using NKD.Models;
using NKD.Helpers;
using EXPEDIT.Share.Helpers;
using EXPEDIT.Share.Services;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace EXPEDIT.Flow.Services {

    [UsedImplicitly]
    public class AutomationService : IAutomationService
    {
        private readonly IOrchardServices _services;
        private readonly IUsersService _users;
        private readonly IWorkflowService _wf;
        private Guid? contactID = null;
        private Guid? companyID = null;
        private Guid? applicationID = null;



        private Guid? ApplicationID
        {
            get
            {
                if (!applicationID.HasValue)
                {
                    if (_services.WorkContext.CurrentUser != null)
                        applicationID = _users.ApplicationID;
                }
                return applicationID;
            }
            set
            {
                applicationID = value;
            }
        }

        private Guid? ContactID
        {
            get
            {
                if (!contactID.HasValue)
                {
                    if (_services.WorkContext.CurrentUser != null)
                        contactID = _users.ContactID;
                }
                return contactID;
            }
            set
            {
                contactID = value;
            }
        }


        private Guid? CompanyID
        {
            get
            {
                if (!companyID.HasValue)
                {
                    if (_services.WorkContext.CurrentUser != null)
                        companyID = _users.DefaultContactCompanyID;
                }
                return companyID;
            }
            set
            {
                companyID = value;
            }
        }

        public AutomationService(
            IOrchardServices orchardServices,
            IUsersService users,
            IWorkflowService wf
            )
        {
            _users = users;
            _services = orchardServices;
            _wf = wf;
        }


        public bool EvaluateCondition() {
            dynamic p = new  { Name = "Mickey Mouse"};

            var result = new Engine()
                    .SetValue("p", p)
                    .Execute("p.Name === 'Mickey Mouse'")
                    .GetCompletionValue() // get the latest statement completion value
                    .ToObject()
                    ;
            return false; 
        }

        public bool ExecuteMethod(AutomationViewModel m, string method) {
            return false;
        }

        public bool Authenticate(AutomationViewModel m, string method)
        {
            Guid applicationID;
            if (!Guid.TryParse(m.Application, out applicationID))
                return false;
            byte[] bytes = Encoding.UTF8.GetBytes(m.Password);
            SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(bytes);
            string hashString = string.Empty;
            foreach (byte x in hash)
            {
                hashString += String.Format("{0:x2}", x);
            }
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                var trigger = d.Triggers.FirstOrDefault(f => f.JsonProxyApplicationID == applicationID && f.JsonUsername == m.Username && f.JsonPassword == hashString && f.JsonMethod == method
                    && f.VersionDeletedBy == null && f.Version == 0);
                if (trigger == null)
                    return false;
                ApplicationID = applicationID;
                ContactID = trigger.JsonProxyContactID;
                CompanyID = trigger.JsonProxyCompanyID;
                return true;

            }
        }


        public bool Authorize(AutomationViewModel m, Guid? gid, ActionPermission permission, Type typeToCheck)
        {
            return Authorize(gid, permission, typeToCheck);
        }

        public bool Authorize(Guid? gid, ActionPermission permission, Type typeToCheck)
        {
            //var contact = overrideContactID ?? _users.ContactID;
            //var application = overrideApplicationID ?? _users.ApplicationID;
            //var company = m.ProxyCompanyID ?? _users.DefaultContactCompanyID;
            var contact = ContactID;
            var application = ApplicationID;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                var table = d.GetTableName(typeToCheck);
                if (gid.HasValue)
                {
                    return _users.CheckPermission(new SecuredBasic
                    {
                        AccessorApplicationID = application,
                        AccessorContactID = contact,
                        OwnerTableType = table,
                        OwnerReferenceID = gid.Value
                    }, permission);
                }
                else
                {
                    return _users.CheckPermission(new SecuredBasic
                    {
                        AccessorApplicationID = application,
                        AccessorContactID = contact,
                        OwnerTableType = table
                    }, permission);
                }
            }
        }

        public bool QuenchStep(NKDC d, AutomationViewModel m)
        {
            m.PreviousStep = d.ProjectPlanTaskResponses.Where(f => f.ProjectPlanTaskResponseID == m.PreviousStepID && f.Version == 0 && f.VersionDeletedBy == null).SingleOrDefault();
            if (m.PreviousStep != null)
            {
                //TODO: Refactor - all queries should be aggregated into one query
                if (!Authorize(m.PreviousStepID, ActionPermission.Read, typeof(ProjectPlanTaskResponse)))
                    return false;
                var taskID = m.PreviousStep.ActualTaskID ?? (m.PreviousStep.ProjectPlanTaskID.HasValue ? m.PreviousStep.ProjectPlanTask.TaskID : null) ?? (m.PreviousTask != null ? m.PreviousTask.TaskID : default(Guid?));
                if (taskID.HasValue)
                    m.PreviousTask = d.Tasks.Where(f => f.TaskID == taskID && f.Version == 0 && f.VersionDeletedBy == null).SingleOrDefault();
                if (m.PreviousStep.VersionWorkflowInstanceID.HasValue)
                    m.PreviousWorkflowInstance = d.WorkflowInstances.Where(f => f.WorkflowInstanceID == m.PreviousStep.VersionWorkflowInstanceID).Single();
                if (m.IncludeContent ?? false)
                    m.content = d.GraphData.Where(f => f.GraphDataID == m.PreviousStep.ActualGraphDataID).Select(f => f.GraphContent).FirstOrDefault();
                m.LastEditedBy = (from o in d.Contacts where o.ContactID == m.PreviousStep.VersionUpdatedBy select o.Username).FirstOrDefault();
                return true;
            }
            else return false;
        }


        public AutomationViewModel GetStep(NKDC d, Guid sid, Guid? tid = null, bool includeContent = false)
        {
            AutomationViewModel m = new AutomationViewModel { IncludeContent = includeContent };
            m.PreviousStep = d.ProjectPlanTaskResponses.Where(f => f.ProjectPlanTaskResponseID == sid && f.Version == 0 && f.VersionDeletedBy == null).SingleOrDefault();
            if (m.PreviousStep != null)
            {
                //TODO: Refactor - all queries should be aggregated into one query
                if (!Authorize(sid, ActionPermission.Read, typeof(ProjectPlanTaskResponse)))
                    return null;
                var taskID = m.PreviousStep.ActualTaskID ?? (m.PreviousStep.ProjectPlanTaskID.HasValue ? m.PreviousStep.ProjectPlanTask.TaskID : null) ?? tid;
                if (taskID.HasValue)
                    m.PreviousTask = d.Tasks.Where(f => f.TaskID == taskID && f.Version == 0 && f.VersionDeletedBy == null).SingleOrDefault();
                if (m.PreviousStep.VersionWorkflowInstanceID.HasValue)
                    m.PreviousWorkflowInstance = d.WorkflowInstances.Where(f => f.WorkflowInstanceID == m.PreviousStep.VersionWorkflowInstanceID).Single();
                if (includeContent)
                    m.content = d.GraphData.Where(f => f.GraphDataID == m.PreviousStep.ActualGraphDataID).Select(f => f.GraphContent).FirstOrDefault();
                m.LastEditedBy = (from o in d.Contacts where o.ContactID == m.PreviousStep.VersionUpdatedBy select o.Username).FirstOrDefault();

                return m;
            }
            else return null;
        }


        public bool Equate(string js)
        {
            return new Engine()
                .Execute(js)
                .GetCompletionValue() // get the latest statement completion value
                .AsBoolean();
        }
        public bool EquateAsync(string js)
        {
            int cancelms = 1000;
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(cancelms);
            try
            {
                System.Threading.Tasks.Task<bool> task = System.Threading.Tasks.Task.Run<bool>(() =>
                {
                    try
                    {
                        Thread t = Thread.CurrentThread;
                        using (cts.Token.Register(t.Abort))
                        {
                            return Equate(js);
                        }

                    }
                    // *** If cancellation is requested, an OperationCanceledException results. 
                    catch (OperationCanceledException)
                    {
                        return false;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }, cts.Token);
                task.Wait(cancelms + 20);
                if (!task.IsCompleted)
                {

                    cts.Cancel();
                    return false;
                }
                return task.Result;
            }
            catch
            {
                return false;
            }
            

        }



        public bool DoNext(AutomationViewModel m)
        {
            var now = DateTime.UtcNow;
            var contact = ContactID;
            var company = CompanyID;
            var application = ApplicationID;
            m.Status = "";
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);                

                //We always require a stepID to start...otherwise...
                if (!m.PreviousStepID.HasValue || m.PreviousStepID == Guid.Empty)
                {
                    if (m.ReferenceID.HasValue)
                        m.PreviousStep.ProjectPlanTaskResponseID = m.ReferenceID.Value; //Get stepid from the url
                    else
                        return true; //The step is in the closed state
                }
                    
                
                //If no projectID, instantiate workflow - need graphdatagroupid
                if (!m.ProjectID.HasValue && !d.ProjectPlanTaskResponses.Any(f => f.ProjectPlanTaskResponseID == m.PreviousStepID)) //&& f.VersionDeletedBy == null && f.Version == 0 removed this to enable continuation of flows after edits
                {
                    //We need workflowid
                    if (!m.GraphDataGroupID.HasValue)
                    {
                        if (!m.TaskID.HasValue)
                        {
                            m.Error = "Couldn't determine task workflow.";
                            return false;
                        }
                        if (!m.PreviousTask.GraphDataGroupID.HasValue)
                            m.PreviousTask = d.Tasks.Where(f => f.TaskID == m.TaskID.Value && f.VersionDeletedBy == null && f.Version == 0).Single();
                        if (!m.GraphDataGroupID.HasValue)
                        {
                            m.Error = "Couldn't determine workflow.";
                            return false;
                        }
                    }

                    //Now let's check for start node
                    if (!m.GraphDataID.HasValue || d.GraphDataRelation.Any(f => f.ToGraphDataID == m.GraphDataID && f.GraphDataGroupID == m.GraphDataGroupID && f.VersionDeletedBy == null && f.Version == 0))
                    {
                        if (m.IncludeContent ?? false)
                        {
                            var gd = (
                                    from o in d.GraphDataRelation.Where(f => f.GraphDataGroupID == m.GraphDataGroupID && f.VersionDeletedBy == null && f.Version == 0)
                                    join q in d.GraphDataRelation.Where(f => f.GraphDataGroupID == m.GraphDataGroupID && f.VersionDeletedBy == null && f.Version == 0)
                                        on o.FromGraphDataID equals q.ToGraphDataID into oq
                                    from soq in oq.DefaultIfEmpty()
                                    where soq == null
                                    join p in d.GraphData on o.FromGraphDataID equals p.GraphDataID
                                    select p
                                  ).FirstOrDefault();
                            if (gd == null)
                            {
                                gd = (from o in d.GraphDataGroups.Where(f => f.GraphDataGroupID == m.GraphDataGroupID && f.VersionDeletedBy == null && f.Version == 0)
                                      join r in d.GraphDataRelation on o.GraphDataGroupID equals r.GraphDataGroupID
                                      join g in d.GraphData on r.FromGraphDataID equals g.GraphDataID
                                      where o.StartGraphDataID == g.GraphDataID && r.FromGraphDataID != r.ToGraphDataID
                                      select g
                                          ).FirstOrDefault();
                            }
                            if (gd != null)
                            {
                                m.PreviousStep.ActualGraphDataID = gd.GraphDataID;
                                m.content = gd.GraphContent;
                            }
                        }
                        else
                        {
                            m.PreviousStep.ActualGraphDataID = (
                                                            from o in d.GraphDataRelation.Where(f => f.GraphDataGroupID == m.GraphDataGroupID && f.VersionDeletedBy == null && f.Version == 0)
                                                            join q in d.GraphDataRelation.Where(f => f.GraphDataGroupID == m.GraphDataGroupID && f.VersionDeletedBy == null && f.Version == 0)
                                                                on o.FromGraphDataID equals q.ToGraphDataID into oq
                                                            from soq in oq.DefaultIfEmpty()
                                                            where soq == null
                                                            select o.FromGraphDataID
                                                          ).FirstOrDefault();
                            if (m.PreviousStep.ActualGraphDataID == null)
                            {
                                m.PreviousStep.ActualGraphDataID = (from o in d.GraphDataGroups.Where(f => f.GraphDataGroupID == m.GraphDataGroupID && f.VersionDeletedBy == null && f.Version == 0)
                                      join r in d.GraphDataRelation on o.GraphDataGroupID equals r.GraphDataGroupID
                                      join g in d.GraphData on r.FromGraphDataID equals g.GraphDataID
                                      where o.StartGraphDataID == g.GraphDataID && r.FromGraphDataID != r.ToGraphDataID
                                      select g.GraphDataID
                                         ).FirstOrDefault();
                            }
                        }
                        if (m.PreviousStep.ActualGraphDataID == null)
                        {
                            m.Error = "Couldn't identify distinct start in workflow.";
                            return false;
                        }
                    }

                    if (Authorize(m, m.GraphDataGroupID, ActionPermission.Read, typeof(GraphDataGroup))
                        && Authorize(m, null, ActionPermission.Create, typeof(ProjectPlanTaskResponse)))
                    {
                        m.PreviousStep.GraphDataGroup = d.GraphDataGroups.Where(f => f.GraphDataGroupID == m.GraphDataGroupID && f.VersionDeletedBy == null && f.Version == 0).Single();
                        m.PreviousStep.ProjectID = Guid.NewGuid();
                        var project = new Project
                        {
                            ProjectID = m.PreviousStep.ProjectID.Value,
                            ProjectName = string.Join("", string.Format("{0} @ {1}", string.Join("", string.Format("{0}", m.PreviousStep.GraphDataGroup.GraphDataGroupName).Take(28)), now.ToString("yyyy-MM-dd HH:mm:ss")).Take(50)),
                            ProjectCode = string.Format("{0}-{1}", now.HexUnixTimestamp(), m.PreviousStep.ProjectID.Value.ToString().Substring(0, 4).ToUpperInvariant()),
                            ProjectTypeID = ConstantsHelper.PROJECT_TYPE_FLOWPRO,
                            VersionUpdatedBy = contact,
                            VersionUpdated = now,
                            VersionOwnerContactID = contact,
                            VersionOwnerCompanyID = company,
                            VersionAntecedentID = m.PreviousStepID
                        };
                        d.Projects.AddObject(project);
                        var wfid = Guid.NewGuid();
                        var wf = new WorkflowInstance
                        {
                            WorkflowInstanceID = wfid,
                            WorkflowID = ConstantsHelper.WORKFLOW_APP_FLOWPRO ,
                            RunStateTypeID =  null,
                            TableType = ConstantsHelper.REFERENCE_TYPE_PROJECTPLANTASKRESPONSE ,
                            ReferenceID = m.id ,
                            ExecutionStatus = "RUNNING" ,
                            ExecutionTimeoutSeconds = null ,
                            Began = now ,
                            CanResume = true ,
                            Resumed = null ,
                            ResumeTriggers = true ,
                            ResumeAttempts = ConstantsHelper.WORKFLOW_INSTANCE_RESUME_ATTEMPTS_LEFT,
                            Pending = null ,
                            Idle = now.AddSeconds(ConstantsHelper.WORKFLOW_INSTANCE_TIMEOUT_IDLE_SECONDS) ,
                            IdleTimeoutSeconds = ConstantsHelper.WORKFLOW_INSTANCE_TIMEOUT_IDLE_SECONDS ,
                            CanCancel =  true,
                            Cancelled =  null,
                            Completed =  null,
                            VersionAntecedentID =  wfid,
                            VersionUpdatedBy =  contact,
                            VersionOwnerContactID =  contact,
                            VersionOwnerCompanyID =  company,
                            VersionUpdated =  now
                        };
                        d.WorkflowInstances.AddObject(wf);
                        
                        m.PreviousStep.VersionWorkflowInstanceID = wfid;
                        m.PreviousStep.ResponsibleCompanyID = m.PreviousTask.WorkCompanyID ?? company;
                        m.PreviousStep.ResponsibleContactID = m.PreviousTask.WorkContactID ?? contact;
                        m.PreviousStep.VersionUpdatedBy = contact;
                        m.PreviousStep.VersionUpdated = now;
                        m.PreviousStep.VersionOwnerContactID = contact;
                        m.PreviousStep.VersionOwnerCompanyID = company;
                        m.PreviousStep.VersionAntecedentID = m.PreviousStepID;

                        m.LastEditedBy = d.Contacts.Where(f => f.ContactID == contact).Select(f => f.Username).FirstOrDefault();

                        if (m.TaskID.HasValue)
                            m.PreviousStep.ActualTaskID = m.TaskID;
                        m.PreviousStep.Began = now;
                        d.ProjectPlanTaskResponses.AddObject(m.PreviousStep);

                        d.SaveChanges();
                        //TODO: Should checkout
                        return true;
                    }
                    else
                    {
                        m.Error = "Unauthorized access";
                        return false;
                    }
                }
                //OK its an existing WF
                else {
                    if (!Authorize(m, m.id, ActionPermission.Update, typeof(ProjectPlanTaskResponse)))
                    {
                        m.Error = "No permission to update task.";
                        return false;
                    }
                    //Quench Model
                    if (!QuenchStep(d, m))
                    {
                        m.Error = "Could not quench existing step. Permission error or does not exist.";
                        return false;
                    }
                    //Prob should check whether its checked out
                    if (m.PreviousWorkflowInstance != null)
                    {
                        if (m.PreviousWorkflowInstance.Completed.HasValue)
                        {
                            m.Error = "Workflow Completed";
                            return false;
                        }
                        if (m.PreviousWorkflowInstance.Idle.HasValue && m.PreviousWorkflowInstance.Idle > now && m.PreviousWorkflowInstance.VersionOwnerContactID != contact)
                        {
                            m.Error = "Workflow Busy";
                            return false;
                        }
                        if (m.PreviousWorkflowInstance.Cancelled.HasValue)
                        {
                            m.Error = "Workflow Cancelled";
                            return false;
                        }
                    }

                    bool isCompleted = false, isDefault = false;
                    var nextStep = Guid.Empty;
                    var options = d.GraphDataRelation.Where(f=>
                        f.FromGraphDataID == m.ActualGraphDataID 
                        && f.VersionDeletedBy == null 
                        && f.ToGraphDataID != m.ActualGraphDataID
                        && f.ToGraphDataID != null 
                        && f.Version == 0)
                        .OrderByDescending(f=>f.Weight)
                        .OrderByDescending(f=>f.VersionUpdated);
                    if (options.Count() == 0)
                    {
                        m.Status += " Workflow Completed.";
                        isCompleted = true;
                    }
                    //if step (response) & graphdataid go through graphdatarelationconditions for next transition   
                    var data = (from o in d.ProjectDatas.Where(f => f.ProjectID == m.ProjectID && f.VersionDeletedBy == null && f.Version == 0)
                                join t in d.ProjectDataTemplates on o.ProjectDataTemplateID equals t.ProjectDataTemplateID
                                select new { t.CommonName, o.Value, t.SystemDataType, o.VersionUpdated }
                                    ).GroupBy(f => f.CommonName, f => f, (key, g) => g.OrderByDescending(f=>f.VersionUpdated).FirstOrDefault());
                    var dict = data.ToDictionary(f => "{{" + f.CommonName + "}}", f => (f.Value ?? "").Replace("\'","\\\'").Replace("\"", "\\\""));
                    foreach (var option in options)
                    {
                        if (option.Condition == null || !option.Condition.Any())
                        {
                            //Dont take the deafult yet
                            continue;
                        }
                        var correct = false;
                        foreach (var condition in option.Condition)
                        {
                            var toCheck = condition.Condition.Condition;
                            if (string.IsNullOrWhiteSpace(toCheck))
                                continue;
                            foreach (var lookup in dict)
                                toCheck = toCheck.Replace(lookup.Key, "\"" + lookup.Value + "\"");
                            if (ConstantsHelper.REGEX_JS_CLEANER.IsMatch(toCheck))
                            {
                                m.Error = "Illegal string in your conditions.";
                                return false;
                            }
                            if (!EquateAsync(toCheck))
                            {
                                correct = false;
                                m.Status += " Failed:" + toCheck;
                                if (condition.JoinedBy == "&&")
                                    break;
                            }
                            else
                            {
                                correct = true;
                                m.Status += " Succeeded:" + toCheck;
                                if (condition.JoinedBy == "||")
                                    break;
                            }
                        }
                        if (correct)
                        {
                            nextStep = option.ToGraphDataID.Value;
                            break;
                        }
                    }
                    if (nextStep == Guid.Empty)
                    {
                        //Now do the default transition
                        var option = options.FirstOrDefault(f => !f.Condition.Any());
                        if (option != null)
                        {
                            nextStep = option.ToGraphDataID.Value;
                            isDefault = true;
                            m.Status += " Transition Default.";
                        }

                    }
                    if (nextStep != Guid.Empty || isCompleted)
                    {
                        if (nextStep == Guid.Empty)
                        {
                            m.PreviousStep.ActualGraphDataID = null;
                            m.PreviousStep.Completed = now;
                            m.PreviousWorkflowInstance.Completed = now;
                        }
                        else
                        {
                            m.PreviousStep.ActualGraphDataID = nextStep;
                            m.PreviousWorkflowInstance.Idle = now.AddSeconds(m.PreviousWorkflowInstance.IdleTimeoutSeconds ?? ConstantsHelper.WORKFLOW_INSTANCE_TIMEOUT_IDLE_SECONDS);
                        }

                        d.SaveChanges();
                        return true;
                    }    
                }
                m.Error = "Could not find a valid transition. ";
                m.Error += m.Status;
                return false;
            }
        }

        public bool SendEmail()
        {
            return false;
        }

        public bool SendSMS()
        {
            return false;
        }

        public bool InstantiateWorkflow()
        {
            return false;
        }

        public bool TransitionWorkflow()
        {
            return false;
        }

        public bool CancelWorkflow()
        {
            return false;
        }


    }
}
