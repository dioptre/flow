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

namespace EXPEDIT.Flow.Services {

    [UsedImplicitly]
    public class AutomationService : IAutomationService
    {
        private readonly IOrchardServices _services;
        private readonly IUsersService _users;
        private readonly IWorkflowService _wf;

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
                m.ProxyApplicationID = applicationID;
                m.ProxyCompanyID = trigger.JsonProxyCompanyID;
                m.ProxyContactID = trigger.JsonProxyContactID;
                return true;

            }
        }

        public bool Authorize(AutomationViewModel m, Guid? gid, ActionPermission permission, Type typeToCheck)
        {
            var contact = m.ProxyContactID ?? _users.ContactID;
            var application = m.ProxyApplicationID ?? _users.ApplicationID;
            //var company = m.ProxyCompanyID ?? _users.DefaultContactCompanyID;
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

        public bool DoNext(AutomationViewModel m)
        {
            var contact = m.ProxyContactID ?? _users.ContactID;
            var application = m.ProxyApplicationID ?? _users.ApplicationID;
            var company = m.ProxyCompanyID ?? _users.DefaultContactCompanyID;
            var now = DateTime.UtcNow;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                //if no stepid - lets make one
                if (!m.PreviousStepID.HasValue)
                    m.PreviousStep.ProjectPlanTaskResponseID = Guid.NewGuid();
                //If no projectID, instantiate workflow - need graphdatagroupid
                if (!m.ProjectID.HasValue && !d.ProjectPlanTaskResponses.Any(f => f.ProjectPlanTaskResponseID == m.PreviousStepID && f.VersionDeletedBy == null && f.Version == 0))
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
                            WorkflowInstanceID = Guid.NewGuid() ,
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
                            Idle = null ,
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
                //TODO: Prob should check whether its checked out

                //m.PreviousStep.ActualGraphDataID

                //if final transition send 000000-0000-00000000

                //if step (response) & graphdataid go through graphdatarelationconditions for next transition            

                //Transition & change owner to responsibleowner if task exists

                //run trigger on out

                //always create an event if successful or not

                //run trigger on in

                //always create an event if successful or not

                //update response

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
