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
         string Search(string query = "");

    }
}