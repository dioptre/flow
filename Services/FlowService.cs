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
using NKD.Helpers;
using System.Data.SqlClient;
using System.Data;
using EntityFramework.Extensions;
using EXPEDIT.License.Models;
using HtmlAgilityPack;
using EXPEDIT.Share.ViewModels;
using Newtonsoft.Json;
using CookComputing.XmlRpc;
using Orchard.Users.Events;
using NKD.ViewModels;

namespace EXPEDIT.Flow.Services {
    
    [UsedImplicitly]
    public class FlowService : IFlowService, Orchard.Users.Events.IUserEventHandler
    {

        public const string STAT_NAME_FLOW_ACCESS = "FlowAccess";
        public static Guid FLOW_MODEL_ID = new Guid("1DB0B648-D8A7-4FB9-8F3F-B2846822258C");
        public const string FS_FLOW_CONTACT_ID = "{0}:ValidFlowUser";

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


        public IEnumerable<SearchViewModel> Search(string query, int? start = 0, int? pageSize = 20, SearchType? st = SearchType.Flow,  DateTime? dateFrom = default(DateTime?), DateTime? dateUntil = default(DateTime?), string viewport = null)
        {
            //if no results show wikipedia
            var application = _users.ApplicationID;
            var contact = _users.ContactID;                
            var company = _users.ApplicationCompanyID;
            //var server = _users.ServerID;

            var results = new List<SearchViewModel>();
            var allCompanies = new Dictionary<Guid, string>();
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                string table;
                switch (st)
                {
                    case SearchType.File:
                        table = d.GetTableName(typeof(FileData));
                        break;
                    case SearchType.Model:
                        table = d.GetTableName(typeof(SupplierModel));
                        break;
                    case SearchType.FlowLocation:
                        table = d.GetTableName(typeof(GraphDataLocation));
                        break;
                    default:
                        table = d.GetTableName(typeof(GraphData));
                        break;
                }

                    using (var con = new SqlConnection(_users.ApplicationConnectionString))
                    using (var cmd = new SqlCommand("E_SP_GetSecuredSearch", con))
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
                        qT.Value = table;
                        cmd.Parameters.Add(qT);

                        var qPS = cmd.CreateParameter();
                        qPS.ParameterName = "@pageSize";
                        qPS.DbType = DbType.Int32;
                        qPS.Value = pageSize;
                        cmd.Parameters.Add(qPS);

                        var qPI = cmd.CreateParameter();
                        qPI.ParameterName = "@startRowIndex";
                        qPI.DbType = DbType.Int32;
                        qPI.Value = start;
                        cmd.Parameters.Add(qPI);

                        if (dateFrom.HasValue)
                        {
                            var qdF = cmd.CreateParameter();
                            qdF.ParameterName = "@dateFrom";
                            qdF.DbType = DbType.DateTime;
                            qdF.Value = dateFrom;
                            cmd.Parameters.Add(qdF);
                        }

                        if (dateUntil.HasValue)
                        {
                            var qdT = cmd.CreateParameter();
                            qdT.ParameterName = "@dateTo";
                            qdT.DbType = DbType.DateTime;
                            qdT.Value = dateUntil;
                            cmd.Parameters.Add(qdT);
                        }

                        if (!string.IsNullOrWhiteSpace(viewport))
                        {
                            var qV = cmd.CreateParameter();
                            qV.ParameterName = "@viewport";
                            //qV.DbType =  DbType.String;
                            //qV.UdtTypeName = "geography";
                            qV.Value = viewport;
                            cmd.Parameters.Add(qV);
                        }

                        con.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                     results.Add(new SearchViewModel
                                     {
                                         Row = reader[0] as long?,
                                         TotalRows = reader[1] as int?,
                                         Score = (reader[2] != null ? decimal.Parse(reader[2].ToString()) : default(decimal?)),
                                         id = reader[3] as Guid?,
                                         ReferenceID = reader[4] as Guid?,
                                         TableType = reader[5] as string,
                                         Title = reader[6] as string,
                                         Description = reader[7] as string,
                                         SpatialJSON = reader[8] as string,
                                         InternalUrl = reader[9] as string,
                                         ExternalUrl = reader[10] as string,
                                         Author = reader[11] as string,                        
                                         Updated = reader[12] as DateTime?
                                     });
                                   
                                }
                            }
                        }
                        con.Close();
                        
                    }

            }
            return results;
      
        }

        public WikiViewModel GetWiki(string wikiName, Guid? nid)
        {
            wikiName = wikiName.ToSlug();
            var companies = _users.ContactCompanies;
            var company = _users.DefaultContactCompanyID;
            var contact = _users.ContactID;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                bool isNew;
                WikiViewModel m;
                Guid? id;
                if (nid.HasValue)
                    id = CheckNodePrivileges(d, null, nid, companies, company, ActionPermission.Update, out isNew);
                else
                    id = CheckNodePrivileges(d, wikiName, null, companies, company, ActionPermission.Update, out isNew);
                if (id == null)
                    return null;
                if (isNew)
                    m = new WikiViewModel { GraphDataID = id.Value, IsNew = true, GraphName = wikiName};
                else
                {
                    m = (from o in d.GraphData where o.GraphDataID == id select 
                             new WikiViewModel {
                                 GraphName = o.GraphName,
                                 GraphDataID = o.GraphDataID,
                                 IsNew = false,
                                 GraphData = o.GraphContent
                             }
                             ).Single();
                }
                return m;
            }
        }

        public bool SubmitWiki(ref WikiViewModel m)
        {
            m.GraphName = m.GraphName.ToSlug();
            var companies = _users.ContactCompanies;
            var company = _users.DefaultContactCompanyID;
            var contact = _users.ContactID;
            var now = DateTime.UtcNow;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                bool isNew;
                var id = CheckNodePrivileges(d, m.GraphName, m.GraphDataID, companies, company, ActionPermission.Update, out isNew);
                if (!id.HasValue)
                    return false;
                if (!isNew && id != m.GraphDataID)
                {
                    m.IsDuplicate = true;
                    return false;
                    //throw new AuthorityException(string.Format("Could not create a duplicate article. Contact: {0} Article: {1} & {2}", contact, m.GraphName));
                }
                if (isNew && string.IsNullOrWhiteSpace(m.GraphName))
                    return false; //Can't create blank wiki
                GraphData g;
                Guid? creatorContact, creatorCompany;
                _users.GetCreator(contact, company, out creatorContact, out creatorCompany); 
                if (isNew)
                {
                    g = new GraphData
                    {
                        GraphDataID = m.GraphDataID.Value,
                        GraphName = m.GraphName,
                        VersionOwnerContactID = creatorContact,
                        VersionOwnerCompanyID = creatorCompany,
                        VersionAntecedentID = m.GraphDataID.Value,
                        Created = now,
                        CreatedBy = contact
                    };
                    d.GraphData.AddObject(g);
                }
                else
                {
                    g = (from o in d.GraphData where o.GraphDataID == id select o).Single();
                }
                g.GraphContent = m.GraphData;
                ProcessNode(m, d, ref g, creatorContact, creatorCompany, contact, now);
                g.VersionUpdated = now;
                g.VersionUpdatedBy = contact;
                d.SaveChanges();
                return true;
            }

        }

        public bool GetDuplicateNode(string wikiName, Guid? id)
        {
            wikiName = wikiName.ToSlug();
            var company = _users.DefaultContactCompanyID;
            var contact = _users.ContactID;
            Guid? creatorContact, creatorCompany;
            _users.GetCreator(contact, company, out creatorContact, out creatorCompany);
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                if (id.HasValue && d.GraphData.Where(f=>f.GraphDataID==id.Value && f.GraphName == wikiName).Any())
                    return false;
                if (!creatorCompany.HasValue)
                    return d.GraphData.Any(f => (f.GraphName == wikiName && (f.VersionOwnerCompanyID == company || f.VersionOwnerCompanyID == null)));
                else
                    return d.GraphData.Any(f => (f.GraphName == wikiName && (f.VersionOwnerCompanyID == company || f.VersionOwnerCompanyID == creatorCompany))); 

            }
        }

        public bool GetDuplicateWorkflow(string workflowName, Guid? id = default(Guid?))
        {
            workflowName = workflowName.ToSlug();
            var company = _users.DefaultContactCompanyID;
            var contact = _users.ContactID;
            Guid? creatorContact, creatorCompany;
            _users.GetCreator(contact, company, out creatorContact, out creatorCompany);
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                if (id.HasValue && d.GraphDataGroups.Where(f => f.GraphDataGroupID == id.Value && f.GraphDataGroupName == workflowName).Any())
                    return false;
                if (!creatorCompany.HasValue)
                    return d.GraphDataGroups.Any(f => (f.GraphDataGroupName == workflowName && (f.VersionOwnerCompanyID == company || f.VersionOwnerCompanyID == null)));
                else
                    return d.GraphDataGroups.Any(f => (f.GraphDataGroupName == workflowName && (f.VersionOwnerCompanyID == company || f.VersionOwnerCompanyID == creatorCompany)));
            }
        }

        public bool CheckPayment()
        {
            if (!_users.ContactID.HasValue)
                return false;
            var contactID = _users.ContactID;
            var modelID = FLOW_MODEL_ID;
            return CacheHelper.AddToCache<bool>(() =>
            {
                ICheckPayment proxy = XmlRpcProxyGen.Create<ICheckPayment>();
                proxy.Url = EXPEDIT.Share.Helpers.ConstantsHelper.APP_XMLRPC_URL;
                var response = proxy.ValidateModelContact(modelID.ToString(), contactID.ToString());
                if (string.IsNullOrWhiteSpace(response))
                    throw new System.Security.SecurityException("Could not retrieve model contact details from service.");
                dynamic result = JsonConvert.DeserializeObject<NullableExpandoObject>(response);
                bool toReturn;
                if (!bool.TryParse(string.Format("{0}", result.valid), out toReturn))
                    return false;
                return toReturn;
            }
            , string.Format(FS_FLOW_CONTACT_ID, contactID), TimeSpan.FromMinutes(30.0));
        }


        /// <summary>
        /// For now lets only support one page per name.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="nodeName"></param>
        /// <param name="permission"></param>
        /// <param name="isNew"></param>
        /// <returns></returns>
        internal Guid? CheckNodePrivileges(NKDC d, string nodeName, Guid? nodeID, Guid?[] companies, Guid? currentCompany, NKD.Models.ActionPermission permission, out bool isNew)
        {
            var table = d.GetTableName(typeof(GraphData));
            LinqModels.MinimumGraphData root;
            isNew = false;

            var possibleNodes = (from o in d.GraphData
                    where ((((!nodeID.HasValue && o.GraphName == nodeName) || (nodeID.HasValue && o.GraphDataID == nodeID.Value)))) && o.Version == 0 && o.VersionDeletedBy == null
                    select new LinqModels.MinimumGraphData
                    {
                        GraphDataID = o.GraphDataID,
                        VersionAntecedentID = o.VersionAntecedentID,
                        VersionOwnerCompanyID = o.VersionOwnerCompanyID,
                        VersionOwnerContactID = o.VersionOwnerContactID
                    }
                    ).AsEnumerable();
            var bestNode = possibleNodes.FirstOrDefault(f => f.VersionOwnerCompanyID == currentCompany);
            if (bestNode != null)
                root = bestNode;
            else
            {
                bestNode = possibleNodes.FirstOrDefault(f => companies.Contains(f.VersionOwnerCompanyID) && f.VersionOwnerCompanyID != null);
                if (bestNode != null)
                    root = bestNode;
                else
                {
                    bestNode = possibleNodes.FirstOrDefault(f=> f.VersionOwnerCompanyID != null);
                    if (bestNode != null)
                        root = bestNode;
                    else
                        root = possibleNodes.FirstOrDefault();
                }                        
            }
            
            if (root != null && permission == ActionPermission.Create)
                return null;
            var verified = false;
            Guid? id = null;
            if (root != null)
                id = root.GraphDataID;
            if (root == null)
            {
                verified = _users.CheckPermission(new SecuredBasic
                {
                    AccessorApplicationID = _users.ApplicationID,
                    AccessorContactID = _users.ContactID,
                    OwnerTableType = table
                }, ActionPermission.Create);
                if (verified)
                {
                    isNew = true;
                    id = Guid.NewGuid();
                }
            }
            else if (root.VersionOwnerCompanyID.HasValue && !root.VersionOwnerContactID.HasValue)
                verified = _users.CheckPermission(new SecuredBasic
                {
                    AccessorApplicationID = _users.ApplicationID,
                    AccessorContactID = _users.ContactID,
                    OwnerReferenceID = root.GraphDataID,
                    OwnerTableType = table
                }, permission);
            else if (root.VersionAntecedentID.HasValue)
                verified = _users.CheckPermission(new SecuredBasic
                {
                    AccessorApplicationID = _users.ApplicationID,
                    AccessorContactID = _users.ContactID,
                    OwnerReferenceID = root.VersionAntecedentID.Value,
                    OwnerTableType = table
                }, permission);
            else
                verified = _users.CheckPermission(new SecuredBasic
                {
                    AccessorApplicationID = _users.ApplicationID,
                    AccessorContactID = _users.ContactID,
                    OwnerReferenceID = root.GraphDataID,
                    OwnerTableType = table
                }, permission);
            if (!verified)
                return null;
            StatisticData stat = null;
            if (!isNew)
            {
                stat = (from o in d.StatisticDatas
                            where o.ReferenceID == id && o.TableType == table
                            && o.StatisticDataName == STAT_NAME_FLOW_ACCESS
                            select o).FirstOrDefault();
                if (stat == null)
                {
                    stat = new StatisticData { StatisticDataID = Guid.NewGuid(), TableType = table, ReferenceID = id, StatisticDataName = STAT_NAME_FLOW_ACCESS, Count = 0 };
                    d.StatisticDatas.AddObject(stat);
                }
                stat.Count++;
                d.SaveChanges();
            }
            if (((permission & ActionPermission.Create) == ActionPermission.Create)
                || ((permission & ActionPermission.Update) == ActionPermission.Update)
                || ((permission & ActionPermission.Delete) == ActionPermission.Delete))
            {
                if (_users.HasPrivateCompanyID && !CheckPayment()) //Paid users must be up to date with subscription
                    return null;
            }
            return id;
        }


        public FlowGroupViewModel GetNode(string name, Guid? nid, Guid? gid, bool includeContent = false, bool includeDisconnected = false)
        {
            name = name.ToSlug();
            var application = _users.ApplicationID;
            var contact = _users.ContactID;
            var now = DateTime.UtcNow;    
            var result = new FlowGroupViewModel { };  
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                using (var con = new SqlConnection(_users.ApplicationConnectionString))
                using (var adapter = new SqlDataAdapter("E_SP_GetNode", con))
                {
                    DataSet dataset = new DataSet();
                    var cmd = adapter.SelectCommand;
                    cmd.CommandType = CommandType.StoredProcedure;

                    var qP = cmd.CreateParameter();
                    qP.ParameterName = "@name";
                    qP.DbType = DbType.String;
                    qP.Value = name;
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

                    var qN = cmd.CreateParameter();
                    qN.ParameterName = "@id";
                    qN.DbType = DbType.Guid;
                    qN.Value = nid;
                    cmd.Parameters.Add(qN);

                    var qG = cmd.CreateParameter();
                    qG.ParameterName = "@gid";
                    qG.DbType = DbType.Guid;
                    qG.Value = gid;
                    cmd.Parameters.Add(qG);

                    var qD = cmd.CreateParameter();
                    qD.ParameterName = "@detailed";
                    qD.DbType = DbType.Boolean;
                    qD.Value = includeContent;
                    cmd.Parameters.Add(qD);

                    var qX = cmd.CreateParameter();
                    qX.ParameterName = "@disconnected";
                    qX.DbType = DbType.Boolean;
                    qX.Value = includeDisconnected;
                    cmd.Parameters.Add(qX);

                    adapter.Fill(dataset);

                    int graphs = 0, edges = 1, groups = 2, files = 3, locations = 4, experiences = 5, worktypes = 6;

                    result.Nodes =  (from o in dataset.Tables[graphs].AsEnumerable() 
                                     select new FlowViewModelDetailed {
                                         GraphDataID = o[0] as Guid?,
                                         GraphName = o[1] as string,
                                         GraphData = o[5] as string
                                     });
                    result.Edges = (from o in dataset.Tables[edges].AsEnumerable()
                                    select new FlowEdgeViewModel
                                    {
                                        GraphDataRelationID = o[0] as Guid?,
                                        GroupID = o[1] as Guid?,
                                        FromID = o[2] as Guid?,
                                        ToID = o[3] as Guid?,
                                        Weight = o[4] as decimal?,
                                        RelationTypeID = o[5] as Guid?,
                                        Related = o[6] as DateTime?,
                                        Sequence = o[7] as int?
                                    });
                    result.Workflows = (from o in dataset.Tables[groups].AsEnumerable()
                                    select new FlowEdgeWorkflowViewModel
                                    {
                                        GraphDataGroupID = o[0] as Guid?,
                                        GraphDataGroupName = o[1] as string,
                                        Comment = o[2] as string
                                    });
                    result.Files = (from o in dataset.Tables[files].AsEnumerable()
                                        select new FlowFileViewModel
                                        {
                                            GraphDataFileDataID = o[0] as Guid?,
                                            GraphDataID = o[1] as Guid?,
                                            FileDataID = o[2] as Guid?,
                                            FileName = o[3] as string
                                        });
                    result.Locations = (from o in dataset.Tables[locations].AsEnumerable()
                                    select new FlowLocationViewModel
                                    {
                                        GraphDataLocationID = o[0] as Guid?,
                                        GraphDataID = o[1] as Guid?,
                                        LocationID = o[2] as Guid?,
                                        LocationName = o[3] as string
                                    });
                    result.Contexts = (from o in dataset.Tables[experiences].AsEnumerable()
                                        select new FlowContextViewModel
                                        {
                                            GraphDataContextID = o[0] as Guid?,
                                            GraphDataID = o[1] as Guid?,
                                            ExperienceID = o[2] as Guid?,
                                            ExperienceName = o[3] as string
                                        });
                    result.WorkTypes = (from o in dataset.Tables[worktypes].AsEnumerable()
                                       select new FlowWorkTypeViewModel
                                       {
                                           WorkTypeID = o[0] as Guid?,
                                           WorkTypeName = o[3] as string
                                       });

                }
            }
            return result;
        }

        private void ProcessNode(IFlow m, NKDC d, ref GraphData g, Guid? creatorContact, Guid? creatorCompany, Guid? contact, DateTime? now = default(DateTime?))
        {
            if (!now.HasValue)
                now = DateTime.UtcNow;
            //Extract Data
            HtmlDocument doc = new HtmlDocument();
            if (!string.IsNullOrWhiteSpace(m.GraphData))
            {
                doc.LoadHtml(m.GraphData);
                var nodes = doc.DocumentNode.SelectNodes("//a");
                if (nodes != null)
                {
                    //Files
                    var hrefList = nodes
                                      .Select(p => p.GetAttributeValue("href", null))
                                      .Where(f => f != null)
                                      .ToList();
                    var files = new List<Guid>();
                    // First we see the input string.
                    foreach (var href in hrefList)
                    {
                        var match = Regex.Match(href, @"share/file/((\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1})$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                        if (match.Success)
                            files.Add(Guid.Parse(match.Groups[1].Value));
                    }
                    (from o in d.GraphDataFileDatas where !files.Contains(o.FileDataID.Value) && o.GraphDataID==m.GraphDataID.Value select o).Delete(); //Delete redundant links
                    var fd = (from o in d.GraphDataFileDatas where o.GraphDataID == m.GraphDataID select o).AsEnumerable();
                    foreach (var file in files)
                    {
                        if (!fd.Any(f => f.FileDataID == file))
                        {
                            var gdc = new GraphDataFileData
                            {
                                GraphDataFileDataID = Guid.NewGuid(),
                                GraphDataID = m.GraphDataID,
                                FileDataID = file,
                                VersionOwnerContactID = creatorContact,
                                VersionOwnerCompanyID = creatorCompany,
                                VersionAntecedentID = m.GraphDataID.Value,
                                VersionUpdated = now,
                                VersionUpdatedBy = contact
                            };
                            g.GraphDataFileData.Add(gdc);
                        }

                    }
                    //Locations
                    var locations = new List<Guid>();
                    // First we see the input string.
                    foreach (var href in hrefList)
                    {
                        var match = Regex.Match(href, @"share/location/((\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1})$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                        if (match.Success)
                            locations.Add(Guid.Parse(match.Groups[1].Value));
                    }
                    (from o in d.GraphDataLocations where !locations.Contains(o.LocationID.Value) && o.GraphDataID==m.GraphDataID.Value select o).Delete(); //Delete redundant links
                    var ld = (from o in d.GraphDataLocations where o.GraphDataID == m.GraphDataID select o).AsEnumerable();
                    foreach (var location in locations)
                    {
                        if (!ld.Any(f => f.LocationID == location))
                        {
                            var gdc = new GraphDataLocation
                            {
                                GraphDataLocationID = Guid.NewGuid(),
                                GraphDataID = g.GraphDataID,
                                LocationID = location,
                                VersionOwnerContactID = creatorContact,
                                VersionOwnerCompanyID = creatorCompany,
                                VersionAntecedentID = m.GraphDataID.Value,
                                VersionUpdated = now,
                                VersionUpdatedBy = contact
                            };
                            g.GraphDataLocation.Add(gdc);
                        }

                    }

                    //TODO: experience
                }
                else
                {
                    (from o in d.GraphDataLocations where o.GraphDataID == m.GraphDataID select o).Delete();
                    (from o in d.GraphDataFileDatas where o.GraphDataID == m.GraphDataID select o).Delete();
                    (from o in d.GraphDataContexts where o.GraphDataID == m.GraphDataID select o).Delete();
                }
            }
            else
            {
                (from o in d.GraphDataLocations where o.GraphDataID == m.GraphDataID select o).Delete();
                (from o in d.GraphDataFileDatas where o.GraphDataID == m.GraphDataID select o).Delete();
                (from o in d.GraphDataContexts where o.GraphDataID == m.GraphDataID select o).Delete();
            }
        }
        
        public bool CreateNode(FlowViewModel m)
        {
            m.GraphName = m.GraphName.ToSlug();
            if (!m.GraphDataID.HasValue)
                return false;
            var company = _users.DefaultContactCompanyID;
            var companies = _users.ContactCompanies;
            var contact = _users.ContactID;
            var now = DateTime.UtcNow;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                bool isNew;
                var id = CheckNodePrivileges(d, m.GraphName, m.GraphDataID, companies, company, ActionPermission.Create, out isNew);
                if (!isNew)
                    return UpdateNode(m);
                if (!id.HasValue)
                    return false;
                Guid? creatorContact, creatorCompany;
                _users.GetCreator(contact, company, out creatorContact, out creatorCompany);
                GraphData g = new GraphData
                    {
                        GraphDataID = m.GraphDataID.Value,
                        GraphName = m.GraphName,
                        VersionOwnerContactID = creatorContact,
                        VersionOwnerCompanyID = creatorCompany,
                        VersionAntecedentID = m.GraphDataID.Value,
                        Created = now,
                        CreatedBy = contact,
                        GraphContent = m.GraphData,
                        VersionUpdated = now,
                        VersionUpdatedBy = contact,
                    };
                d.GraphData.AddObject(g);
                ProcessNode(m, d, ref g, creatorContact, creatorCompany, contact, now);
                d.SaveChanges();
                return true;
            }
        }

        public bool UpdateNode(FlowViewModel m)
        {
            m.GraphName = m.GraphName.ToSlug();
            if (!m.GraphDataID.HasValue)
                return false;
            var company = _users.DefaultContactCompanyID;
            var companies = _users.ContactCompanies;
            var contact = _users.ContactID;
            Guid? creatorContact, creatorCompany;
            _users.GetCreator(contact, company, out creatorContact, out creatorCompany);
            var now = DateTime.UtcNow;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                bool isNew;
                var id = CheckNodePrivileges(d, m.GraphName, m.GraphDataID, companies, company, ActionPermission.Update, out isNew);
                if (!id.HasValue || isNew || id != m.GraphDataID)
                    return false;
                var g = (from o in d.GraphData where o.GraphDataID == m.GraphDataID select o).Single();
                if (!string.IsNullOrWhiteSpace(m.GraphName) && m.GraphName != g.GraphName)
                {
                    g.GraphName = m.GraphName;
                }
                if (!string.IsNullOrWhiteSpace(m.GraphData) && m.GraphData != g.GraphContent)
                {
                    g.GraphContent = m.GraphData;
                }
                ProcessNode(m, d, ref g, creatorContact, creatorCompany, contact, now);
                g.VersionUpdated = now;
                g.VersionUpdatedBy = contact;                
                d.SaveChanges();
                return true;
            }
        }
        
        public bool DeleteNode(Guid mid)
        {
            var company = _users.DefaultContactCompanyID;
            var companies = _users.ContactCompanies;
            var contact = _users.ContactID;
            var now = DateTime.UtcNow;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                bool isNew;
                var id = CheckNodePrivileges(d, null, mid, companies, company, ActionPermission.Delete, out isNew);
                if (!id.HasValue || isNew || id != mid)
                    return false;
                var g = (from o in d.GraphData where o.GraphDataID == mid select o).Single();
                d.GraphDataRelation.Where(f => f.FromGraphDataID == mid || f.ToGraphDataID == mid).Delete();
                d.GraphDataContexts.Where(f => f.GraphDataID == mid).Delete();
                d.GraphData.DeleteObject(g);
                d.SaveChanges();
                return true;
            }
        }

        public bool CreateEdge(FlowEdgeViewModel m)
        {            
            if (!m.GraphDataRelationID.HasValue || !m.FromID.HasValue || !m.ToID.HasValue || !m.GroupID.HasValue)
                return false;
            var company = _users.DefaultContactCompanyID;
            var companies = _users.ContactCompanies;
            var contact = _users.ContactID;
            var now = DateTime.UtcNow;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                bool isNew;
                var id = CheckNodePrivileges(d, null, m.FromID, companies, company, ActionPermission.Update, out isNew);
                if (id == null || isNew)
                    return false;
                id = CheckNodePrivileges(d, null, m.ToID, companies, company, ActionPermission.Update, out isNew);
                if (id == null || isNew)
                    return false;
                if ((from o in d.GraphDataRelation where (o.FromGraphDataID == m.FromID && o.ToGraphDataID == m.ToID && o.GraphDataGroupID == m.GroupID) || o.GraphDataRelationID == m.GraphDataRelationID select o).Any())
                    return true;
                Guid? creatorContact, creatorCompany;
                _users.GetCreator(contact, company, out creatorContact, out creatorCompany);
                var g = new GraphDataRelation
                {
                    GraphDataRelationID = m.GraphDataRelationID.Value,
                    FromGraphDataID = m.FromID,
                    ToGraphDataID = m.ToID,
                    GraphDataGroupID = m.GroupID,
                    Weight = m.Weight,
                    RelationTypeID = m.RelationTypeID,
                    Related = now,
                    Sequence = m.Sequence,
                    VersionUpdated = now,
                    VersionUpdatedBy = contact,
                    VersionOwnerCompanyID = creatorCompany,
                    VersionOwnerContactID = creatorContact
                };
                d.GraphDataRelation.AddObject(g);
                d.SaveChanges();
                return true;
            }
        }

        public bool DeleteEdge(Guid mid)
        {
            var company = _users.DefaultContactCompanyID;
            var companies = _users.ContactCompanies;
            var contact = _users.ContactID;
            var now = DateTime.UtcNow;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                bool isNew;
                var edge = (from o in d.GraphDataRelation where o.GraphDataRelationID == mid select o).SingleOrDefault();
                if (edge == null)
                    return true;
                var id = CheckNodePrivileges(d, null, edge.FromGraphDataID, companies, company, ActionPermission.Delete, out isNew);
                if (id == null || isNew)
                    return false;
                id = CheckNodePrivileges(d, null, edge.ToGraphDataID, companies, company, ActionPermission.Delete, out isNew);
                if (id == null || isNew)
                    return false;
                d.GraphDataRelation.DeleteObject(edge);
                d.SaveChanges();
                return true;
            }
        }

        public ContactViewModel GetMyInfo()
        {
            return _users.GetMyInfo();
        }

        public FlowEdgeWorkflowViewModel GetWorkflow(Guid id)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                var verified = _users.CheckPermission(new SecuredBasic
                {
                    AccessorApplicationID = _users.ApplicationID,
                    AccessorContactID = _users.ContactID,
                    OwnerReferenceID = id,
                    OwnerTableType = d.GetTableName(typeof(GraphDataGroup))
                }, ActionPermission.Read);
                if (!verified)
                    return null;
                var firstNodes = (from o in d.GraphDataRelation where o.GraphDataGroupID == id orderby o.Sequence ascending, o.VersionUpdated descending select new { o.ToGraphDataID, o.FromGraphDataID }).FirstOrDefault();
                Guid? firstNode = default(Guid?);
                if (firstNodes != null)
                {
                    if (firstNodes.ToGraphDataID.HasValue)
                        firstNode = firstNodes.ToGraphDataID;
                    else
                        firstNode = firstNodes.FromGraphDataID;
                }
                return (from o in d.GraphDataGroups
                        where o.GraphDataGroupID == id
                        select new FlowEdgeWorkflowViewModel
                            {
                                GraphDataGroupID = o.GraphDataGroupID,
                                GraphDataGroupName = o.GraphDataGroupName,
                                firstNode = firstNode,
                                Comment = o.Comment                                
                            }).FirstOrDefault();
            }
        }


        public bool CreateWorkflow(FlowEdgeWorkflowViewModel m)
        {
            if (!m.GraphDataGroupID.HasValue)
                return false;
            var company = _users.DefaultContactCompanyID;
            var companies = _users.ContactCompanies;
            var contact = _users.ContactID;
            var now = DateTime.UtcNow;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                if (d.GraphDataGroups.Any(f => f.GraphDataGroupID == m.GraphDataGroupID))
                    return UpdateWorkflow(m);
                var table = d.GetTableName(typeof(GraphDataGroup));
                var verified = _users.CheckPermission(new SecuredBasic
                {
                    AccessorApplicationID = _users.ApplicationID,
                    AccessorContactID = _users.ContactID,
                    OwnerTableType = table
                }, ActionPermission.Create);
                if (_users.HasPrivateCompanyID && !CheckPayment())
                {
                    return false;
                }
                Guid? creatorContact, creatorCompany;
                _users.GetCreator(contact, company, out creatorContact, out creatorCompany);
                var g = new GraphDataGroup
                {
                    GraphDataGroupID = m.GraphDataGroupID.Value,
                    GraphDataGroupName = m.GraphDataGroupName,
                    Comment = m.Comment,
                    VersionUpdated = now,
                    VersionUpdatedBy = contact,
                    VersionOwnerCompanyID = creatorCompany,
                    VersionOwnerContactID = creatorContact
                };
                d.GraphDataGroups.AddObject(g);
                d.SaveChanges();
                return true;
            }
        }


        public bool UpdateWorkflow(FlowEdgeWorkflowViewModel m)
        {
            if (!m.GraphDataGroupID.HasValue)
                return false;
            var company = _users.DefaultContactCompanyID;
            var companies = _users.ContactCompanies;
            var contact = _users.ContactID;
            var now = DateTime.UtcNow;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                var obj = (from o in d.GraphDataGroups where o.GraphDataGroupID == m.GraphDataGroupID select o).SingleOrDefault();
                if (obj == null)
                    return false;
                var table = d.GetTableName(typeof(GraphDataGroup));
                var verified = _users.CheckPermission(new SecuredBasic
                {
                    AccessorApplicationID = _users.ApplicationID,
                    AccessorContactID = _users.ContactID,
                    OwnerReferenceID = m.GraphDataGroupID,
                    OwnerTableType = table
                }, ActionPermission.Update);
                if (_users.HasPrivateCompanyID && !CheckPayment())
                {
                    return false;
                }
                if (!string.IsNullOrWhiteSpace(m.GraphDataGroupName) && obj.GraphDataGroupName != m.GraphDataGroupName)
                    obj.GraphDataGroupName = m.GraphDataGroupName;
                if (!string.IsNullOrWhiteSpace(m.Comment) && obj.Comment != m.Comment)
                    obj.Comment = m.Comment;
                obj.VersionUpdated = now;
                obj.VersionUpdatedBy = contact;
                d.SaveChanges();
                return true;
            }
        }


        public IEnumerable<SearchViewModel> GetMyFiles()
        {
            var contact = _users.ContactID;
            if (contact == null)
                return new SearchViewModel[] { };
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var wwwroot = System.Web.VirtualPathUtility.ToAbsolute("~/share/file/");
                var d = new NKDC(_users.ApplicationConnectionString, null);
                var m = (from o in d.FileDatas
                         where o.VersionOwnerContactID == contact && o.VersionDeletedBy == null && o.Version == 0
                         select new SearchViewModel
                         {
                             id = o.FileDataID,
                             Title = o.FileName,
                             Description = o.Comment,
                             Updated = o.VersionUpdated,
                             ReferenceID = o.FileDataID,
                             TableType = "file"
                         });
                return m;
            }
            
        }



        public IEnumerable<SearchViewModel> GetMyNodes()
        {
            var contact = _users.ContactID;
            if (contact == null)
                return new SearchViewModel[] { };
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                var m = (from o in d.GraphData
                         where (o.VersionOwnerContactID == contact || o.CreatedBy == contact) && o.VersionDeletedBy == null && o.Version == 0
                         select new SearchViewModel
                         {
                             id = o.GraphDataID,
                             Title = o.GraphName,
                             Description = o.Comment,
                             Updated = o.VersionUpdated,
                             ReferenceID = o.GraphDataID,
                             TableType = "node"
                         });
                return m;
            }
        }


        public IEnumerable<SearchViewModel> GetMyWorkflows()
        {
            var contact = _users.ContactID;
            if (contact == null)
                return new SearchViewModel[] { };
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                var m = (from o in d.GraphDataGroups
                         where (o.VersionOwnerContactID == contact || o.CreatedBy == contact) && o.VersionDeletedBy == null && o.Version == 0
                         select new SearchViewModel
                         {
                             id = o.GraphDataGroupID,
                             Title = o.GraphDataGroupName,
                             Description = o.Comment,
                             Updated = o.VersionUpdated,
                             ReferenceID = o.GraphDataGroupID,
                             TableType = "workflow"
                         });
                return m;
            }
        }


        public IEnumerable<SecurityViewModel> GetMySecurityLists(string tt)
        {
            if (string.IsNullOrWhiteSpace(tt))
                return new SecurityViewModel[] {};
            var company = _users.DefaultContactCompanyID;
            var contact = _users.ContactID;
            var application = _users.ApplicationID;
            if (contact == null)
                return new SecurityViewModel[] { };
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                tt = tt.ToLowerInvariant();
                string table;
                switch (tt)
                {
                    case "node":
                        table = d.GetTableName(typeof(GraphData));
                        break;
                    case "file":
                        table = d.GetTableName(typeof(FileData));
                        break;
                    case "workflow":
                        table = d.GetTableName(typeof(GraphDataGroup));
                        break;
                    default:
                        return new SecurityViewModel[] { };

                }
                                
                var m = (from o in d.E_SP_GetSecurityList(application, contact, null, table)                         
                         select new SecurityViewModel
                         {
                             SecurityTypeID = o.SecurityTypeID,
                             id = o.SecurityID,
                             Updated = o.VersionUpdated,
                             OwnerReferenceID = o.OwnerReferenceID,
                             ReferenceName = o.ReferenceName,
                             AccessorCompanyID = o.AccessorCompanyID,
                             AccessorCompanyName = o.AccessorCompanyName,
                             AccessorContactID = o.AccessorContactID,
                             AccessorContactName = o.AccessorContactName,
                             AccessorRoleID = o.AccessorRoleID,
                             AccessorRoleName = o.AccessorRoleName,
                             AccessorProjectID = o.AccessorProjectID,
                             AccessorProjectName = o.AccessorProjectName,
                             CanCreate = o.CanCreate,
                             CanRead = o.CanRead,
                             CanUpdate = o.CanUpdate,
                             CanDelete = o.CanDelete
                         });
                return m;
            }
        }



        public bool CreateSecurity(SecurityViewModel m)
        {
            if (m == null)
                return false;
            var company = _users.DefaultContactCompanyID;
            var contact = _users.ContactID;
            var application = _users.ApplicationID;
            if (contact == null)
                return false;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                var tt = m.OwnerTableType.ToLowerInvariant();
                string table;
                switch (tt)
                {
                    case "node":
                        table = d.GetTableName(typeof(GraphData));
                        break;
                    case "file":
                        table = d.GetTableName(typeof(FileData));
                        break;
                    case "workflow":
                        table = d.GetTableName(typeof(GraphDataGroup));
                        break;
                    default:
                        return false;
                }
                if (m.AccessorUserID.HasValue && !m.AccessorContactID.HasValue)
                    m.AccessorContactID = (from o in d.Contacts where o.AspNetUserID == m.AccessorUserID && o.Version == 0 && o.VersionDeletedBy == null select o.ContactID).Single();

                if (m.SecurityTypeID.HasValue && m.SecurityTypeID == (uint)SecurityType.WhiteList)
                {
                    var delete = (from o in d.SecurityWhitelists where 
                        o.OwnerApplicationID == application &&
                        o.OwnerContactID == contact &&
                        o.OwnerTableType == table &&
                        o.OwnerReferenceID == m.ReferenceID.Value &&
                        o.AccessorApplicationID == application &&
                        (!m.AccessorCompanyID.HasValue || o.AccessorCompanyID == m.AccessorCompanyID) &&
                        (!m.AccessorProjectID.HasValue || o.AccessorProjectID == m.AccessorProjectID) &&
                        (!m.AccessorContactID.HasValue || o.AccessorContactID == m.AccessorContactID) &&
                        (!m.AccessorRoleID.HasValue || o.AccessorRoleID == m.AccessorRoleID)          
                        select o).ToArray(); //Delete duplicates
                    foreach (var del in delete)
                        d.DeleteObject(del);
                    var sec = new SecurityWhitelist 
                    {
                        SecurityWhitelistID = m.SecurityID.Value,
                        OwnerApplicationID = application,
                        OwnerContactID = contact,
                        OwnerTableType = table,
                        OwnerReferenceID = m.ReferenceID.Value,
                        AccessorApplicationID = application,
                        AccessorCompanyID = m.AccessorCompanyID,
                        AccessorProjectID = m.AccessorProjectID,
                        AccessorContactID = m.AccessorContactID,
                        AccessorRoleID = m.AccessorRoleID,
                        CanCreate = m.CanCreate.HasValue ? m.CanCreate.Value : true,
                        CanRead = m.CanRead.HasValue ? m.CanRead.Value : true,
                        CanUpdate = m.CanUpdate.HasValue ? m.CanUpdate.Value : true,
                        CanDelete = m.CanDelete.HasValue ? m.CanDelete.Value : true,
                        VersionUpdated = DateTime.UtcNow,
                        VersionOwnerCompanyID = company,
                        VersionOwnerContactID = contact,
                        VersionUpdatedBy = contact,
                        VersionAntecedentID = m.SecurityID.Value

                    };
                    d.SecurityWhitelists.AddObject(sec);
                    d.SaveChanges();
                    return true;
                }
                else
                {
                    var delete = (from o in d.SecurityBlacklists
                                  where
                                      o.OwnerApplicationID == application &&
                                      o.OwnerContactID == contact &&
                                      o.OwnerTableType == table &&
                                      o.OwnerReferenceID == m.ReferenceID.Value &&
                                      o.AccessorApplicationID == application &&
                                      (!m.AccessorCompanyID.HasValue || o.AccessorCompanyID == m.AccessorCompanyID) &&
                                      (!m.AccessorProjectID.HasValue || o.AccessorProjectID == m.AccessorProjectID) &&
                                      (!m.AccessorContactID.HasValue || o.AccessorContactID == m.AccessorContactID) &&
                                      (!m.AccessorRoleID.HasValue || o.AccessorRoleID == m.AccessorRoleID)
                                  select o).ToArray(); //Delete duplicates
                    foreach (var del in delete)
                        d.DeleteObject(del);
                    var sec = new SecurityBlacklist
                    {
                        SecurityBlacklistID = m.SecurityID.Value,
                        OwnerApplicationID = application,
                        OwnerContactID = contact,
                        OwnerTableType = table,
                        OwnerReferenceID = m.ReferenceID.Value,
                        AccessorApplicationID = application,
                        AccessorCompanyID = m.AccessorCompanyID,
                        AccessorProjectID = m.AccessorProjectID,
                        AccessorContactID = m.AccessorContactID,
                        AccessorRoleID = m.AccessorRoleID,
                        CanCreate = m.CanCreate.HasValue ? m.CanCreate.Value : false,
                        CanRead = m.CanRead.HasValue ? m.CanRead.Value : false,
                        CanUpdate = m.CanUpdate.HasValue ? m.CanUpdate.Value : false,
                        CanDelete = m.CanDelete.HasValue ? m.CanDelete.Value : false,
                        VersionUpdated = DateTime.UtcNow,
                        VersionOwnerCompanyID = company,
                        VersionOwnerContactID = contact,
                        VersionUpdatedBy = contact,
                        VersionAntecedentID = m.SecurityID.Value

                    };
                    d.SecurityBlacklists.AddObject(sec);
                    d.SaveChanges();
                    return true;
                }

            
            }
        }


        public bool DeleteSecurity(Guid sid, int? securityTypeID)
        {
            var contact = _users.ContactID;
            var application = _users.ApplicationID;
            if (contact == null)
                return false;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);            
                if (!securityTypeID.HasValue || securityTypeID == (int)SecurityType.WhiteList)
                {
                    var delete = (from o in d.SecurityWhitelists
                                  where
                                      o.SecurityWhitelistID == sid &&
                                      o.OwnerApplicationID == application &&
                                      o.OwnerContactID == contact                                   
                                  select o).ToArray();
                    foreach (var del in delete)
                        d.DeleteObject(del);                 
                    d.SaveChanges();
                    return true;
                }
                if (!securityTypeID.HasValue || securityTypeID == (int)SecurityType.BlackList)
                {
                    var delete = (from o in d.SecurityBlacklists
                                  where
                                      o.SecurityBlacklistID == sid &&
                                      o.OwnerApplicationID == application &&
                                      o.OwnerContactID == contact 
                                  select o).ToArray();
                    foreach (var del in delete)
                        d.DeleteObject(del);
                    d.SaveChanges();
                    return true;
                }
            }
            return false;
        }

        public bool AssignLicense(Guid userid, Guid licenseid)
        {
            var contact = _users.ContactID;
            var application = _users.ApplicationID;
            if (contact == null)
                return false;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                var license = (from o in d.E_SP_GetLicenses(null,null,null,null,null,null,licenseid,null,null,null,null)
                               select o).FirstOrDefault();
                if (license == null)
                    return false;
                if (license.ContactID != contact)
                    return false;
                var c = (from o in d.Contacts where o.AspNetUserID == userid && o.Version == 0 && o.VersionDeletedBy == null select new { o.ContactID, o.Username }).FirstOrDefault();
                if (c == null)
                    return false;
                license.LicenseeGUID = c.ContactID;
                license.LicenseeUsername =c.Username;
                d.SaveChanges();
            }
            return true;
        }


        public IEnumerable<EXPEDIT.Flow.ViewModels.LicenseViewModel> GetMyLicenses()
        {
            var contact = _users.ContactID;
            var application = _users.ApplicationID;
            if (contact == null)
                return new EXPEDIT.Flow.ViewModels.LicenseViewModel[] {};
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                return (from o in d.E_SP_GetLicenses(application, contact, null, null, null, null, null, null, null, null, 9999999)
                        select new EXPEDIT.Flow.ViewModels.LicenseViewModel
                        {
                            LicenseID = o.LicenseID
                        }).AsEnumerable();
            }
        }

        public void Creating(UserContext context) { }

        public void Created(UserContext context)  { }

        public void LoggedIn(IUser user)
        {
            CacheHelper.Cache.Remove(string.Format(FS_FLOW_CONTACT_ID, _users.ContactID));
            if (_users.ContactID.HasValue && CheckPayment() && !_users.HasPrivateCompanyID)
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                var cid = Guid.NewGuid();
                var now = DateTime.UtcNow;
                var c = new Company
                {
                    CompanyID = cid,
                    CompanyName = cid.ToString(),
                    PrimaryContactID = _users.ContactID,
                    VersionUpdated = now,
                    Comment = "Generated Private Company"
                };
                d.Companies.AddObject(c);
                var e = new Experience
                {
                    ExperienceID = Guid.NewGuid(),
                    ExperienceName = "Private Company - " + _users.Username,
                    CompanyID = cid,
                    ContactID = _users.ContactID,
                    DateStart = now,
                    IsApproved = true,
                    VersionUpdated = now
                };
                d.Experiences.AddObject(e);
                d.SaveChanges();
            }
        }

        public void LoggedOut(IUser user) { }

        public void AccessDenied(IUser user) { }

        public void ChangedPassword(IUser user) { }

        public void SentChallengeEmail(IUser user) { }

        public void ConfirmedEmail(IUser user) { }

        public void Approved(IUser user) { }

       
    }
}
