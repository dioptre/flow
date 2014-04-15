using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using JetBrains.Annotations;
using Orchard.ContentManagement;
using Orchard.FileSystems.Media;
using Orchard.Localization;
using Orchard.Security;
using Orchard.Settings;
using Orchard.Validation;
using Orchard;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Transactions;
using Orchard.Messaging.Services;
using Orchard.Logging;
using Orchard.Tasks.Scheduling;
using Orchard.Data;
using NKD.Module.BusinessObjects;
using NKD.Services;
using Orchard.Media.Services;
using EXPEDIT.Flow.ViewModels;

using Orchard.DisplayManagement;
using ImpromptuInterface;
using NKD.Models;
using System.Data.SqlClient;
using System.Data;



namespace EXPEDIT.Flow.Services {
    
    [UsedImplicitly]
    public class FlowService : IFlowService {

        private readonly IOrchardServices _orchardServices;
        private readonly IContentManager _contentManager;
        private readonly IMessageManager _messageManager;
        private readonly IScheduledTaskManager _taskManager;
        private readonly IUsersService _users;
        private readonly IMediaService _media;
        public ILogger Logger { get; set; }

        public FlowService(
            IContentManager contentManager, 
            IOrchardServices orchardServices, 
            IMessageManager messageManager, 
            IScheduledTaskManager taskManager, 
            IUsersService users, 
            IMediaService media)
        {
            _orchardServices = orchardServices;
            _contentManager = contentManager;
            _messageManager = messageManager;
            _taskManager = taskManager;
            _media = media;
            _users = users;
            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
        }

        public Localizer T { get; set; }



        public dynamic Search(string query, SearchType st, int start, int pageSize)
        {
            //if no results show wikipedia
            var application = _users.ApplicationID;
            var contact = _users.ContactID;                
            var company = _users.ApplicationCompanyID;
            var server = _users.ServerID;
            string s;
            switch(st){
                case SearchType.File:
                    s = "X_FileData";
                    break;
                case SearchType.Model:
                    s = "Q_SupplierModel";
                    break;
                default:
                    s = "E_GraphData";
                    break;
            }
            var allCompanies = new Dictionary<Guid, string>();
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                using (DataTable table = new DataTable())
                {
                    using (var con = new SqlConnection(_users.ApplicationConnectionString))
                    using (var cmd = new SqlCommand("E_SP_GetSecuredSearch", con))
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        //query, contact, application, s, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, start, pageSize, verified, found
                        var qP = cmd.CreateParameter();
                        qP.ParameterName = "@text";
                        qP.DbType = DbType.String;
                        qP.Value = query;
                        cmd.Parameters.Add(qP);

                        var qC = cmd.CreateParameter();
                        qC.ParameterName = "@contactid";
                        qC.DbType = DbType.Guid;
                        qC.Value = contact;
                        cmd.Parameters.Add(qC);

                        var qA = cmd.CreateParameter();
                        qA.ParameterName = "@applicationid";
                        qA.DbType = DbType.Guid;
                        qA.Value = application;
                        cmd.Parameters.Add(qA);

                        var qT = cmd.CreateParameter();
                        qT.ParameterName = "@table";
                        qT.DbType = DbType.String;
                        qT.Value = s;
                        cmd.Parameters.Add(qT);

                        da.Fill(table);

                        return table;
                    }

                
                }

            }
      
            return null;
        }

    

       
    }
}
