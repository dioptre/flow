﻿using System;
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
        bool CreateEdge(FlowEdgeViewModel flow);

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


    }
}