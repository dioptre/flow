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
using NKD.Module.BusinessObjects;

namespace EXPEDIT.Flow.Services
{
    [ServiceContract]
    public interface IAutomationService : IDependency
    {
       

        [OperationContract]
        bool ExecuteMethod(AutomationViewModel m,string method);

        [OperationContract]
        bool DoNext(AutomationViewModel m);

        [OperationContract]
        bool DoAs(AutomationViewModel m);

        [OperationContract]
        bool Checkin(Guid stepID);

        [OperationContract]
        bool NotifyTransition(AutomationViewModel m);

        [OperationContract]
        bool Authenticate(AutomationViewModel m, string method);

        [OperationContract]
        bool Authorize(AutomationViewModel m, Guid? gid, ActionPermission permission, Type typeToCheck);

        AutomationViewModel GetStep(NKDC d, Guid sid, Guid? tid = null, bool includeContent = false, string locale = "en-US");

        bool QuenchStep(NKDC d, AutomationViewModel m);

        bool ProcessEvents();

        bool AiDuration(AutomationViewModel m);

    }
}