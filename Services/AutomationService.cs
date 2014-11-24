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

namespace EXPEDIT.Flow.Services {

    [UsedImplicitly]
    public class AutomationService : IAutomationService
    {
        private readonly IOrchardServices _services;
        private readonly IUsersService _users;

        public AutomationService(
            IOrchardServices orchardServices,
            IUsersService users
            )
        {
            _users = users;
            _services = orchardServices;

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
                var d = new NKDC(null, null);
                var trigger = d.Triggers.FirstOrDefault(f => f.JsonProxyApplicationID == applicationID && f.JsonUsername == m.Username && f.JsonPassword == hashString && f.JsonMethod == method);
                if (trigger == null)
                    return false;
                m.ProxyApplicationID = applicationID;
                m.ProxyCompanyID = trigger.JsonProxyCompanyID;
                m.ProxyContactID = trigger.JsonProxyContactID;
                return true;

            }
        }

        public bool Authorize(AutomationViewModel m, Guid gid, ActionPermission permission, Type typeToCheck)
        {
            var contact = m.ProxyContactID ?? _users.ContactID;
            var application = m.ProxyApplicationID ?? _users.ApplicationID;
            //var company = m.ProxyCompanyID ?? _users.DefaultContactCompanyID;
            var d = new NKDC(_users.ApplicationConnectionString, null);
            var table = d.GetTableName(typeToCheck);
            return _users.CheckPermission(new SecuredBasic
            {
                AccessorApplicationID = application,
                AccessorContactID = contact,
                OwnerTableType = table
            }, permission);
        }

        public bool DoNext(AutomationViewModel m)
        {
            //If no projectID, instantiate workflow - need graphdatagroupid
            //m.PreviousStep.ActualGraphDataID

            //if final transition send 000000-0000-00000000

            //if step (response) & graphdataid go through graphdatarelationconditions for next transition            

            //run trigger on out

            //always create an event if successful or not

            //run trigger on in

            //always create an event if successful or not

            //update response

            return false;
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
