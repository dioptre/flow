using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using Orchard;
using System.ServiceModel;
using NKD.ViewModels;
using EXPEDIT.Flow.ViewModels;
using EXPEDIT.Share.ViewModels;
using System.Data;
using NKD.Models;

namespace EXPEDIT.Flow.Services
{
    [ServiceContract]
    public interface IFlowService : IDependency
    {
        [OperationContract]
        IEnumerable<SearchViewModel> Search(string query, int? start = 0, int? pageSize = 20, SearchType? st = SearchType.Flow, DateTime? dateFrom = default(DateTime?), DateTime? dateUntil = default(DateTime?), string viewport = null);

        [OperationContract]
        WikiViewModel GetWiki(string wikiName, Guid? nid);

        [OperationContract]
        bool GetDuplicateNode(string wikiName, Guid? id = default(Guid?));

        [OperationContract]
        bool SubmitWiki(ref WikiViewModel wiki);

        [OperationContract]
        FlowGroupViewModel GetNode(string name, Guid? nid, Guid? gid, bool includeContent = false, bool includeDisconnected = false, bool monitor = true);

        [OperationContract]
        bool CreateNode(FlowViewModel flow);

        [OperationContract]
        bool UpdateNode(FlowViewModel flow);

        [OperationContract]
        bool DeleteNode(Guid nid);

        [OperationContract]
        bool UnlinkNode(Guid nid, Guid? gid = null);

        [OperationContract]
        bool CreateEdge(FlowEdgeViewModel flow);
        [OperationContract]
        bool UpdateEdge(FlowEdgeViewModel m);
        [OperationContract]
        bool DeleteEdge(Guid eid);


        [OperationContract]
        FlowEdgeWorkflowViewModel GetWorkflow(Guid id);

        [OperationContract]
        bool CreateWorkflow(FlowEdgeWorkflowViewModel flow);

        [OperationContract]
        bool UpdateWorkflow(FlowEdgeWorkflowViewModel flow);

        [OperationContract]
        bool CheckPayment();

        [OperationContract]
        UserProfileViewModel GetMyProfile();

        [OperationContract]
        bool UpdateProfile(UserProfileViewModel user);

        [OperationContract]
        ContactViewModel GetMyInfo();

        [OperationContract]
        IEnumerable<SearchViewModel> GetMyFiles();

        [OperationContract]
        bool GetDuplicateWorkflow(string workflowName, Guid? id = default(Guid?));

        [OperationContract]
        IEnumerable<SearchViewModel> GetMyNodes();

        [OperationContract]
        IEnumerable<SearchViewModel> GetMyWorkflows();

        [OperationContract]
        IEnumerable<SecurityViewModel> GetMySecurityLists(string table);

        [OperationContract]
        bool CreateSecurity(SecurityViewModel m);

        [OperationContract]
        bool DeleteSecurity(Guid sid, int? SecurityTypeID);

        [OperationContract]
        bool AssignLicense(Guid userid, Guid licenseid);

        [OperationContract]
        IEnumerable<EXPEDIT.Flow.ViewModels.LicenseViewModel> GetMyLicenses(Guid? licenseID = default(Guid?));

        [OperationContract]
        bool GetDuplicateWorkflow(Guid gid);
        [OperationContract]
        bool GetDuplicateNode(Guid gid);
        [OperationContract]
        bool CheckWorkflowPermission(Guid gid, ActionPermission permission);
        [OperationContract]
        bool CheckNodePermission(Guid gid, ActionPermission permission);

        [OperationContract]
        bool GetTranslation(TranslationViewModel m);

        [OperationContract]
        bool UpdateTranslation(TranslationViewModel m);

        [OperationContract]
        bool DeleteTranslation(TranslationViewModel m);


        [OperationContract]
        bool GetLocale(LocaleViewModel m);

        [OperationContract]
        bool UpdateLocale(LocaleViewModel m);

        [OperationContract]
        bool DeleteLocale(LocaleViewModel m);

        [OperationContract]
        bool CreateLocale(LocaleViewModel m);


        [OperationContract]
        AutomationViewModel GetStep(Guid? sid, Guid? pid, Guid? tid, Guid? nid, Guid? gid, bool includeContent = false, bool includeDisconnected = false, bool monitor = true);
        [OperationContract]
        AutomationViewModel[] GetMySteps();

        [OperationContract]
        bool CreateStep(AutomationViewModel flow);

        [OperationContract]
        bool UpdateStep(AutomationViewModel flow);

        [OperationContract]
        bool DeleteStep(Guid stepID);


        [OperationContract]
        ProjectViewModel GetProject(Guid pid);

        [OperationContract]
        ProjectDataViewModel[] GetProjectData(Guid[] pdid);
        [OperationContract]
        bool CreateProjectData(ProjectDataViewModel m);
        [OperationContract]
        bool UpdateProjectData(ProjectDataViewModel m);
        [OperationContract]
        bool DeleteProjectData(ProjectDataViewModel m);

        [OperationContract]
        EdgeConditionViewModel[] GetEdgeCondition(Guid[] pdid);
        [OperationContract]
        bool CreateEdgeCondition(EdgeConditionViewModel m);
        [OperationContract]
        bool UpdateEdgeCondition(EdgeConditionViewModel m);
        [OperationContract]
        bool DeleteEdgeCondition(EdgeConditionViewModel m);

        ContextVariableViewModel[] GetContextNames(Guid wfid);


        [OperationContract]
        ConditionViewModel GetCondition(Guid id);
        [OperationContract]
        bool CreateCondition(ConditionViewModel m);
        [OperationContract]
        bool UpdateCondition(ConditionViewModel m);
        [OperationContract]
        bool DeleteCondition(ConditionViewModel m);

        [OperationContract]
        TaskViewModel GetTask(Guid gid, Guid nid);
        [OperationContract]
        TaskViewModel GetTask(Guid id);
        [OperationContract]
        bool CreateTask(TaskViewModel m);
        [OperationContract]
        bool UpdateTask(TaskViewModel m);
        [OperationContract]
        bool DeleteTask(TaskViewModel m);

        [OperationContract]
        TriggerViewModel GetTrigger(Guid id);
        [OperationContract]
        bool CreateTrigger(TriggerViewModel m);
        [OperationContract]
        bool UpdateTrigger(TriggerViewModel m);
        [OperationContract]
        bool DeleteTrigger(TriggerViewModel m);

        [OperationContract]
        void Cleanup();
    }
}