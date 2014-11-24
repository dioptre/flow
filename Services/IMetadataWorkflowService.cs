using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using Orchard;
using NKD.Models;
using System.ServiceModel;
using Orchard.Media.Models;
using NKD.ViewModels;
using System.Threading.Tasks;
using Orchard.ContentManagement;

namespace EXPEDIT.Flow.Services
{
     [ServiceContract]
    public interface IMetadataWorkflowService : IDependency 
    {

         [OperationContract]
         Guid AssignMetadata(Guid? tryWorkflowID, Dictionary<string, object> lookup);

         string CurrentState
         {
             [OperationContract]
             get;
         }

         [OperationContract]
         T GetMetadata<T>(Guid workflowID, string lookup);

       
         [OperationContract]
         bool CompleteProcess(Guid companyID, Guid contactID, Guid workflowID);



    }
}