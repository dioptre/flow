using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using Orchard;
using System.ServiceModel;

using EXPEDIT.Flow.ViewModels;
using EXPEDIT.Share.ViewModels;

namespace EXPEDIT.Flow.Services
{
     [ServiceContract]
    public interface IFlowService : IDependency 
    {
         [OperationContract]
         IEnumerable<SearchViewModel> Search(string query, int? start = 0, int? pageSize = 20, SearchType? st = SearchType.Flow);

         [OperationContract]
         WikiViewModel GetWiki(string wikiName, Guid? nid);

         [OperationContract]
         bool GetDuplicateNode(string wikiName);

         [OperationContract]
         bool SubmitWiki(ref WikiViewModel wiki);

         [OperationContract]
         FlowGroupViewModel GetNode(string name, Guid? nid, Guid? gid, bool includeContent = false);

         [OperationContract]
         bool CreateNode(FlowViewModel flow);

         [OperationContract]
         bool UpdateNode(FlowViewModel flow);

         [OperationContract]
         bool DeleteNode(FlowViewModel flow);

         [OperationContract]
         bool CreateEdge(FlowEdgeViewModel flow);

         [OperationContract]
         bool DeleteEdge(FlowEdgeViewModel flow);

    }
}