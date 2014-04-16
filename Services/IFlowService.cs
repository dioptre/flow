using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using Orchard;
using System.ServiceModel;

using EXPEDIT.Flow.ViewModels;

namespace EXPEDIT.Flow.Services
{
     [ServiceContract]
    public interface IFlowService : IDependency 
    {
         [OperationContract]
         dynamic Search(string query, int? start = 0, int? pageSize = 20, SearchType? st = SearchType.Flow);

         [OperationContract]
         WikiViewModel GetWiki(string wikiName);

         [OperationContract]
         bool GetDuplicateWiki(string wikiName);

         [OperationContract]
         bool SubmitWiki(ref WikiViewModel wiki);

    }
}