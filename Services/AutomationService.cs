using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using JetBrains.Annotations;
using Orchard;
using System.Security.Principal;
using EXPEDIT.Flow.ViewModels;
using EXPEDIT.Flow.Models;
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
using Orchard.Environment.Configuration;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Runtime.Serialization.Formatters;

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
        private readonly ShellSettings _shellSettings;
        private bool ProxyAuthenticated { get; set; }


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
            IWorkflowService wf,
             ShellSettings shellSettings
            )
        {
            _users = users;
            _services = orchardServices;
            _wf = wf;
            _shellSettings = shellSettings;
        }

        private void ContactTick(Guid? contactID, int add = 1)
        {
            var key = string.Format("API-USER-TICKER-{0}", contactID);
            var count = CacheHelper.AddToCache<int>(() =>
            {
                object val = CacheHelper.Cache[key];
                if (val != null)
                    return ((int)val) + add;
                else
                    return 1;
            }, key, new TimeSpan(3, 0, 0));
            if (count > 100)
            {
                //Disable it
                _users.DisableContact(contactID.Value);

            }
        }


        private void CompanyTick(Guid? companyID, int add = 1)
        {
            var key = string.Format("API-COMPANY-TICKER-{0}", companyID);
            var count = CacheHelper.AddToCache<int>(() =>
            {
                object val = CacheHelper.Cache[key];
                if (val != null)
                    return ((int)val) + add;
                else
                    return 1;
            }, key, new TimeSpan(3, 0, 0));           
        }

        private bool CompanyDisabled(Guid? companyID) {
            var key = string.Format("API-COMPANY-TICKER-{0}", companyID);
            object ticks = CacheHelper.Cache[key];
            if (ticks == null)
                return false;
            int t = (int)ticks;
            if (companyID.HasValue && t > 100)
                return true;
            if (!companyID.HasValue && t > 100)
                return true;
            return false;
        }

        public bool Authenticate(AutomationViewModel m, string method)
        {

            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                
                var runningApplicationID = (from o in d.Applications where o.ApplicationName == _shellSettings.Name select o.ApplicationId).FirstOrDefault();
                Guid applicationID;
                if (!Guid.TryParse(m.Application, out applicationID))
                    applicationID = runningApplicationID;
                else if (applicationID != runningApplicationID)
                    return false;

                byte[] bytes = Encoding.UTF8.GetBytes(m.Password);
                SHA256Managed hashstring = new SHA256Managed();
                byte[] hash = hashstring.ComputeHash(bytes);
                string hashString = string.Empty;
                foreach (byte x in hash)
                {
                    hashString += String.Format("{0:x2}", x);
                }

                var trigger = d.Triggers.FirstOrDefault(f => f.JsonProxyApplicationID == applicationID && f.JsonUsername == m.Username 
                    && ((f.JsonPassword == hashString && f.JsonPasswordType=="SHA256") || (f.JsonPassword == m.Password && f.JsonPasswordType == "TEXT"))
                    && f.CommonName == null //API method
                    && f.VersionDeletedBy == null && f.Version == 0);
                if (trigger == null)
                    return false;
                ApplicationID = applicationID;
                ContactID = trigger.JsonProxyContactID;
                CompanyID = trigger.JsonProxyCompanyID;
                ProxyAuthenticated = true;
            }
            if (!ContactID.HasValue)
                return false;
            if (_users.IsContactDisabled(ContactID.Value))
                return false;
            return true;
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
                    m.PreviousWorkflowInstance = d.WorkflowInstances.Where(f => f.WorkflowInstanceID == m.PreviousStep.VersionWorkflowInstanceID && f.Version == 0 && f.VersionDeletedBy == null).Single();
                if (m.IncludeContent ?? false)
                {
                    var g = d.GraphData.Where(f => f.GraphDataID == m.PreviousStep.ActualGraphDataID && f.Version == 0 && f.VersionDeletedBy == null).FirstOrDefault();
                    m.content = g.GraphContent;
                    m.GraphName = g.GraphName;
                }
                
                m.LastEditedBy = (from o in d.Contacts.Where(f=> f.Version == 0 && f.VersionDeletedBy == null) where o.ContactID == m.PreviousStep.VersionUpdatedBy select o.Username).FirstOrDefault();
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
                if (!m.PreviousStep.ActualGraphDataID.HasValue)
                    return m; 
                var taskID = m.PreviousStep.ActualTaskID ?? (m.PreviousStep.ProjectPlanTaskID.HasValue ? m.PreviousStep.ProjectPlanTask.TaskID : null) ?? tid;
                if (taskID.HasValue)
                    m.PreviousTask = d.Tasks.Where(f => f.TaskID == taskID && f.Version == 0 && f.VersionDeletedBy == null).SingleOrDefault();
                if (m.PreviousStep.VersionWorkflowInstanceID.HasValue)
                    m.PreviousWorkflowInstance = d.WorkflowInstances.Where(f => f.WorkflowInstanceID == m.PreviousStep.VersionWorkflowInstanceID).Single();
                if (includeContent)
                {
                    var g = d.GraphData.Where(f => f.Version == 0 && f.VersionDeletedBy == null && f.GraphDataID == m.PreviousStep.ActualGraphDataID).FirstOrDefault();
                    m.content = g.GraphContent;
                    m.GraphName = g.GraphName;
                }
                m.LastEditedBy = (from o in d.Contacts.Where(f => f.Version == 0 && f.VersionDeletedBy == null) where o.ContactID == m.PreviousStep.VersionUpdatedBy select o.Username).FirstOrDefault();

                return m;
            }
            else return null;
        }


        public bool Equate(string js)
        {
            if (string.IsNullOrWhiteSpace(js))
                return true;
            return new Engine()
                .Execute(js)
                .GetCompletionValue() // get the latest statement completion value
                .AsBoolean();
        }
        public bool EquateAsync(string js)
        {
            if (string.IsNullOrWhiteSpace(js))
                return true; 
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

	public bool DoAs(AutomationViewModel m) {
        if (string.IsNullOrWhiteSpace(m.NewUserEmail))
            return false;
        var newUser = string.Format("Guest{0}{1}", DateTime.UtcNow.HexUnixTimestamp(), Guid.NewGuid().ToString().Substring(0, 4).ToUpperInvariant());
        if (_users.VerifyUserUnicity(newUser, m.NewUserEmail))
        {
            if (_users.Create(m.NewUserEmail, newUser, Guid.NewGuid().ToString()) != null)
            {
                //Now force reauthentication
                //Recorded in parent company for now
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    ContactID = (from o in d.Contacts where o.Username == newUser && o.Version == 0 && o.VersionDeletedBy == null select o.ContactID).Single();
                }
                return DoNext(m);
            }
            else
                return false;
        }
        else return false;
	}



        public bool DoNext(AutomationViewModel m)
        {
            var now = DateTime.UtcNow;
            var contact = ContactID;
            var company = CompanyID;
            var application = ApplicationID;
            ContactTick(contact);
            m.Status = "";
            bool isNew = false;
            bool isTransitioned = false;
            bool isCompleted = false;
            bool checkAuthorization = false;
            Guid nextGid = default(Guid);
            Guid? oldGid = default(Guid?);
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);                

                //We always require a stepID to start...otherwise...
                if (!m.PreviousStepID.HasValue || m.PreviousStepID == Guid.Empty)
                {
                    if (m.ReferenceID.HasValue)
                        m.PreviousStep.ProjectPlanTaskResponseID = m.ReferenceID.Value; //Get stepid from the url
                    else
                        m.PreviousStep.ProjectPlanTaskResponseID = Guid.NewGuid(); //Lets create one
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
                                      join r in d.GraphDataRelation.Where(f => f.Version == 0 && f.VersionDeletedBy == null) on o.GraphDataGroupID equals r.GraphDataGroupID
                                      join g in d.GraphData.Where(f => f.Version == 0 && f.VersionDeletedBy == null) on r.FromGraphDataID equals g.GraphDataID
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
                                      join r in d.GraphDataRelation.Where(f=> f.Version == 0 && f.VersionDeletedBy == null) on o.GraphDataGroupID equals r.GraphDataGroupID
                                      join g in d.GraphData.Where(f=> f.Version == 0 && f.VersionDeletedBy == null) on r.FromGraphDataID equals g.GraphDataID
                                      where o.StartGraphDataID == g.GraphDataID && r.FromGraphDataID != r.ToGraphDataID
                                      select g.GraphDataID
                                         ).FirstOrDefault();
                            }
                        }
                        if (m.PreviousStep.ActualGraphDataID == null || m.PreviousStep.ActualGraphDataID == Guid.Empty)
                        {
                            m.Error = "Couldn't identify distinct start in workflow.";
                            return false;
                        }
                        nextGid = m.PreviousStep.ActualGraphDataID.Value;
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
                            ProjectCode = string.Format("{0}-{1}", now.HexUnixTimestamp(), m.PreviousStepID.Value.ToString().Substring(0, 4).ToUpperInvariant()),
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

                        if (!m.TaskID.HasValue)
                            m.PreviousTask = d.Tasks.Where(f => f.Version == 0 && f.VersionDeletedBy == null && f.GraphDataGroupID == m.GraphDataGroupID && f.GraphDataID == m.GraphDataID).FirstOrDefault();

                        m.PreviousStep.ResponsibleCompanyID = m.PreviousTask.WorkCompanyID ?? company;
                        m.PreviousStep.ResponsibleContactID = m.PreviousTask.WorkContactID ?? contact;
                        m.PreviousStep.VersionUpdatedBy = contact;
                        m.PreviousStep.VersionUpdated = now;
                        m.PreviousStep.VersionOwnerContactID = contact;
                        m.PreviousStep.VersionOwnerCompanyID = company;
                        m.PreviousStep.VersionAntecedentID = m.PreviousStepID;

                        m.LastEditedBy = d.Contacts.Where(f => f.ContactID == contact).Select(f => f.Username).FirstOrDefault();
                                  
                            
                        if (m.PreviousTask != null)
                        {
                            m.PreviousStep.ActualTaskID = m.TaskID;
                            if (m.PreviousTask.WorkCompanyID.HasValue || m.PreviousTask.WorkContactID.HasValue)
                            {
                                m.PreviousStep.VersionOwnerContactID = m.PreviousTask.WorkContactID;
                                m.PreviousStep.VersionOwnerCompanyID = m.PreviousTask.WorkCompanyID;
                                checkAuthorization = true;
                            }
                        }
                        
                        m.PreviousStep.Began = now;
                        d.ProjectPlanTaskResponses.AddObject(m.PreviousStep);

                        Dictionary<string, string> pdd = new Dictionary<string, string>();
                        if (ProxyAuthenticated)
                            pdd = m.Variables;
                        else 
                            pdd = m.QueryParamsVariables;

                        //OK now save context variables passed in
                        foreach (var v in m.Variables)
                        {
                            var pdtid = Guid.NewGuid();
                            var lbl = string.Format("{0}",v.Key).Replace("\"", "\\\"");
                            var pdt = new ProjectDataTemplate
                            {
                                ProjectDataTemplateID = pdtid,
                                TemplateStructure = string.Format("{{\"label\":\"{0}\",\"field_type\":\"text\",\"required\":false,\"field_options\":{{\"size\":\"small\"}},\"cid\":\"{1}\",\"uid\":\"{2}\"}}", lbl , pdtid, pdtid),
                                CommonName = v.Key,
                                VersionUpdated = now,
                                VersionUpdatedBy = contact
                            };
                            d.ProjectDataTemplates.AddObject(pdt);
                            var pd = new ProjectData
                            {
                                ProjectDataID = Guid.NewGuid(),
                                ProjectID = m.ProjectID,
                                ProjectDataTemplateID = pdtid,
                                ProjectPlanTaskResponseID = m.PreviousStepID,
                                Value = v.Value,
                                VersionUpdated = now,
                                VersionUpdatedBy = contact
                            };
                            d.ProjectDatas.AddObject(pd);
                        }                      

                       

                        isNew = true;
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

                    bool isDefault = false;
                    oldGid = m.ActualGraphDataID;
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
                                join t in d.ProjectDataTemplates.Where(f => f.Version == 0 && f.VersionDeletedBy == null) on o.ProjectDataTemplateID equals t.ProjectDataTemplateID
                                select new { t.CommonName, o.Value, t.SystemDataType, o.VersionUpdated }
                                    ).GroupBy(f => f.CommonName, f => f, (key, g) => g.OrderByDescending(f=>f.VersionUpdated).FirstOrDefault());
                    var dict = data.ToDictionary(f => "{{" + f.CommonName + "}}", f => (f.Value ?? "").Replace("\'","\\\'").Replace("\"", "\\\""));
                    Guid? firstDefault = null;
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
                            {
                                firstDefault = option.ToGraphDataID.Value;
                                continue;
                            }
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
                            nextGid = option.ToGraphDataID.Value;
                            break;
                        }
                    }
                    if (nextGid == Guid.Empty)
                    {
                        if (firstDefault.HasValue && firstDefault.Value != Guid.Empty)
                        {
                            nextGid = firstDefault.Value;
                            isDefault = true;
                            m.Status += " Transition Default (C).";
                        }
                        else
                        {
                            //Now do the default transition
                            var option = options.FirstOrDefault(f => !f.Condition.Any());
                            if (option != null)
                            {
                                nextGid = option.ToGraphDataID.Value;
                                isDefault = true;
                                m.Status += " Transition Default.";
                            }
                        }

                    }

                    if (m.PreviousWorkflowInstance != null && m.PreviousWorkflowInstance.Idle.HasValue)
                        m.PreviousStep.Hours = (m.PreviousStep.Hours ?? 0) + ((decimal)((now - m.PreviousWorkflowInstance.Idle.Value.AddSeconds(-ConstantsHelper.WORKFLOW_INSTANCE_TIMEOUT_IDLE_SECONDS)).TotalHours));

                    if (nextGid != Guid.Empty || isCompleted)
                    {                       
                        if (isCompleted)
                        {
                            m.PreviousStep.ActualGraphDataID = null;
                            m.PreviousStep.Completed = now;
                            m.PreviousWorkflowInstance.Completed = now;
                        }
                        else
                        {
                            m.PreviousStep.ActualGraphDataID = nextGid;
                            m.PreviousWorkflowInstance.Idle = now.AddSeconds(m.PreviousWorkflowInstance.IdleTimeoutSeconds ?? ConstantsHelper.WORKFLOW_INSTANCE_TIMEOUT_IDLE_SECONDS);
                            m.PreviousTask = d.Tasks.Where(f => f.Version == 0 && f.VersionDeletedBy == null && f.GraphDataGroupID == m.GraphDataGroupID && f.GraphDataID == nextGid).FirstOrDefault();
                            if (m.PreviousTask != null)
                            {
                                if (m.PreviousTask.WorkCompanyID.HasValue || m.PreviousTask.WorkContactID.HasValue)
                                {
                                    m.PreviousStep.VersionOwnerContactID = m.PreviousTask.WorkContactID;
                                    m.PreviousStep.VersionOwnerCompanyID = m.PreviousTask.WorkCompanyID;
                                    checkAuthorization = true;
                                }

                            }
                            m.PreviousStep.ActualTaskID = m.TaskID;
                        }

                        m.PreviousStep.VersionUpdatedBy = contact;
                        m.PreviousStep.VersionUpdated = now;      

                        isTransitioned = true;
                    }    
                }
                
                if (isTransitioned || isNew)
                {
                    EnqueueEvents(d, m, oldGid, isCompleted ? default(Guid?) : nextGid, now);
                }

                d.SaveChanges();

                if (!isCompleted)
                {
                    if (checkAuthorization)
                    {
                        if (!Authorize(m.PreviousStepID, ActionPermission.Read, typeof(ProjectPlanTaskResponse)))
                        {
                            m.PreviousStep = null;
                            m.PreviousTask = null;
                            m.PreviousWorkflowInstance = null;
                            m.Error = "Transitioned to a different department.";
                            return true;
                        }
                    }
                }

                if (isTransitioned || isNew)
                    return true;

                m.Error = "Could not find a valid transition. ";
                m.Error += m.Status;
                return false;
            }
        }


        public bool ExecuteMethod(AutomationViewModel m, string method)
        {
            return false;

        }

        private void EnqueueEvents(NKDC d, AutomationViewModel m, Guid? oldgid, Guid? newgid, DateTime? now = default(DateTime?))
        {
            if (!now.HasValue)
                now = DateTime.UtcNow;
            var isExit = false;
            var isEntry = false;
            var isUpdate = false;
            if (oldgid.HasValue && newgid == Guid.Empty)
                isUpdate = true;
            if (oldgid.HasValue && newgid == null)
                isExit = true;
            if (newgid.HasValue && newgid != Guid.Empty)
                isEntry = true;
            if (oldgid.HasValue && oldgid != Guid.Empty)
                isExit = true;
            isUpdate = isUpdate || isExit || isEntry;
            var triggers = (from t in d.Triggers.Where(f => f.Version == 0 && f.VersionDeletedBy == null)
                            join g in d.TriggerGraphs.Where(f => f.Version == 0 && f.VersionDeletedBy == null)
                            on t.TriggerID equals g.TriggerID
                            where
                            (newgid.HasValue && isEntry && g.GraphDataID == newgid && g.GraphDataGroupID==m.ActualGraphDataGroupID && g.OnEnter == true) //entry
                            || (isUpdate && g.GraphDataID == oldgid && g.GraphDataGroupID == m.ActualGraphDataGroupID && g.OnDataUpdate == true) //update
                            || (oldgid.HasValue && isExit && g.GraphDataID == oldgid && g.GraphDataGroupID==m.ActualGraphDataGroupID && g.OnExit == true) //exit
                            select new { t, g });
            foreach(var trigger in triggers) {
                var runNext = now.Value;
                if (trigger.g.GraphDataGroup.VersionOwnerCompanyID == null && trigger.g.GraphDataGroup.VersionOwnerContactID == null)
                    continue; //Only allow paid users to trigger
                if (trigger.t.DelayUntil.HasValue)
                {
                    runNext = trigger.t.DelayUntil.Value;
                    if (runNext < now)
                        continue;
                }
                else
                {
                    var ts = new TimeSpan(0);
                    if (trigger.t.DelayDays.HasValue)
                        ts = ts.Add(new TimeSpan(trigger.t.DelayDays.Value, 0, 0, 0));
                    if (trigger.t.DelaySeconds.HasValue)
                        ts = ts.Add(new TimeSpan(0, 0, trigger.t.DelaySeconds.Value));
                    if (trigger.t.DelayWeeks.HasValue)
                        ts = ts.Add(new TimeSpan(7 * trigger.t.DelayWeeks.Value, 0, 0, 0));
                    if (ts.Ticks < 0 || (trigger.t.DelayYears.HasValue && trigger.t.DelayYears < 0) || (trigger.t.DelayMonths.HasValue && trigger.t.DelayMonths < 0))
                        continue;
                    runNext = runNext.Add(ts);
                    if (trigger.t.DelayMonths.HasValue)
                        runNext = runNext.AddMonths(trigger.t.DelayMonths.Value);
                    if (trigger.t.DelayYears.HasValue)
                        runNext = runNext.AddYears(trigger.t.DelayYears.Value);

                }
                var evt = new ProjectPlanTaskResponseEvent
                {
                    ProjectPlanTaskResponseEventID = Guid.NewGuid(),
                    ProjectPlanTaskResponseID = m.id,
                    ProjectID = m.ProjectID,
                    TaskID = m.TaskID,
                    TriggerGraphID = trigger.g.TriggerGraphID,
                    OriginTriggerID = trigger.t.TriggerID,
                    //DestinationTriggerID = m.DestinationTriggerID,
                    //JsonCustomVars = m.JsonCustomVars,
                    RunNext = runNext,
                    RunsLeft = (!trigger.t.Repeats.HasValue || trigger.t.Repeats == 0) ? 1 : trigger.t.Repeats.Value ,
                    Executed = null,
                    Failed = null,
                    Reason = null,
                    Version = 0,
                    VersionUpdatedBy = ContactID,
                    VersionOwnerContactID = trigger.g.GraphDataGroup.VersionOwnerContactID,
                    VersionOwnerCompanyID = trigger.g.GraphDataGroup.VersionOwnerCompanyID,
                    VersionUpdated = now.Value,
                };
                d.ProjectPlanTaskResponseEvents.AddObject(evt);
            }

        }

        public bool ProcessEvents()
        {
            //Disable account if theyve done too many requests
            var contactTicks = new Dictionary<Guid?, int>();
            var companyTicks = new Dictionary<Guid?, int>();
            var disabledCompanies = new List<Guid?>();
            var now = DateTime.UtcNow;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                var disabledContacts = d.Contacts.Where(f => f.Version == 0  && (( f.VersionCertainty.HasValue && f.VersionCertainty < 0) || f.VersionDeletedBy != null)).Select(f=>f.ContactID).ToArray();
                var events = (from o in d.ProjectPlanTaskResponseEvents.Where(f => f.Version == 0 && f.VersionDeletedBy == null)
                              where o.RunsLeft > 0 && o.RunNext < now && (!o.VersionOwnerContactID.HasValue || !disabledContacts.Contains(o.VersionOwnerContactID.Value))
                              select o);
                foreach (var evt in events)
                {
                    if (disabledCompanies.Any(f => f == evt.VersionOwnerCompanyID))
                        continue;
                    if (CompanyDisabled(evt.VersionOwnerCompanyID))
                    {
                        disabledCompanies.Add(evt.VersionOwnerCompanyID);
                        continue;
                    }
                    if (disabledContacts.Any(f => f == evt.VersionOwnerContactID))
                        continue; //rate limited contact

                    var trigger = d.TriggerGraphs.Where(f=>f.Version == 0 && f.VersionDeletedBy == null && f.TriggerGraphID == evt.TriggerGraphID).SingleOrDefault();
                    if (trigger == null || trigger.Trigger == null || (string.Format("{0}",trigger.TriggerGraphID) != trigger.Trigger.CommonName)) //we use common name for additional security
                    {
                        evt.Reason = "NO TRIGGER FOUND"; //16 max
                        evt.Failed = now;
                        evt.RunsLeft = 0; //critical failure
                        evt.Executed = now;
                        evt.RunNext = null;
                    }
                    else
                    {
                        //OK Let's check it... 
                        //condition
                        bool conditionPassed = true;
                        var data = (from o in d.ProjectDatas.Where(f => f.ProjectID == evt.ProjectID && f.VersionDeletedBy == null && f.Version == 0)
                                        join t in d.ProjectDataTemplates.Where(f => f.Version == 0 && f.VersionDeletedBy == null) on o.ProjectDataTemplateID equals t.ProjectDataTemplateID
                                        select new { t.CommonName, o.Value, t.SystemDataType, o.VersionUpdated }
                                       ).GroupBy(f => f.CommonName, f => f, (key, g) => g.OrderByDescending(f => f.VersionUpdated).FirstOrDefault()).ToArray();                        
                        if (trigger.Trigger.Condition != null &&  !string.IsNullOrWhiteSpace(trigger.Trigger.Condition.Condition))
                        {                            
                            var dict = data.ToDictionary(f => "{{" + f.CommonName + "}}", f => (f.Value ?? "").Replace("\'", "\\\'").Replace("\"", "\\\""));
                            foreach (var lookup in dict)
                                trigger.Trigger.Condition.Condition = trigger.Trigger.Condition.Condition.Replace(lookup.Key, "\"" + lookup.Value + "\"");
                            if (ConstantsHelper.REGEX_JS_CLEANER.IsMatch(trigger.Trigger.Condition.Condition))
                            {
                                evt.Reason = "ILLEGAL CONDITION"; //16 max
                                evt.Failed = now;
                                evt.RunsLeft = 0; //critical failure
                                evt.RunNext = null;
                                conditionPassed = false;
                            }
                            else
                            {
                                conditionPassed = EquateAsync(trigger.Trigger.Condition.Condition);
                                if (!conditionPassed)
                                {
                                    evt.Reason = "CONDITION FAILED"; //16 max
                                    evt.Failed = now;
                                }
                            }
                           
                        }
                        if (conditionPassed)
                        {
                            //Execute here
                            //Parse variables
                            dynamic settings = Newtonsoft.Json.Linq.JObject.Parse(HttpUtility.HtmlDecode(trigger.Trigger.JSON));
                            var jsLookup = data.Where(f => !string.IsNullOrWhiteSpace(f.CommonName))
                                .ToDictionary(f =>
                                    string.Format("{0}", f.CommonName).Replace(" ", "_").Replace("\'", "_").Replace("\"", "_")
                                    , f => (f.Value ?? ""));
                            var txtLookup = data.ToDictionary(f => "{{" + f.CommonName + "}}", f => (f.Value ?? "").Replace("\'", "\\\'").Replace("\"", "\\\""));
                            Func<string, string> clean = (string toClean) =>
                            {
                                if (string.IsNullOrWhiteSpace(toClean))
                                    return "";
                                foreach (var l in txtLookup)
                                    toClean = toClean.Replace(l.Key, l.Value);
                                return toClean;
                            };
                            bool success = true;
                            Continuation continuation = null;
                            IDictionary<string, JToken> lookup = null;
                            string strNewCompanyID = null, strNewContactID = null, strNewWorkflowID = null, strCompanyLevel = null, strRelationship = null;
                            try
                            {
                                switch (string.Format("{0}", trigger.Trigger.JsonMethod).ToLowerInvariant())
                                {
                                    case "email":
                                        settings.email.recipient.Value = clean(settings.email.recipient.Value);
                                        if (!RegexHelper.IsEmail(settings.email.recipient.Value))
                                        {
                                            success = false;
                                            evt.Reason = "INVALID EMAIL";
                                            break;
                                        }
                                        settings.email.message.Value = clean(settings.email.message.Value);
                                        settings.email.subject.Value = clean(settings.email.subject.Value);
                                        success = SendEmail(settings);
                                        if (!success)
                                            evt.Reason = "SVR RESP FAILED";
                                        break;
                                    case "webhook":
                                        string js = JsonConvert.SerializeObject(jsLookup, Formatting.Indented, new JsonSerializerSettings
                                        {
                                            TypeNameHandling = TypeNameHandling.All,
                                            TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple
                                        });
                                        success = SendWebhook(settings, js);
                                        if (!success)
                                            evt.Reason = "HOOK RESP FAILED";
                                        break;
                                    case "csingle": //Single continuation
                                        //if (((IDictionary<string, Object>)settings).ContainsKey("continuation.single"))
                                        //{
                                        //}      
                                        lookup = ((IDictionary<string, JToken>)settings.csingle);

                                        strNewCompanyID = lookup.ContainsKey("NewCompanyID") ? lookup["NewCompanyID"].Value<string>() : null;
                                        strNewContactID = lookup.ContainsKey("NewContactID") ? lookup["NewContactID"].Value<string>() : null;
                                        strNewWorkflowID = lookup.ContainsKey("NewWorkflowID") ? lookup["NewWorkflowID"].Value<string>() : null;
                                        strCompanyLevel = lookup.ContainsKey("CompanyLevel") ? lookup["CompanyLevel"].Value<string>() : null;
                                        strRelationship = lookup.ContainsKey("Relationship") ? lookup["Relationship"].Value<string>() : null;

                                        continuation = new Continuation
                                        {
                                            EventID = evt.ProjectPlanTaskResponseEventID,

                                            OldWorkflowID = trigger.GraphDataGroupID,
                                            OldWorkflowCompanyID = trigger.GraphDataGroup.VersionOwnerCompanyID,
                                            OldWorkflowContactID = trigger.GraphDataGroup.VersionOwnerContactID,

                                            OldStepID = evt.ProjectPlanTaskResponseID,
                                            OldStepCompanyID = evt.ProjectPlanTaskResponse.VersionOwnerCompanyID,
                                            OldStepContactID = evt.ProjectPlanTaskResponse.VersionOwnerContactID,


                                            NewCompanyID = string.IsNullOrWhiteSpace(strNewCompanyID) ? (Guid?)null : Guid.Parse(strNewCompanyID),
                                            NewContactID = string.IsNullOrWhiteSpace(strNewContactID) ? (Guid?)null : Guid.Parse(strNewContactID),
                                            NewWorkflowID = string.IsNullOrWhiteSpace(strNewWorkflowID) ? (Guid?)null : Guid.Parse(strNewWorkflowID),
                                            CompanyLevel = string.IsNullOrWhiteSpace(strCompanyLevel) ? (int?)null : int.Parse(strCompanyLevel),
                                        };
                                        switch (string.Format("{0}", strRelationship).Trim().ToLowerInvariant())
                                        {
                                            case "parent":
                                                continuation.Relationship = (uint?)Continuation.RelationshipType.Parent;
                                                break;
                                            case "child":
                                                continuation.Relationship = (uint)Continuation.RelationshipType.Child;
                                                break;
                                            case "peer":
                                                continuation.Relationship = (uint)Continuation.RelationshipType.Peer;
                                                break;
                                            case "self":
                                                continuation.Relationship = (uint)Continuation.RelationshipType.Self;
                                                break;
                                            default:
                                                continuation.Relationship = null;
                                                break;
                                        }

                                        SendContinuationSingle(continuation);
                                        break;
                                    case "cmulti": //Multi continuation
                                        lookup = ((IDictionary<string, JToken>)settings.cmulti);
                                        strNewCompanyID = lookup.ContainsKey("NewCompanyID") ? lookup["NewCompanyID"].Value<string>() : null;
                                        strNewContactID = lookup.ContainsKey("NewContactID") ? lookup["NewContactID"].Value<string>() : null;
                                        strNewWorkflowID = lookup.ContainsKey("NewWorkflowID") ? lookup["NewWorkflowID"].Value<string>() : null;
                                        strCompanyLevel = lookup.ContainsKey("CompanyLevel") ? lookup["CompanyLevel"].Value<string>() : null;
                                        strRelationship = lookup.ContainsKey("Relationship") ? lookup["Relationship"].Value<string>() : null;
                                        continuation = new Continuation
                                        {

                                            EventID = evt.ProjectPlanTaskResponseEventID,

                                            OldWorkflowID = trigger.GraphDataGroupID,
                                            OldWorkflowCompanyID = trigger.GraphDataGroup.VersionOwnerCompanyID,
                                            OldWorkflowContactID = trigger.GraphDataGroup.VersionOwnerContactID,

                                            OldStepID = evt.ProjectPlanTaskResponseID,
                                            OldStepCompanyID = evt.ProjectPlanTaskResponse.VersionOwnerCompanyID,
                                            OldStepContactID = evt.ProjectPlanTaskResponse.VersionOwnerContactID,


                                            NewCompanyID = string.IsNullOrWhiteSpace(strNewCompanyID) ? (Guid?)null : Guid.Parse(strNewCompanyID),
                                            NewContactID = string.IsNullOrWhiteSpace(strNewContactID) ? (Guid?)null : Guid.Parse(strNewContactID),
                                            NewWorkflowID = string.IsNullOrWhiteSpace(strNewWorkflowID) ? (Guid?)null : Guid.Parse(strNewWorkflowID),
                                            CompanyLevel = string.IsNullOrWhiteSpace(strCompanyLevel) ? (int?)null : int.Parse(strCompanyLevel),
                                        };
                                        switch (string.Format("{0}", strRelationship).Trim().ToLowerInvariant())
                                        {
                                            case "parent":
                                                continuation.Relationship = (uint?)Continuation.RelationshipType.Parent;
                                                break;
                                            case "child":
                                                continuation.Relationship = (uint)Continuation.RelationshipType.Child;
                                                break;
                                            case "peer":
                                                continuation.Relationship = (uint)Continuation.RelationshipType.Peer;
                                                break;
                                            case "self":
                                                continuation.Relationship = (uint)Continuation.RelationshipType.Self;
                                                break;
                                            default:
                                                continuation.Relationship = null;
                                                break;
                                        }
                                        SendContinuationMulti(continuation);
                                        break;
                                    default:
                                        evt.Reason = "ILLEGAL METHOD"; //16 max
                                        evt.Failed = now;
                                        evt.RunsLeft = 0; //critical failure
                                        evt.RunNext = null;
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                success = false;
                                evt.Reason = string.Join("", string.Format("{0}", ex.Message).Take(16).ToArray());
                            }
                            if (!success)
                            {
                                if (string.IsNullOrWhiteSpace(evt.Reason))
                                    evt.Reason = "UNKNOWN RESPONSE";
                                evt.Failed = now;
                            }

                        }

                         if (evt.RunsLeft > 0)
                            evt.RunsLeft--;        
                        if (evt.RunsLeft > 0 && evt.RunNext.HasValue)
                        {
                            var oldRunTime = evt.RunNext;
                            //Calculate next run
                            var ts = new TimeSpan(0);
                            if (trigger.Trigger.DelayDays.HasValue)
                                ts = ts.Add(new TimeSpan(trigger.Trigger.DelayDays.Value, 0, 0, 0));
                            if (trigger.Trigger.DelaySeconds.HasValue)
                                ts = ts.Add(new TimeSpan(0, 0, trigger.Trigger.DelaySeconds.Value));
                            if (trigger.Trigger.DelayWeeks.HasValue)
                                ts = ts.Add(new TimeSpan(7 * trigger.Trigger.DelayWeeks.Value, 0, 0, 0));
                            evt.RunNext = evt.RunNext.Value.Add(ts);
                            if (trigger.Trigger.DelayMonths.HasValue)
                                evt.RunNext = evt.RunNext.Value.AddMonths(trigger.Trigger.DelayMonths.Value);
                            if (trigger.Trigger.DelayYears.HasValue)
                                evt.RunNext = evt.RunNext.Value.AddYears(trigger.Trigger.DelayYears.Value);
                            if (evt.RunNext.Value.AddMinutes(-2) <= oldRunTime)
                            {
                                evt.RunNext = null;
                                evt.RunsLeft = 0;
                                evt.Reason = "REPEATED TOO SOON";
                            }
                        }
                        else
                        {
                            evt.RunsLeft = 0;
                        }
                        evt.Executed = now;
                    }
                     


                    //Count API requests
                    if (contactTicks.ContainsKey(evt.VersionOwnerContactID))
                        contactTicks[evt.VersionOwnerContactID]++;
                    else
                        contactTicks[evt.VersionOwnerContactID] = 1;
                    
                    if (companyTicks.ContainsKey(evt.VersionOwnerCompanyID))
                        companyTicks[evt.VersionOwnerCompanyID]++;
                    else
                        companyTicks[evt.VersionOwnerCompanyID] = 1;
                }
                d.SaveChanges();
            }
            foreach(var tick in contactTicks.Keys)
                ContactTick(tick, contactTicks[tick]);
            foreach (var tick in companyTicks.Keys)
                CompanyTick(tick, companyTicks[tick]);
            return true;
        }


        private bool SendWebhook(dynamic settings, string js)
        {

            var webhook = (HttpWebRequest)WebRequest.Create(settings.webhook.url.Value);
            webhook.ContentType = "text/json";
            webhook.Method = "POST";
            //httpWebRequest.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => { return true; };

            using (var streamWriter = new StreamWriter(webhook.GetRequestStream()))
            {              

                streamWriter.Write(js);
                streamWriter.Flush();
                streamWriter.Close();

                var response = (HttpWebResponse)webhook.GetResponse();
                int statusCode = (int)response.StatusCode;
                if (statusCode >= 100 && statusCode < 400) //Good requests
                    return true;
                //using (var streamReader = new StreamReader(response.GetResponseStream()))
                //{
                //    var result = streamReader.ReadToEnd();
                //}
            }
            return false;
        }

        private bool SendEmail(dynamic settings)
        {
            try
            {
                _users.EmailUsers(new string[] { settings.email.recipient.Value }, settings.email.subject.Value, settings.email.message.Value);
            }
            catch
            {
                return false;
            }
            return true;
        }

        private bool SendSMS(dynamic settings)
        {
            return false;
        }

        private bool SendContinuationSingle(Continuation settings)
        {
            //Create Task
            ContactID = settings.OldStepContactID;
            CompanyID = settings.OldStepCompanyID;
            if (!Authorize(null, ActionPermission.Create, typeof(ProjectPlanTaskResponse)))
                return false;
            var m = new AutomationViewModel { };
            m.IncludeContent = false;
            m.PreviousStep = new ProjectPlanTaskResponse { ProjectPlanTaskResponseID = Guid.NewGuid(), ActualGraphDataGroupID = settings.NewWorkflowID };
            m.PreviousTask = new NKD.Module.BusinessObjects.Task { };
            var wfi = DoNext(m);
            if (wfi)
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    var rd = new ProjectPlanTaskResponseData
                    {
                        ProjectPlanTaskResponseDataID = Guid.NewGuid(),
                        ProjectPlanTaskResponseID = m.PreviousStepID.Value,
                        TableType = d.GetTableName(typeof(ProjectPlanTaskResponseEvent)),
                        ReferenceID = settings.EventID,
                        VersionOwnerCompanyID = settings.OldStepCompanyID,
                        VersionOwnerContactID = settings.OldStepContactID,
                        VersionUpdated = DateTime.UtcNow
                    };
                    d.ProjectPlanTaskResponseDatas.AddObject(rd);
                    d.SaveChanges();
                    return true;
                }
            }
            return false;
        }

        private bool SendContinuationMulti(Continuation settings)
        {
            try
            {
                ContactID = settings.OldStepContactID;
                CompanyID = settings.OldStepCompanyID;
                if (!Authorize(null, ActionPermission.Create, typeof(ProjectPlanTaskResponse)))
                    return false;
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    Guid[] companies = new Guid[] { };
                    if (!settings.NewCompanyID.HasValue)
                    {
                        //companylevel
                        var wfCompanies = (from o in d.E_UDF_ContactCompanies(settings.OldWorkflowContactID.Value)
                                           join c in d.CompanyRelations.Where(f => f.Version == 0 && f.VersionDeletedBy == null)
                                           on o.Value equals c.CompanyID
                                           select new { c.CompanyID, c.ParentCompanyID }).ToArray();
                        var nulls = (from o in wfCompanies where o.ParentCompanyID == null select o.CompanyID);
                        var lvlCompanies = new List<Guid>();
                        Action<Guid, int> recurse = null;
                        recurse = new Action<Guid, int>((Guid c, int l) =>
                        {
                            if (l < settings.CompanyLevel && l < 50)
                            {
                                foreach (var wfc in wfCompanies)
                                {
                                    if (wfc.ParentCompanyID == c)
                                        recurse(wfc.CompanyID, l++);
                                }
                            }
                            else if (l == settings.CompanyLevel)
                            {
                                lvlCompanies.Add(c);
                            }

                        });
                        foreach (var l1 in nulls)
                        {
                            recurse(l1, 1);
                        }
                        companies = lvlCompanies.ToArray();
                    }
                    else
                    {
                        companies = new Guid[] { settings.NewCompanyID.Value };
                    }
                    var defaultCompanies = new Guid[] {
                                                     _users.ApplicationCompanyID,
                                                     Guid.Empty,
                                                    UsersService.COMPANY_DEFAULT};
                    companies = companies.Except(defaultCompanies).ToArray();
                    foreach (var company in companies)
                    {
                        var recipients = (from o in d.Experiences.Where(f =>
                                            (f.DateFinished == null || f.DateFinished > DateTime.UtcNow)
                                     && (f.Expiry == null || f.Expiry > DateTime.UtcNow)
                                     && f.CompanyID != null
                                     && (f.DateStart <= DateTime.UtcNow || f.DateStart == null)
                                     && f.Version == 0 && f.VersionDeletedBy == null
                                            )
                                        where o.CompanyID != null && o.CompanyID == company //companies.Contains(o.CompanyID.Value)
                                        select o.Contact);

                        var senders = recipients; //peer & itself
                        var senderCompanies = new Guid[] {};
                        if (settings.Relationship == (uint)Continuation.RelationshipType.Parent)
                        {
                            senderCompanies = d.CompanyRelations.Where(f=>f.Version ==0 && f.VersionDeletedBy == null  && f.CompanyID == company).Select(f=>f.ParentCompanyID).ToArray();
                        }
                        if (settings.Relationship == (uint)Continuation.RelationshipType.Child)
                        {
                            senderCompanies = d.CompanyRelations.Where(f => f.Version == 0 && f.VersionDeletedBy == null && f.ParentCompanyID == company).Select(f => f.CompanyID).ToArray();
                        }
                        senderCompanies = senderCompanies.Except(defaultCompanies).ToArray();
                        if (senderCompanies.Any())
                        {
                            senders = (from o in d.Experiences.Where(f =>
                                            (f.DateFinished == null || f.DateFinished > DateTime.UtcNow)
                                     && (f.Expiry == null || f.Expiry > DateTime.UtcNow)
                                     && f.CompanyID != null
                                     && (f.DateStart <= DateTime.UtcNow || f.DateStart == null)
                                     && f.Version == 0 && f.VersionDeletedBy == null
                                            )
                                where o.CompanyID != null && senderCompanies.Contains(o.CompanyID.Value)
                                select o.Contact);
                        }

                        //Get ready for impersonation
                        var newwf = d.GraphDataGroups.Where(f => f.Version == 0 && f.VersionDeletedBy == null && f.GraphDataGroupID == settings.NewWorkflowID).Single();
                        settings.OldWorkflowCompanyID = newwf.VersionOwnerCompanyID;
                        settings.OldWorkflowContactID = newwf.VersionOwnerContactID;


                        foreach (var recipient in recipients)
                        {
                            if (recipient == null)
                                continue;
                            //Authorize impersonation
                            ContactID = recipient.ContactID;
                            CompanyID = company; //This is our NewWorkflowID for multi
                            if (!Authorize(settings.NewWorkflowID, ActionPermission.Read, typeof(GraphDataGroup)))
                                continue;
                            //Now Impersonate
                            CompanyID = settings.OldWorkflowCompanyID; //Can't choose who owns the instance, so revert back to WF owner
                            ContactID = settings.OldWorkflowContactID;

                            foreach (var sender in senders)
                            {
                                if (sender == null)
                                    continue;
                                if (sender.ContactID == recipient.ContactID && settings.Relationship != (uint)Continuation.RelationshipType.Self)
                                    continue;
                                if (sender.ContactID != recipient.ContactID && settings.Relationship == (uint)Continuation.RelationshipType.Self)
                                    continue;

                                var m = new AutomationViewModel { };
                                m.IncludeContent = false;
                                m.PreviousStep = new ProjectPlanTaskResponse { ProjectPlanTaskResponseID = Guid.NewGuid(), ActualGraphDataGroupID = settings.NewWorkflowID };
                                m.PreviousTask = new NKD.Module.BusinessObjects.Task { };
                                m.QueryParams = new Dictionary<string, object>();
                                m.QueryParams.Add("Regarding", string.Format("{0} {1} ({2})", sender.Firstname, sender.Surname, sender.Username));
                                m.QueryParams.Add("RegardingContactID", sender.ContactID);
                                var wfi = DoNext(m);
                                if (wfi)
                                {
                                    var step = d.ProjectPlanTaskResponses.Where(f => f.Version == 0 && f.VersionDeletedBy == null && f.ProjectPlanTaskResponseID == m.PreviousStepID).Single();
                                    step.VersionOwnerCompanyID = company;
                                    step.VersionOwnerContactID = recipient.ContactID;
                                    var rd = new ProjectPlanTaskResponseData
                                    {
                                        ProjectPlanTaskResponseDataID = Guid.NewGuid(),
                                        ProjectPlanTaskResponseID = m.PreviousStepID.Value,
                                        TableType = d.GetTableName(typeof(ProjectPlanTaskResponseEvent)),
                                        ReferenceID = settings.EventID,
                                        VersionOwnerCompanyID = settings.OldWorkflowCompanyID,
                                        VersionOwnerContactID = settings.OldWorkflowContactID,
                                        VersionUpdated = DateTime.UtcNow
                                    };
                                    d.ProjectPlanTaskResponseDatas.AddObject(rd);
                                }
                                //Send email to assignee/s
                                //settings.email.message.Value += string.Format("<br/><br/><p>See more detail at FlowPro:</p><a href=\"http://flowpro.io/flow#/step/{0}\">http://flowpro.io/flow#/step/{0}</a>", evt.ProjectPlanTaskResponseID);
                            }
                        }
                    }
                    d.SaveChanges();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }


        private bool CancelWorkflow()
        {
            return false;
        }

        private bool PauseWorkflow()
        {
            return false;

        }


    }
}
