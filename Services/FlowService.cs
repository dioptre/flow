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

using System.Net.Http;
using System.Globalization;



namespace EXPEDIT.Flow.Services {

    [UsedImplicitly]
    public class FlowService : IFlowService, Orchard.Users.Events.IUserEventHandler
    {

        public const string STAT_NAME_FLOW_ACCESS = "FlowAccess";
        public static Guid FLOW_MODEL_ID = new Guid("1DB0B648-D8A7-4FB9-8F3F-B2846822258C");
        public const string FS_FLOW_CONTACT_ID = "{0}:ValidFlowUser";

        private readonly IAutomationService _automation;
        private readonly IUsersService _users;
        private readonly IOrchardServices _services;
        public ILogger Logger { get; set; }

        public FlowService(
            IOrchardServices orchardServices,
            IUsersService users,
            IAutomationService automation
            )
        {
            _users = users;
            _services = orchardServices;
            _automation = automation;
            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
        }

        public Localizer T { get; set; }


        public IEnumerable<SearchViewModel> Search(string query, int? start = 0, int? pageSize = 20, SearchType? st = SearchType.Flow, DateTime? dateFrom = default(DateTime?), DateTime? dateUntil = default(DateTime?), string viewport = null)
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
                    case SearchType.FlowGroup:
                        table = d.GetTableName(typeof(GraphDataGroup));
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
                    m = new WikiViewModel { GraphDataID = id.Value, IsNew = true, GraphName = wikiName };
                else
                {
                    m = (from o in d.GraphData
                         where o.GraphDataID == id
                         select
                             new WikiViewModel
                             {
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
                if (id.HasValue && d.GraphData.Where(f => f.GraphDataID == id.Value && f.GraphName == wikiName).Any())
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
            if (_services.Authorizer.Authorize(StandardPermissions.SiteOwner))
                return true;
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
                    bestNode = possibleNodes.FirstOrDefault(f => f.VersionOwnerCompanyID != null);
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


        public FlowGroupViewModel GetNode(string name, Guid? nid, Guid? gid, bool includeContent = false, bool includeDisconnected = false, bool monitor = true)
        {
            name = name.ToSlug();
            var application = _users.ApplicationID;
            var contact = _users.ContactID;
            var now = DateTime.UtcNow;
            var result = new FlowGroupViewModel { };
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                if (contact.HasValue && monitor)
                {
                    try
                    {
                        var d = new NKDC(_users.ApplicationConnectionString, null);
                        var history = new GraphDataHistory
                        {
                            ContactID = contact,
                            Opened = now,
                            Session = string.Format("{0}", HttpContext.Current.Request.UserHostAddress),
                            GraphDataID = nid.Value,
                            VersionUpdated = now,
                            VersionUpdatedBy = contact
                        };
                        d.GraphDataHistories.AddObject(history);
                        d.SaveChanges();
                    }
                    catch { }
                }          


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

                    if (dataset.Tables.Count < 1)
                        return result;

                    result.Nodes = (from o in dataset.Tables[graphs].AsEnumerable()
                                    select new FlowViewModelDetailed
                                    {
                                        GraphDataID = o[0] as Guid?,
                                        GraphName = o[1] as string,
                                        GraphData = o[5] as string,
                                        VersionUpdated = o[18] as DateTime?                                        
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
                                        Sequence = o[7] as int?,
                                        EdgeConditionsText = o[17] as string
                                    });
                    result.Workflows = (from o in dataset.Tables[groups].AsEnumerable()
                                        select new FlowEdgeWorkflowViewModel
                                        {
                                            GraphDataGroupID = o[0] as Guid?,
                                            GraphDataGroupName = o[1] as string,
                                            StartGraphDataID = o[2] as Guid?
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
                    if (files.Count > 0)
                        (from o in d.GraphDataFileDatas where !files.Contains(o.FileDataID.Value) && o.GraphDataID == m.GraphDataID.Value select o).Delete(); //Delete redundant links
                    else
                        (from o in d.GraphDataFileDatas where o.GraphDataID == m.GraphDataID.Value select o).Delete(); //Delete redundant links
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
                    (from o in d.GraphDataLocations where !locations.Contains(o.LocationID.Value) && o.GraphDataID == m.GraphDataID.Value select o).Delete(); //Delete redundant links
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
                    //(from o in d.GraphDataRelation where o.FromGraphDataID == m.GraphDataID || o.ToGraphDataID == m.GraphDataID select o).Delete();
                    (from o in d.GraphDataLocations where o.GraphDataID == m.GraphDataID select o).Delete();
                    (from o in d.GraphDataFileDatas where o.GraphDataID == m.GraphDataID select o).Delete();
                    //(from o in d.GraphDataHistories where o.GraphDataID == m.GraphDataID select o).Delete();
                    //(from o in d.GraphDataTriggers where o.GraphDataID == m.GraphDataID select o).Delete();
                    //(from o in d.ProjectPlanTaskResponses where o.ActualGraphDataID == m.GraphDataID select o).Delete();
                    //(from o in d.Tasks where o.GraphDataID == m.GraphDataID select o).Delete();
                }

                //Forms
                var forms = doc.DocumentNode.SelectNodes("//*[contains(@class,'tiny-form')]");
                if (forms != null)
                {
                    var json = forms.Select(p => p.GetAttributeValue("data-json", null))
                        .Where(f => f != null)
                        .ToList();
                    foreach (var js in json)
                    {
                        dynamic form = Newtonsoft.Json.Linq.JObject.Parse(HttpUtility.HtmlDecode(js));
                        string recipients = null;
                        if (form.emails != null)
                            recipients = form.emails.Value;
                        Guid formID = Guid.Parse(form.id.Value);
                        string formStructure = JsonConvert.SerializeObject(form.fields);
                        string formName = null;
                        if (form.heading != null)
                            formName = form.heading.Value;
                        string formHash = null;
                        if (!string.IsNullOrWhiteSpace(formStructure))
                            formHash = formStructure.ComputeHash();
                        else
                            formHash = null;
                        Form theForm = d.Forms.SingleOrDefault(f => f.FormID == formID);
                        if (theForm == null)
                        {
                            theForm = new Form
                            {
                                FormID = formID,
                                FormName = formName,
                                FormActions = recipients,
                                FormStructure = formStructure,
                                FormStructureChecksum = formHash,
                                VersionOwnerContactID = creatorContact,
                                VersionOwnerCompanyID = creatorCompany,
                                VersionUpdated = DateTime.UtcNow,
                                VersionUpdatedBy = contact
                            };
                            d.Forms.AddObject(theForm);
                        }
                        else
                        {
                            if (formHash != theForm.FormStructureChecksum || recipients != theForm.FormActions || formName != theForm.FormName)
                            {
                                theForm.FormName = formName;
                                theForm.FormStructureChecksum = formHash;
                                theForm.FormStructure = formStructure;
                                theForm.FormActions = recipients;
                                theForm.VersionUpdated = DateTime.UtcNow;
                                theForm.VersionUpdatedBy = contact;
                            }
                        }
                    }
                }

                //ProjectDataTemplate
                var pdts = doc.DocumentNode.SelectNodes("//*[contains(@class,'tiny-form')]");
                if (pdts != null)
                {
                    var json = pdts.Select(p => p.GetAttributeValue("data-json", null))
                        .Where(f => f != null)
                        .ToList();
                    foreach (var js in json)
                    {
                        dynamic form = Newtonsoft.Json.Linq.JObject.Parse(HttpUtility.HtmlDecode(js));                       
                        Guid formID = Guid.Parse(form.id.Value);
                        if (form.fields.Type == Newtonsoft.Json.Linq.JTokenType.Array)
                        {
                            foreach (dynamic pdt in form.fields)
                            {
                                string pdtStructure = JsonConvert.SerializeObject(pdt);
                                Guid pdtID = Guid.Parse(pdt.uid.Value);
                                string pdtName = null;
                                if (pdt.label != null)
                                    pdtName = pdt.label.Value;
                                string pdtHash = null;
                                if (!string.IsNullOrWhiteSpace(pdtStructure))
                                    pdtHash = pdtStructure.ComputeHash();
                                else
                                    pdtHash = null;
                                ProjectDataTemplate template = d.ProjectDataTemplates.SingleOrDefault(f => f.ProjectDataTemplateID == pdtID);
                                if (template == null)
                                {
                                    template = new ProjectDataTemplate
                                    {
                                        ProjectDataTemplateID = pdtID,
                                        FormID = formID,
                                        CommonName = pdtName,
                                        TemplateStructure = pdtStructure,
                                        TemplateStructureChecksum = pdtHash,
                                        TableType = d.GetTableName(typeof(GraphData)),
                                        ReferenceID = m.GraphDataID,
                                        VersionOwnerContactID = creatorContact,
                                        VersionOwnerCompanyID = creatorCompany,
                                        VersionUpdated = DateTime.UtcNow,
                                        VersionUpdatedBy = contact
                                    };
                                    d.ProjectDataTemplates.AddObject(template);
                                }
                                else
                                {
                                    if (pdtHash != template.TemplateStructureChecksum || pdtName != template.CommonName)
                                    {
                                        template.CommonName = pdtName;
                                        template.TemplateStructureChecksum = pdtHash;
                                        template.TemplateStructure = pdtStructure;
                                        template.VersionUpdated = DateTime.UtcNow;
                                        template.VersionUpdatedBy = contact;
                                    }
                                }
                            }
                        }
                        
                    }
                }
            }
            else
            {
                //(from o in d.GraphDataRelation where o.FromGraphDataID == m.GraphDataID || o.ToGraphDataID == m.GraphDataID select o).Delete();
                (from o in d.GraphDataLocations where o.GraphDataID == m.GraphDataID select o).Delete();
                (from o in d.GraphDataFileDatas where o.GraphDataID == m.GraphDataID select o).Delete();
                //(from o in d.GraphDataHistories where o.GraphDataID == m.GraphDataID select o).Delete();
                //(from o in d.GraphDataTriggers where o.GraphDataID == m.GraphDataID select o).Delete();
                //(from o in d.ProjectPlanTaskResponses where o.ActualGraphDataID == m.GraphDataID select o).Delete();
                //(from o in d.Tasks where o.GraphDataID == m.GraphDataID select o).Delete();
            }

            //Default Group
            if (m.workflows != null)
            {
                foreach (var gid in m.workflows)
                {
                    if (gid.HasValue)
                    {
                        var firstProcess = !(from o in d.GraphDataRelation where o.GraphDataGroupID==gid.Value select o).Any();
                        if (firstProcess)
                        {
                            var fp = new GraphDataRelation
                            {
                                FromGraphDataID = m.GraphDataID,
                                ToGraphDataID = null,
                                GraphDataRelationID = Guid.NewGuid(),
                                GraphDataGroupID = gid.Value
                            };
                            d.GraphDataRelation.AddObject(fp);
                        }
                    }
                }
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
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                bool isNew;
                var id = CheckNodePrivileges(d, null, mid, companies, company, ActionPermission.Delete, out isNew);
                if (!id.HasValue || isNew || id != mid)
                    return false;
                var g = (from o in d.GraphData where o.GraphDataID == mid select o).Single();
                d.GraphDataHistories.Where(f => f.GraphDataID == mid).Delete();
                d.GraphDataRelation.Where(f => f.FromGraphDataID == mid || f.ToGraphDataID == mid).Delete();
                d.GraphDataTriggers.Where(f => f.GraphDataID == mid).Delete();
                d.ProjectPlanTaskResponses.Where(f => f.ActualGraphDataID == mid).Delete();
                d.Tasks.Where(f => f.GraphDataID == mid).Delete();
                d.GraphDataFileDatas.Where(f => f.GraphDataID == mid).Delete();
                d.GraphDataLocations.Where(f => f.GraphDataID == mid).Delete();                            
                d.GraphData.DeleteObject(g);
                d.SaveChanges();
                return true;
            }
        }

        public bool UnlinkNode(Guid mid, Guid? gid = null)
        {
            var company = _users.DefaultContactCompanyID;
            var companies = _users.ContactCompanies;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                bool isNew;
                var id = CheckNodePrivileges(d, null, mid, companies, company, ActionPermission.Delete, out isNew);
                if (!id.HasValue || isNew || id != mid)
                    return false;
                var g = (from o in d.GraphData where o.GraphDataID == mid select o).Single();
                if (gid.HasValue)
                    d.GraphDataRelation.Where(f => (f.FromGraphDataID == mid || f.ToGraphDataID == mid) && (f.GraphDataRelationID == gid.Value)).Delete();
                else
                    d.GraphDataRelation.Where(f => (f.FromGraphDataID == mid || f.ToGraphDataID == mid)).Delete();
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
                if (d.GraphDataGroups.Any(f=>f.GraphDataGroupID == m.GroupID && f.VersionOwnerContactID.HasValue) && !_users.CheckPermission(new SecuredBasic
                {
                    AccessorApplicationID = _users.ApplicationID,
                    AccessorContactID = _users.ContactID,
                    OwnerReferenceID = m.GroupID,
                    OwnerTableType = d.GetTableName(typeof(GraphDataGroup))
                }, ActionPermission.Update))
                    return false;
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
        public bool UpdateEdge(FlowEdgeViewModel m)
        {

            try
            {
                var contact = _users.ContactID;
                var now = DateTime.Now;
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(m.id.Value, ActionPermission.Update, typeof(GraphDataRelation)))
                        return false;
                    //Update
                    var gdrc = (from o in d.GraphDataRelation where o.GraphDataRelationID == m.id && o.VersionDeletedBy == null select o).Single();
                    if (m.Weight != null && gdrc.Weight != m.Weight)
                        gdrc.Weight = m.Weight;
                    if (m.Sequence != null && gdrc.Sequence != m.Sequence)
                        gdrc.Sequence = m.Sequence;
                    if (m.RelationTypeID != null && gdrc.RelationTypeID != m.RelationTypeID)
                        gdrc.RelationTypeID = m.RelationTypeID;
                    if (gdrc.EntityState == EntityState.Modified)
                    {
                        gdrc.VersionUpdated = now;
                        gdrc.VersionUpdatedBy = contact;
                    }
                    d.SaveChanges();
                    return true;
                }

            }
            catch (Exception ex)
            {
                return false;
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
                d.GraphDataRelationConditions.Where(f => f.GraphDataRelationID == edge.GraphDataRelationID).Delete();
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
                                StartGraphDataID = o.StartGraphDataID,
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
                //if (!verified) //Allow everyone to create, commenting out verification
                //    return false;
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
                    StartGraphDataID = m.StartGraphDataID,
                    Comment = m.Comment,
                    Created = now,
                    CreatedBy = contact,
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
            var applicationCompany = _users.ApplicationCompanyID;
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
                if (!verified && obj.VersionOwnerCompanyID.HasValue && obj.VersionOwnerCompanyID != applicationCompany)
                {
                    return false;
                }
                if (_users.HasPrivateCompanyID && !CheckPayment())
                {
                    return false;
                }
                if (!string.IsNullOrWhiteSpace(m.GraphDataGroupName) && obj.GraphDataGroupName != m.GraphDataGroupName)
                    obj.GraphDataGroupName = m.GraphDataGroupName;
                if (!string.IsNullOrWhiteSpace(m.Comment) && obj.Comment != m.Comment)
                    obj.Comment = m.Comment;
                if (m.StartGraphDataID != null && obj.StartGraphDataID != m.StartGraphDataID)
                    obj.StartGraphDataID = m.StartGraphDataID;
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
                return new SecurityViewModel[] { };
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
                    var delete = (from o in d.SecurityWhitelists
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
                var license = (from o in d.Licenses where o.LicenseID == licenseid && o.Version == 0 && o.VersionDeletedBy == null
                               select o).FirstOrDefault();
                if (license == null)
                    return false;
                if (license.ContactID != contact)
                    return false;
                var c = (from o in d.Contacts where o.AspNetUserID == userid && o.Version == 0 && o.VersionDeletedBy == null select new { o.ContactID, o.Username }).FirstOrDefault();
                if (c == null)
                    return false;
                if (license.LicenseeGUID != c.ContactID)
                {
                    license.LicenseeGUID = c.ContactID;
                    license.LicenseeUsername = c.Username;
                    license.VersionUpdated = DateTime.UtcNow;
                    license.VersionUpdatedBy = contact;
                    d.SaveChanges();
                }
            }
            return true;
        }


        public IEnumerable<EXPEDIT.Flow.ViewModels.LicenseViewModel> GetMyLicenses(Guid? licenseID = default(Guid?))
        {
            var contact = _users.ContactID;
            var application = _users.ApplicationID;
            if (contact == null)
                return new EXPEDIT.Flow.ViewModels.LicenseViewModel[] { };
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                return (from o in d.E_SP_GetLicenses(application, contact, null, null, null, null, licenseID, null, null, null, 9999999)
                        select new EXPEDIT.Flow.ViewModels.LicenseViewModel
                        {
                            LicenseID = o.LicenseID,
                            CompanyID = o.CompanyID,
                            ContactID = o.ContactID,
                            LicenseeGUID = o.LicenseeGUID,
                            LicenseeName = o.LicenseeName,
                            LicenseeUsername = o.LicenseeUsername,
                            ApplicationID = o.ApplicationID,
                            ValidFrom = o.ValidFrom,
                            Expiry = o.Expiry,
                            SupportExpiry = o.SupportExpiry,
                            ValidForDuration = o.ValidForDuration,
                            ValidForUnitID = o.ValidForUnitID,
                            ValidForUnitName = o.ValidForUnitName,
                            ProRataCost = o.ProRataCost,
                            ModelID = o.ModelID,
                            ModelName = o.StandardModelName,
                            ModelRestrictions = o.ModelRestrictions,
                            ModelPartID = o.ModelPartID,
                            PartName = o.StandardPartName,
                            PartRestrictions = o.PartRestrictions,
                            AssetID = o.AssetID
                        }).AsEnumerable();
            }
        }

        public bool GetDuplicateWorkflow(Guid gid)
        {
            //var company = _users.DefaultContactCompanyID;
            //var contact = _users.ContactID;
            //var application = _users.ApplicationID;
            //if (contact == null)
            //    return false;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                return (from o in d.GraphDataGroups where o.GraphDataGroupID == gid select o).Any();
            }
        }

        public bool GetDuplicateNode(Guid gid)
        {
            //var company = _users.DefaultContactCompanyID;
            //var contact = _users.ContactID;
            //var application = _users.ApplicationID;
            //if (contact == null)
            //    return false;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                return (from o in d.GraphData where o.GraphDataID == gid select o).Any();
            }
        }

        public bool CheckPermission(Guid? gid, ActionPermission permission, Type typeToCheck)
        {
            var contact = _users.ContactID;
            if (contact == null)
                contact = Guid.NewGuid();
            var application = _users.ApplicationID;
            var d = new NKDC(_users.ApplicationConnectionString, null);
            var table = d.GetTableName(typeToCheck);
            if (gid.HasValue)
            {
                return _users.CheckPermission(new SecuredBasic
                {
                    AccessorApplicationID = application,
                    AccessorContactID = contact,
                    OwnerTableType = table,
                    OwnerReferenceID = gid.Value
                }, permission);
            }
            else
            {
                return _users.CheckPermission(new SecuredBasic
                {
                    AccessorApplicationID = application,
                    AccessorContactID = contact,
                    OwnerTableType = table
                }, permission);
            }
        }

        public bool CheckWorkflowPermission(Guid gid, ActionPermission permission)
        {
            return CheckPermission(gid, permission, typeof(GraphDataGroup));
        }

        public bool CheckNodePermission(Guid gid, ActionPermission permission)
        {
            return CheckPermission(gid, permission, typeof(GraphData));
        }   

        

        public UserProfileViewModel GetMyProfile()
        {
            var contact = _users.ContactID;
            if (contact == null)
                return null;
            var application = _users.ApplicationID;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                var r = (from o in d.Contacts
                         where o.ContactID == contact.Value && o.Version == 0 && o.VersionDeletedBy == null
                         select o).Single();
                var c = new UserProfileViewModel
                {
                    ContactID = r.ContactID,
                    ContactName = r.ContactName,
                    Title = r.Title,
                    Surname = r.Surname,
                    Firstname = r.Firstname,
                    Username = r.Username,
                    Hash = r.Hash,
                    DefaultEmail = r.DefaultEmail,
                    DefaultEmailValidated = r.DefaultEmailValidated,
                    DefaultMobile = r.DefaultMobile,
                    DefaultMobileValidated = r.DefaultMobileValidated,
                    MiddleNames = r.MiddleNames,
                    Initials = r.Initials,
                    DOB = r.DOB,
                    BirthCountryID = r.BirthCountryID,
                    BirthCity = r.BirthCity,
                    AspNetUserID = r.AspNetUserID,
                    XafUserID = r.XafUserID,
                    OAuthID = r.OAuthID,
                    Photo = r.Photo,
                    ShortBiography = r.ShortBiography
                };

                var address = (from o in d.ContactAddresses where o.ContactID == contact && o.Version == 0 && o.VersionDeletedBy == null && o.Address.Version == 0 && o.Address.VersionDeletedBy == null orderby o.VersionUpdated descending select o.Address).FirstOrDefault();
                if (address != null)
                {
                    c.AddressID = address.AddressID;
                    c.AddressTypeID = address.AddressTypeID;
                    c.AddressName = address.AddressName;
                    c.Sequence = address.Sequence;
                    c.Street = address.Street;
                    c.Extended = address.Extended;
                    c.City = address.City;
                    c.State = address.State;
                    c.Country = address.Country;
                    c.Postcode = address.Postcode;
                    c.IsHQ = address.IsHQ;
                    c.IsPostBox = address.IsPostBox;
                    c.IsBusiness = address.IsBusiness;
                    c.IsHome = address.IsHome;
                    c.Phone = address.Phone;
                    c.Fax = address.Fax;
                    c.Email = address.Email;
                    c.Mobile = address.Mobile;
                    c.LocationID = address.LocationID;

                }
                return c;
            }
        }

        public bool UpdateProfile(UserProfileViewModel m)
        {
            var contact = _users.ContactID;
            var application = _users.ApplicationID;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                //Ensure only owner can change details
                if (m.ContactID.HasValue && m.ContactID.Value != contact)
                    return false;
                var c = (from o in d.Contacts where o.Version == 0 && o.VersionDeletedBy == null && o.ContactID == contact select o).Single();
                var a = (from o in d.ContactAddresses where o.ContactID == contact && o.Version == 0 && o.VersionDeletedBy == null && o.Address.Version == 0 && o.Address.VersionDeletedBy == null orderby o.VersionUpdated descending select o.Address).FirstOrDefault();
                if (a == null)
                {
                    a = new Address
                    {
                        AddressID = Guid.NewGuid(),
                        VersionUpdated = DateTime.UtcNow
                    };
                    var ca = new ContactAddress
                    {
                        ContactAddressID = Guid.NewGuid(),
                        AddressID = a.AddressID,
                        ContactID = contact,
                        VersionUpdated = DateTime.UtcNow
                    };
                    a.ContactAddresses.Add(ca);
                    d.Addresses.AddObject(a);
                }
                if (!string.IsNullOrWhiteSpace(m.Title) && c.Title != m.Title)
                    c.Title = m.Title;
                if (!string.IsNullOrWhiteSpace(m.Firstname) && c.Firstname != m.Firstname)
                    c.Firstname = m.Firstname;
                if (!string.IsNullOrWhiteSpace(m.Surname) && c.Surname != m.Surname)
                    c.Surname = m.Surname;
                if (!string.IsNullOrWhiteSpace(m.DefaultEmail) && c.DefaultEmail != m.DefaultEmail)
                {
                    if (!_users.UpdateUserEmail(m.DefaultEmail))
                        return false;
                    c.DefaultEmail = m.DefaultEmail;
                }
                if (!string.IsNullOrWhiteSpace(m.DefaultEmail) && a.Email != m.DefaultEmail)
                    a.Email = m.DefaultEmail; //Use the same email
                if (!string.IsNullOrWhiteSpace(m.DefaultMobile) && c.DefaultMobile != m.DefaultMobile)
                    c.DefaultMobile = m.DefaultMobile;
                if (!string.IsNullOrWhiteSpace(m.DefaultMobile) && a.Mobile != m.DefaultMobile)
                    a.Mobile = m.DefaultMobile; //Use the same mobile
                if (a.AddressName != m.AddressName)
                    a.AddressName = m.AddressName; //Company Name
                if (!string.IsNullOrWhiteSpace(m.Street) && a.Street != m.Street)
                    a.Street = m.Street;
                if (a.Extended != m.Extended)
                    a.Extended = m.Extended;
                if (!string.IsNullOrWhiteSpace(m.City) && a.City != m.City)
                    a.City = m.City;
                if (!string.IsNullOrWhiteSpace(m.State) && a.State != m.State)
                    a.State = m.State;
                if (!string.IsNullOrWhiteSpace(m.Country) && a.Country != m.Country)
                    a.Country = m.Country;
                if (!string.IsNullOrWhiteSpace(m.Postcode) && a.Postcode != m.Postcode)
                    a.Postcode = m.Postcode;

                d.SaveChanges();
                return true;
            }           
        }

        public void Creating(UserContext context) { }

        public void Created(UserContext context) { }

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

        public bool GetTranslation(TranslationViewModel m)
        {
            var translateURL = @"https://www.googleapis.com/language/translate/v2";
            try
            {
                var contact = _users.ContactID;
                var application = _users.ApplicationID;
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    m.TranslationQueue = new Dictionary<Guid, Translation>();
                    switch (m.SearchType)
                    {
                        case SearchType.FlowGroup:
                            if (!CheckPermission(m.DocID.Value, ActionPermission.Read, typeof(GraphDataGroup)))
                                return false;
                            m.TableType = d.GetTableName(typeof(GraphDataGroup));
                            m.TranslationQueue = (from g in d.GraphDataGroups.Where(f => f.GraphDataGroupID == m.DocID && f.Version == 0 && f.VersionDeletedBy == null)
                                                  join t in d.TranslationData.Where(f => f.ReferenceID == m.DocID && f.TableType == m.TableType && f.TranslationCulture == m.TranslationCulture && f.Version == 0 && f.VersionDeletedBy == null)
                                                      on g.GraphDataGroupID equals t.ReferenceID
                                                      into gt
                                                  from tx in gt.DefaultIfEmpty()
                                                  select new { g.GraphDataGroupID, g.GraphDataGroupName, g.VersionUpdated, g.VersionOwnerContactID, g.VersionOwnerCompanyID, Translation = gt.FirstOrDefault() })
                                                  .ToDictionary(f => f.GraphDataGroupID, f => f.Translation == null ?
                                                      new Translation(
                                                        f.GraphDataGroupName,
                                                        f.VersionUpdated,
                                                        f.VersionOwnerContactID,
                                                        f.VersionOwnerCompanyID) :
                                                      new Translation(
                                                        f.GraphDataGroupName,
                                                        f.VersionUpdated,
                                                        f.VersionOwnerContactID,
                                                        f.VersionOwnerCompanyID,
                                                        f.Translation.TranslationDataID,
                                                        f.Translation.TranslationName,
                                                        f.Translation.Translation,
                                                        f.Translation.VersionUpdated
                                                      ));
                            break;
                        case SearchType.Flows:
                            if (!CheckPermission(m.DocID.Value, ActionPermission.Read, typeof(GraphDataGroup)))
                                return false;
                            m.TableType = d.GetTableName(typeof(GraphData));
                            m.TranslationQueue = (from g in (from g in d.GraphData
                                                  join gg in d.GraphDataRelation.Where(f=>f.GraphDataGroupID == m.DocID && f.Version == 0 && f.VersionDeletedBy == null)
                                                    on g.GraphDataID equals gg.FromGraphDataID select g).Union(
                                                     (from g in d.GraphData
                                                      join gg in d.GraphDataRelation.Where(f => f.GraphDataGroupID == m.DocID && f.Version == 0 && f.VersionDeletedBy == null)
                                                    on g.GraphDataID equals gg.ToGraphDataID select g))
                                                  join t in d.TranslationData.Where(f => f.TableType == m.TableType && f.TranslationCulture == m.TranslationCulture && f.Version == 0 && f.VersionDeletedBy == null)
                                                      on g.GraphDataID equals t.ReferenceID
                                                      into gt
                                                  from tx in gt.DefaultIfEmpty()
                                                  select new { g.GraphDataID, g.GraphName, g.VersionUpdated, g.VersionOwnerContactID, g.VersionOwnerCompanyID, Translation = gt.FirstOrDefault() })
                                                  .ToDictionary(f => f.GraphDataID, f => f.Translation == null ?
                                                      new Translation(
                                                        f.GraphName,
                                                        f.VersionUpdated,
                                                        f.VersionOwnerContactID,
                                                        f.VersionOwnerCompanyID) :
                                                      new Translation(
                                                        f.GraphName,
                                                        f.VersionUpdated,
                                                        f.VersionOwnerContactID,
                                                        f.VersionOwnerCompanyID,
                                                        f.Translation.TranslationDataID,
                                                        f.Translation.TranslationName,
                                                        f.Translation.Translation,
                                                        f.Translation.VersionUpdated
                                                      ));
                            break;
                        case SearchType.Flow:
                            m.TableType = d.GetTableName(typeof(GraphData));
                            if (m.id.HasValue)
                            {
                                if (m.Refresh && !CheckPermission(m.id.Value, ActionPermission.Update, typeof(TranslationData)))
                                    return false;
                                else if (!m.Refresh && !CheckPermission(m.id.Value, ActionPermission.Read, typeof(TranslationData)))
                                    return false;
                                m.TranslationQueue = (from t in d.TranslationData.Where(f => f.TranslationDataID == m.id && f.Version == 0 && f.VersionDeletedBy == null)
                                                      join g in d.GraphData.Where(f =>f.Version == 0 && f.VersionDeletedBy == null)                                                      
                                                          on t.ReferenceID equals g.GraphDataID
                                                      select new { g.GraphDataID, g.GraphName, g.VersionUpdated, g.VersionOwnerContactID, g.VersionOwnerCompanyID, Translation = t })
                                                      .ToDictionary(f => f.GraphDataID, f => f.Translation == null ?
                                                          new Translation(
                                                            f.GraphName,
                                                            f.VersionUpdated,
                                                            f.VersionOwnerContactID,
                                                            f.VersionOwnerCompanyID) :
                                                          new Translation(
                                                            f.GraphName,
                                                            f.VersionUpdated,
                                                            f.VersionOwnerContactID,
                                                            f.VersionOwnerCompanyID,
                                                            f.Translation.TranslationDataID,
                                                            f.Translation.TranslationName,
                                                            f.Translation.Translation,
                                                            f.Translation.VersionUpdated,
                                                            f.Translation.TranslationCulture
                                                          ));
                                if (string.IsNullOrWhiteSpace(m.TranslationCulture))
                                    m.TranslationCulture = m.TranslationQueue.Where(f => f.Value != null && !string.IsNullOrWhiteSpace(f.Value.TranslationCulture)).Select(f => f.Value.TranslationCulture).FirstOrDefault();
                                if (!m.DocID.HasValue)
                                    m.DocID = m.TranslationQueue.Select(f => f.Key).FirstOrDefault();
                            }
                            else
                            {
                                if (m.Refresh && !CheckPermission(m.id.Value, ActionPermission.Update, typeof(TranslationData)))
                                    return false;
                                if (!m.Refresh && !CheckPermission(m.DocID.Value, ActionPermission.Read, typeof(GraphData)))
                                    return false;
                                m.TranslationQueue = (from g in d.GraphData.Where(f => f.GraphDataID == m.DocID && f.Version == 0 && f.VersionDeletedBy == null)
                                                      join t in d.TranslationData.Where(f => f.ReferenceID == m.DocID && f.TableType == m.TableType && f.TranslationCulture == m.TranslationCulture && f.Version == 0 && f.VersionDeletedBy == null)
                                                          on g.GraphDataID equals t.ReferenceID
                                                          into gt
                                                      from tx in gt.DefaultIfEmpty()
                                                      select new { g.GraphDataID, g.GraphName, g.VersionUpdated, g.VersionOwnerContactID, g.VersionOwnerCompanyID, Translation = gt.FirstOrDefault() })
                                    //.Where(f => f.Translation == null || f.Translation.Translation == null)
                                                  .ToDictionary(f => f.GraphDataID, f => f.Translation == null ?
                                                      new Translation(
                                                        f.GraphName,
                                                        f.VersionUpdated,
                                                        f.VersionOwnerContactID,
                                                        f.VersionOwnerCompanyID) :
                                                      new Translation(
                                                        f.GraphName,
                                                        f.VersionUpdated,
                                                        f.VersionOwnerContactID,
                                                        f.VersionOwnerCompanyID,
                                                        f.Translation.TranslationDataID,
                                                        f.Translation.TranslationName,
                                                        f.Translation.Translation,
                                                        f.Translation.VersionUpdated
                                                      ));
                            }                            
                            break;
                        default:
                            return false;
                    }

                    if (!m.TranslationQueue.Any())
                        return false; //ref didnt exist;
                    else
                    {
                        var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
                        var lang = cultures.OrderBy(f=>f.Name).FirstOrDefault(f => f.Name.StartsWith(m.TranslationCulture));
                        using (var client = new HttpClient())
                        {
                            client.DefaultRequestHeaders.Add("X-HTTP-Method-Override", "GET");
                            foreach (var translation in m.TranslationQueue)
                            {
                                if (!m.Refresh && (translation.Value.TranslatedName != null && translation.Value.TranslatedText != null))
                                    continue;

                                switch (m.SearchType)
                                {
                                    case SearchType.FlowGroup:
                                        break;
                                    case SearchType.Flows:
                                        break;
                                    case SearchType.Flow:
                                        translation.Value.OriginalText = (from o in d.GraphData where o.GraphDataID == m.DocID && o.VersionDeletedBy == null && o.Version == 0 select o.GraphContent).FirstOrDefault();
                                        break;
                                    default:
                                        return false;
                                }
                                System.Threading.Tasks.Task<HttpResponseMessage> rName = null, rText = null;
                                Func<System.Threading.Tasks.Task<HttpResponseMessage>, System.Threading.Tasks.Task<string>> responseContent = async delegate(System.Threading.Tasks.Task<HttpResponseMessage> responseAsync)
                                {
                                    var response = responseAsync.Result;
                                    if (response.IsSuccessStatusCode)
                                    {
                                        dynamic json = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
                                        return json.data.translations[0].translatedText ?? string.Empty;
                                    }
                                    else
                                    {
                                        return null;
                                    }
                                };
                                if (m.Refresh || (string.IsNullOrWhiteSpace(translation.Value.TranslatedName) && !string.IsNullOrWhiteSpace(translation.Value.OriginalName)))
                                {
                                    client.DefaultRequestHeaders.Accept.Clear();
                                    var requestContent = new FormUrlEncodedContent(new[] { 
                                    new KeyValuePair<string, string>("key", "AIzaSyA7mP-819Mgz4dy6X0NIlQ6SjyzDn5QEJA") ,
                                    //new KeyValuePair<string, string>("source", "en") ,
                                    new KeyValuePair<string, string>("target", lang.TwoLetterISOLanguageName),
                                    new KeyValuePair<string, string>("q", translation.Value.HumanName) 
                                    });
                                    rName = client.PostAsync(translateURL, requestContent);
                                }
                                if (m.Refresh || (string.IsNullOrWhiteSpace(translation.Value.TranslatedText) && !string.IsNullOrWhiteSpace(translation.Value.OriginalText)))
                                {
                                    client.DefaultRequestHeaders.Accept.Clear();
                                    var requestContent = new FormUrlEncodedContent(new[] { 
                                    new KeyValuePair<string, string>("key", "AIzaSyA7mP-819Mgz4dy6X0NIlQ6SjyzDn5QEJA") ,
                                    //new KeyValuePair<string, string>("source", "en") ,
                                    new KeyValuePair<string, string>("target", lang.TwoLetterISOLanguageName),
                                    new KeyValuePair<string, string>("q", translation.Value.OriginalText) 
                                     });
                                    //client.DefaultRequestHeaders.Add("X-HTTP-Method-Override", "GET");
                                    rText = client.PostAsync(translateURL, requestContent);
                                }
                                if (rName != null)
                                    translation.Value.TranslatedName = responseContent(rName).Result;
                                if (rText != null)
                                    translation.Value.TranslatedText = responseContent(rText).Result;
                                if (!string.IsNullOrWhiteSpace(translation.Value.TranslatedName) || !string.IsNullOrWhiteSpace(translation.Value.TranslatedText))
                                {
                                    bool newRecord = !translation.Value.TranslationDataID.HasValue, failedCreate = false;
                                    TranslationData tx = null;
                                    if (newRecord)
                                    {
                                        try
                                        {
                                            translation.Value.TranslationDataID = Guid.NewGuid();
                                            //Insert
                                            tx = new TranslationData
                                            {
                                                TranslationDataID = translation.Value.TranslationDataID.Value,
                                                TableType = m.TableType,
                                                ReferenceID = translation.Key,
                                                ReferenceName = translation.Value.OriginalName,
                                                ReferenceUpdated = translation.Value.OriginalUpdated,
                                                OriginCulture = m.OriginCulture ?? "en-US",
                                                TranslationCulture = lang.Name, //TODO could check real culture
                                                TranslationName = translation.Value.TranslatedName,
                                                Translation = translation.Value.TranslatedText,
                                                VersionUpdated = DateTime.UtcNow,
                                                VersionOwnerContactID = translation.Value.OriginalContact,
                                                VersionOwnerCompanyID = translation.Value.OriginalCompany,
                                                VersionUpdatedBy = contact
                                            };
                                            d.TranslationData.AddObject(tx);
                                            d.SaveChanges();
                                        }
                                        catch
                                        {
                                            failedCreate = true;
                                            d.TranslationData.DeleteObject(tx);
                                        }
                                    }
                                    if (!newRecord || failedCreate)
                                    {
                                        //Update
                                        if (!failedCreate)
                                            tx = (from o in d.TranslationData where o.TranslationDataID == translation.Value.TranslationDataID && o.VersionDeletedBy == null select o).Single();
                                        else
                                            tx = (from o in d.TranslationData where 
                                                      o.TableType == m.TableType && o.ReferenceID == translation.Key && o.ReferenceName == translation.Value.OriginalName 
                                                      && o.TranslationCulture == lang.Name &&
                                                      o.Version == 0 && o.VersionDeletedBy == null select o).Single();
                                        tx.ReferenceName = translation.Value.OriginalName;
                                        tx.ReferenceUpdated = translation.Value.OriginalUpdated;
                                        tx.OriginCulture = m.OriginCulture ?? "en-US";
                                        tx.TranslationCulture = lang.Name; //TODO could check real culture
                                        tx.TranslationName = translation.Value.TranslatedName;
                                        if (rText != null)
                                            tx.Translation = translation.Value.TranslatedText;
                                        tx.VersionUpdated = DateTime.UtcNow;
                                        tx.VersionUpdatedBy = contact;
                                        d.SaveChanges();
                                    }

                                }

                            }
                           
                        }
                    }

                    m.TranslationResults = (from o in m.TranslationQueue select new TranslationViewModel { 
                        TranslationCulture = m.TranslationCulture,
                        TranslationText = o.Value.TranslatedText,
                        TranslationName = o.Value.TranslatedName,
                        id = o.Value.TranslationDataID,
                        DocID = o.Key,
                        DocType = m.TableType,
                        DocName = o.Value.OriginalName,
                        DocUpdated = o.Value.OriginalUpdated,
                        VersionUpdated = o.Value.TranslationUpdated
                    }).ToArray();

                }

            }
            catch (Exception ex)
            {
                return false;
            }

            return true;

        }

        public bool UpdateTranslation(TranslationViewModel m)
        {

            try
            {
                var contact = _users.ContactID;
                var application = _users.ApplicationID;
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    switch (m.SearchType)
                    {
                        case SearchType.FlowGroup:
                            if (!CheckPermission(m.DocID.Value, ActionPermission.Update, typeof(GraphDataGroup)))
                                return false;
                            m.TableType = d.GetTableName(typeof(GraphDataGroup));
                            break;
                        case SearchType.Flow:
                            if (!CheckPermission(m.DocID.Value, ActionPermission.Update, typeof(GraphData)))
                                return false;
                            m.TableType = d.GetTableName(typeof(GraphData));
                            break;
                        default:
                            return false;
                    }
                    //Update
                    var tx = (from o in d.TranslationData where o.TranslationDataID == m.id && o.VersionDeletedBy == null select o).Single();
                    tx.OriginCulture = m.OriginCulture ?? "en-US";
                    if (m.TranslationName != null)
                        tx.TranslationName = m.TranslationName;
                    if (m.TranslationText != null)
                        tx.Translation = m.TranslationText;
                    tx.VersionUpdated = DateTime.UtcNow;
                    tx.VersionUpdatedBy = contact;
                    d.SaveChanges();
                    return true;
                }                       

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool DeleteTranslation(TranslationViewModel m)
        {

            try
            {
                var contact = _users.ContactID;
                var application = _users.ApplicationID;
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    switch (m.SearchType)
                    {
                        case SearchType.FlowGroup:
                            if (!CheckPermission(m.id.Value, ActionPermission.Delete, typeof(TranslationData)))
                                return false;
                            break;
                        case SearchType.Flow:
                            if (!CheckPermission(m.id.Value, ActionPermission.Delete, typeof(TranslationData)))
                                return false;
                            break;
                        default:
                            return false;
                    }
                    //Delete
                    var tx = (from o in d.TranslationData where o.TranslationDataID == m.id && o.VersionDeletedBy == null select o).Single();
                    d.TranslationData.DeleteObject(tx);                    
                    d.SaveChanges();
                    return true;
                }

            }
            catch (Exception ex)
            {
                return false;
            }
        }


        public bool CreateLocale(LocaleViewModel m)
        {
            var contact = _users.ContactID;
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    var tx = new TranslationData
                    {
                        TranslationDataID = m.TranslationDataID.Value,
                        TableType = LocaleViewModel.DocType,
                        //ReferenceID = translation.Key,
                        ReferenceName = m.Label,
                        //ReferenceUpdated = translation.Value.OriginalUpdated,
                        OriginCulture = m.OriginalCulture ?? "en-US",
                        TranslationCulture = m.TranslationCulture, //TODO could check real culture
                        TranslationName = m.OriginalText,
                        Translation = m.Translation,
                        VersionUpdated = DateTime.UtcNow,
                        VersionUpdatedBy = contact
                    };
                    d.TranslationData.AddObject(tx);
                    d.SaveChanges();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool GetLocale(LocaleViewModel m)
        {
            var translateURL = @"https://www.googleapis.com/language/translate/v2";
            try
            {
                var contact = _users.ContactID;
                var application = _users.ApplicationID;
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    m.LocaleQueue = (from o in d.TranslationData.Where(f=>f.TableType == LocaleViewModel.DocType && f.TranslationCulture == m.OriginalCulture && f.Version == 0 && f.VersionDeletedBy == null)
                                     join t in d.TranslationData.Where(f =>f.TableType == LocaleViewModel.DocType && f.TranslationCulture == m.TranslationCulture && f.Version == 0 && f.VersionDeletedBy == null)
                                                         on o.ReferenceName equals t.ReferenceName
                                                         into gt
                                     from tx in gt.DefaultIfEmpty()
                                     select new {Original = o, Translation = tx}).Select(f=>
                                     new LocaleViewModel
                                     {
                                         id = (f.Translation == null) ? (Guid?)null : f.Translation.TranslationDataID,
                                         Label = f.Original.ReferenceName,
                                         OriginalText = f.Original.TranslationName,
                                         OriginalCulture = f.Original.OriginCulture ?? "en-US",
                                         Translation = (f.Translation == null) ? null : f.Translation.Translation,
                                         TranslationCulture = m.TranslationCulture,
                                         VersionUpdated = (f.Translation == null) ? null : f.Translation.VersionUpdated
                                     }).ToArray();


                    var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
                    var lang = cultures.OrderBy(f=>f.Name).FirstOrDefault(f => f.Name.StartsWith(m.TranslationCulture));
                    
                    if (!m.LocaleQueue.Any())
                        return false; //ref didnt exist;
                    else
                    {
                        LocaleViewModel[] translate;
                        if (m.Refresh)
                            translate = m.LocaleQueue;
                        else
                            translate = m.LocaleQueue.Where(f => f.Translation == null).ToArray();
                        if (translate.Any())
                        using (var client = new HttpClient())
                        {
                            client.DefaultRequestHeaders.Add("X-HTTP-Method-Override", "GET");
                            foreach (var translation in translate)
                            {
                                System.Threading.Tasks.Task<HttpResponseMessage> rText = null;
                                Func<System.Threading.Tasks.Task<HttpResponseMessage>, System.Threading.Tasks.Task<string>> responseContent = async delegate(System.Threading.Tasks.Task<HttpResponseMessage> responseAsync)
                                {
                                    var response = responseAsync.Result;
                                    if (response.IsSuccessStatusCode)
                                    {
                                        dynamic json = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
                                        return json.data.translations[0].translatedText ?? string.Empty;
                                    }
                                    else
                                    {
                                        return null;
                                    }
                                };

                                client.DefaultRequestHeaders.Accept.Clear();
                                var requestContent = new FormUrlEncodedContent(new[] { 
                                    new KeyValuePair<string, string>("key", "AIzaSyA7mP-819Mgz4dy6X0NIlQ6SjyzDn5QEJA") ,
                                    //new KeyValuePair<string, string>("source", "en") ,
                                    new KeyValuePair<string, string>("target", lang.TwoLetterISOLanguageName),
                                    new KeyValuePair<string, string>("q", translation.OriginalText) 
                                });
                                rText = client.PostAsync(translateURL, requestContent);
                                
                                
                                if (rText != null)
                                    translation.Translation = responseContent(rText).Result;
                                if (!string.IsNullOrWhiteSpace(translation.Translation))
                                {
                                    if (!translation.TranslationDataID.HasValue)
                                    {
                                        translation.TranslationDataID = Guid.NewGuid();
                                        //Insert
                                        var tx = new TranslationData
                                        {
                                            TranslationDataID = translation.TranslationDataID.Value,
                                            TableType = LocaleViewModel.DocType,
                                            //ReferenceID = translation.Key,
                                            ReferenceName = translation.Label,
                                            //ReferenceUpdated = translation.Value.OriginalUpdated,
                                            OriginCulture = m.OriginalCulture ?? "en-US",
                                            TranslationCulture = lang.Name, //TODO could check real culture
                                            TranslationName = translation.OriginalText,
                                            Translation = translation.Translation,
                                            VersionUpdated = DateTime.UtcNow,
                                            VersionUpdatedBy = contact
                                        };
                                        d.TranslationData.AddObject(tx);
                                    }
                                    else
                                    {
                                        //Update
                                        var tx = (from o in d.TranslationData where o.TranslationDataID == translation.TranslationDataID && o.VersionDeletedBy == null select o).Single();
                                        tx.ReferenceName = translation.Label;
                                        //tx.ReferenceUpdated = translation.Value.OriginalUpdated;
                                        tx.OriginCulture = m.OriginalCulture ?? "en-US";
                                        tx.TranslationCulture = lang.Name; //TODO could check real culture
                                        tx.TranslationName = translation.OriginalText;
                                        tx.Translation = translation.Translation;
                                        tx.VersionUpdated = DateTime.UtcNow;
                                        tx.VersionUpdatedBy = contact;
                                    }
                                }

                            }
                            d.SaveChanges();
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                return false;
            }

            return true;

        }

        public bool UpdateLocale(LocaleViewModel m)
        {

            try
            {
                var contact = _users.ContactID;
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(m.id.Value, ActionPermission.Update, typeof(TranslationData)))
                        return false;                   
                    //Update
                    var tx = (from o in d.TranslationData where o.TranslationDataID == m.id && o.VersionDeletedBy == null && o.TableType == LocaleViewModel.DocType select o).Single();
                    tx.OriginCulture = m.OriginalCulture ?? "en-US";
                    if (m.TranslationName != null)
                        tx.TranslationName = m.TranslationName;
                    if (m.Translation != null)
                        tx.Translation = m.Translation;
                    tx.VersionUpdated = DateTime.UtcNow;
                    tx.VersionUpdatedBy = contact;
                    d.SaveChanges();
                    return true;
                }

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool DeleteLocale(LocaleViewModel m)
        {
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(m.id.Value, ActionPermission.Delete, typeof(TranslationData)))
                        return false;                  
                    //Delete
                    var tx = (from o in d.TranslationData where o.TranslationDataID == m.id && o.VersionDeletedBy == null && o.TableType == LocaleViewModel.DocType select o).Single();
                    d.TranslationData.DeleteObject(tx);
                    d.SaveChanges();
                    return true;
                }

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public AutomationViewModel[] GetMySteps()
        {
            var contact = _users.ContactID;
            var application = _users.ApplicationID;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                return (from o in d.E_SP_GetSteps(null, contact, application, null, null, null, null, null, null, null, null)
                         select new AutomationViewModel
                         {
                             PreviousStep = new ProjectPlanTaskResponse
                             {
                                 ProjectPlanTaskResponseID = o.ProjectPlanTaskResponseID,
                                 ProjectID = o.ProjectID,
                                 ProjectPlanTaskID = o.ProjectPlanTaskID,
                                 ResponsibleCompanyID = o.ResponsibleCompanyID,
                                 ResponsibleContactID = o.ResponsibleContactID,
                                 ActualTaskID = o.ActualTaskID,
                                 ActualWorkTypeID = o.ActualWorkTypeID,
                                 ActualGraphDataGroupID = o.ActualGraphDataGroupID,
                                 ActualGraphDataID = o.ActualGraphDataID,
                                 Began = o.Began,
                                 Completed = o.Completed,
                                 Hours = o.Hours,
                                 EstimatedProRataUnits = o.EstimatedProRataUnits,
                                 EstimatedProRataCost = o.EstimatedProRataCost,
                                 EstimatedValue = o.EstimatedValue,
                                 PerformanceMetricParameterID = o.PerformanceMetricParameterID,
                                 PerformanceMetricQuantity = o.PerformanceMetricQuantity,
                                 PerformanceMetricContributedPercent = o.PerformanceMetricContributedPercent,
                                 ApprovedProRataUnits = o.ApprovedProRataUnits,
                                 ApprovedProRataCost = o.ApprovedProRataCost,
                                 Approved = o.Approved,
                                 ApprovedBy = o.ApprovedBy,
                                 Comments = o.Comments,
                                 Version = o.Version,
                                 VersionAntecedentID = o.VersionAntecedentID,
                                 VersionCertainty = o.VersionCertainty,
                                 VersionWorkflowInstanceID = o.VersionWorkflowInstanceID,
                                 VersionUpdatedBy = o.VersionUpdatedBy,
                                 VersionDeletedBy = o.VersionDeletedBy,
                                 VersionOwnerContactID = o.VersionOwnerContactID,
                                 VersionOwnerCompanyID = o.VersionOwnerCompanyID,
                                 VersionUpdated = o.VersionUpdated,

                             },
                             label = o.GraphName,
                             Row = o.Row,
                             TotalRows = o.TotalRows,
                             Score = o.Score,
                             ProjectName = o.ProjectName,
                             ProjectCode = o.ProjectCode,
                             GraphDataGroupName = o.GraphDataGroupName,
                             GraphName = o.GraphName,
                             GraphContent = o.GraphContent,
                             LastEditedBy = o.LastEditedBy
                         }
                    ).Where(f=>f.Completed == null).OrderBy(f=>f.VersionUpdated).ToArray();                        
            }

        }

        public AutomationViewModel GetStep(Guid? sid, Guid? pid, Guid? tid, Guid? nid, Guid? gid, bool includeContent = false, bool includeDisconnected = false, bool monitor = true)
        {
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    Guid? taskID = null;
                    AutomationViewModel m = null;
                    if (sid.HasValue)
                        m = _automation.GetStep(d, sid.Value, tid, includeContent);
                    //New Step
                    if (m == null)
                    {
                        m = new AutomationViewModel {};
                        m.IncludeContent = includeContent;
                        m.PreviousStep = new ProjectPlanTaskResponse { ProjectPlanTaskResponseID = sid ?? Guid.NewGuid(), ProjectID = pid, ActualTaskID = taskID, ActualGraphDataGroupID = gid, ActualGraphDataID = nid };
                        if (tid.HasValue)
                            m.PreviousTask = new NKD.Module.BusinessObjects.Task { GraphDataID = nid, GraphDataGroupID = gid, TaskID = tid.Value };
                        else
                            m.PreviousTask = new NKD.Module.BusinessObjects.Task { };
                        if (!CreateStep(m))
                            return null;
                    }
                    return m;
                }

            }
            catch (Exception ex)
            {
                return null;
            }
        }



        public bool CreateStep(AutomationViewModel m)
        {
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(m.PreviousStepID.Value, ActionPermission.Create, typeof(ProjectPlanTaskResponse)))
                        return false;
                    if (d.ProjectPlanTaskResponses.Any(f => f.ProjectPlanTaskResponseID == m.PreviousStepID))
                    {
                        return false;
                    }
                    else
                    {
                        return _automation.DoNext(m);
                    }
                }
            }
            catch (Exception ex)
            {
                m.Error = ex.Message;
                return false;
            }
        }



        public bool UpdateStep(AutomationViewModel m) { return false; }



        public bool DeleteStep(Guid stepID) { return false; }



        public ProjectViewModel GetProject(Guid pid)
        {
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(pid, ActionPermission.Read, typeof(Project)))
                        return null;
                    var m = (from o in d.Projects.Where(f => f.ProjectID == pid && f.Version == 0 && f.VersionDeletedBy == null)
                             select new ProjectViewModel
                             {
                                 id = o.ProjectID,
                                 ProjectCode = o.ProjectCode,
                                 ProjectName = o.ProjectName,
                                 ClientCompanyID = o.ClientCompanyID,
                                 ClientContactID = o.ClientContactID
                             }).Single();
                    m.ProjectData = (from o in d.ProjectDatas.Where(f=>f.ProjectID == pid && f.Version == 0 && f.VersionDeletedBy == null) select o.ProjectDataID).ToArray();

                    return m;
                }

            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public ProjectDataViewModel[] GetProjectData(Guid[] pdids)
        {
            if (pdids == null || pdids.Length == 0)
                return new ProjectDataViewModel[] { };
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    var pdid = pdids[0];
                    var pid = d.ProjectDatas.Single(g=> g.ProjectDataID == pdid && g.Version == 0 && g.VersionDeletedBy == null).ProjectID;
                    if (pid == null || !CheckPermission(pid, ActionPermission.Read, typeof(Project)))
                        return null;
                    var m = (from o in d.ProjectDatas.Where(f=> f.VersionDeletedBy == null  && f.Version == 0 
                        && f.ProjectID ==  pid)
                             join p in d.ProjectDataTemplates.Where(h=> h.Version == 0 && h.VersionDeletedBy == null)
                             on o.ProjectDataTemplateID equals p.ProjectDataTemplateID into tp
                             from t in tp.DefaultIfEmpty()
                             select new ProjectDataViewModel
                             {
                                id = o.ProjectDataID,
                                CommonName = t.CommonName,
                                UniqueID = t.UniqueID ,
                                UniqueIDSystemDataType = t.UniqueIDSystemDataType ,
                                TemplateStructure = t.TemplateStructure ,
                                TemplateStructureChecksum = t.TemplateStructureChecksum ,
                                TemplateActions = t.TemplateActions ,
                                TemplateType = t.TemplateType ,
                                TemplateMulti = t.TemplateMulti ,
                                TemplateSingle = t.TemplateSingle ,
                                TableType = t.TableType ,
                                ReferenceID = t.ReferenceID  ,
                                UserDataType = t.UserDataType ,
                                SystemDataType = t.SystemDataType ,
                                IsReadOnly = t.IsReadOnly ,
                                IsVisible = t.IsVisible ,
                                ProjectDataTemplateID = o.ProjectDataTemplateID ,
                                ProjectID = o.ProjectID ,
                                ProjectPlanTaskResponseID = o.ProjectPlanTaskResponseID ,
                                Value = o.Value  
                             }).ToArray();                    
                    return m;
                }

            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public bool CreateProjectData(ProjectDataViewModel m)
        {
            var contact = _users.ContactID;
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(m.ProjectID, ActionPermission.Read, typeof(Project)))
                        return false;

                    ProjectDataTemplate pdt = null;
                    if (m.ProjectDataTemplateID.HasValue)
                        pdt = (from o in d.ProjectDataTemplates where o.ProjectDataTemplateID == m.ProjectDataTemplateID.Value select o).FirstOrDefault();
                    if (pdt == null)
                    {
                        pdt = new ProjectDataTemplate
                        {
                            ProjectDataTemplateID = m.ProjectDataTemplateID.Value,
                            CommonName = m.CommonName,
                            UniqueID = m.UniqueID,
                            UniqueIDSystemDataType = m.UniqueIDSystemDataType,
                            TemplateStructure = m.TemplateStructure,
                            TemplateStructureChecksum = m.TemplateStructureChecksum,
                            TemplateActions = m.TemplateActions,
                            TemplateType = m.TemplateType,
                            TemplateMulti = m.TemplateMulti,
                            TemplateSingle = m.TemplateSingle,
                            TableType = m.TableType,
                            ReferenceID = m.ReferenceID,
                            UserDataType = m.UserDataType,
                            SystemDataType = m.SystemDataType,
                            IsReadOnly = m.IsReadOnly,
                            IsVisible = m.IsVisible,
                            VersionUpdated = DateTime.UtcNow,
                            VersionUpdatedBy = contact
                        };
                        d.ProjectDataTemplates.AddObject(pdt);
                    }
                    var pd = new ProjectData
                    {
                        ProjectDataID = m.id.Value,
                        ProjectDataTemplateID = m.ProjectDataTemplateID,
                        ProjectID = m.ProjectID,
                        ProjectPlanTaskResponseID = m.ProjectPlanTaskResponseID,
                        Value = m.Value,
                        VersionUpdated = DateTime.UtcNow,
                        VersionUpdatedBy = contact
                    };
                    d.ProjectDatas.AddObject(pd);
                    d.SaveChanges();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool UpdateProjectData(ProjectDataViewModel m)
        {

            try
            {
                var contact = _users.ContactID;
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(m.id.Value, ActionPermission.Update, typeof(ProjectData)))
                        return false;
                    //Update
                    var pd = (from o in d.ProjectDatas where o.ProjectDataID == m.id && o.VersionDeletedBy == null select o).Single();
                    if (pd.Value != null && pd.Value != m.Value)
                    {
                        pd.Value = m.Value;
                        pd.VersionUpdated = DateTime.UtcNow;
                        pd.VersionUpdatedBy = contact;
                        d.SaveChanges();
                    }
                    return true;
                }

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool DeleteProjectData(ProjectDataViewModel m)
        {
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(m.id.Value, ActionPermission.Delete, typeof(ProjectData)))
                        return false;
                    //Delete
                    var pd = (from o in d.ProjectDatas where o.ProjectDataID == m.id && o.VersionDeletedBy == null select o).Single();
                    d.ProjectDatas.DeleteObject(pd);
                    d.SaveChanges();
                    return true;
                }

            }
            catch (Exception ex)
            {
                return false;
            }
        }



        public EdgeConditionViewModel[] GetEdgeCondition(Guid[] mids)
        {
            if (mids == null || mids.Length == 0)
                return new EdgeConditionViewModel[] { };
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    var mid = mids[0];
                    var id = d.GraphDataRelationConditions.Single(g => g.GraphDataRelationConditionID == mid && g.Version == 0 && g.VersionDeletedBy == null).GraphDataRelationID;
                    if (id == null || !CheckPermission(id, ActionPermission.Read, typeof(GraphDataRelation)))
                        return null;
                    var m = (from o in d.GraphDataRelationConditions.Where(f => f.VersionDeletedBy == null && f.Version == 0 && f.GraphDataRelationID == id)
                             join c in d.Precondition.Where(h => h.Version == 0 && h.VersionDeletedBy == null)
                             on o.ConditionID equals c.ConditionID
                             select new EdgeConditionViewModel
                             {
                                 id = o.GraphDataRelationConditionID,
                                 JSON = c.JSON,
                                 OverrideProjectDataWithJsonCustomVars = c.OverrideProjectDataWithJsonCustomVars,
                                 Condition = c.Condition,
                                 Grouping = o.Grouping,
                                 Sequence = o.Sequence,
                                 JoinedBy = o.JoinedBy,
                                 ConditionID = c.ConditionID,
                                 GraphDataRelationID = o.GraphDataRelationID
                             }).ToArray();
                    return m;
                }

            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public bool CreateEdgeCondition(EdgeConditionViewModel m)
        {
            var contact = _users.ContactID;
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(m.GraphDataRelationID, ActionPermission.Update, typeof(GraphDataRelation)))
                        return false;
                    var c = new Precondition
                    {
                        ConditionID = m.ConditionID.Value,
                        Condition = m.Condition,
                        JSON = m.JSON,
                        OverrideProjectDataWithJsonCustomVars = m.OverrideProjectDataWithJsonCustomVars,
                        VersionUpdated = DateTime.UtcNow,
                        VersionUpdatedBy = contact
                    };
                    d.Precondition.AddObject(c);
                    var gdrc = new GraphDataRelationCondition
                    {
                        GraphDataRelationConditionID = m.id.Value,
                        GraphDataRelationID = m.GraphDataRelationID.Value,
                        ConditionID = m.ConditionID.Value,
                        Grouping = m.Grouping,
                        Sequence = m.Sequence,
                        JoinedBy = m.JoinedBy,
                        VersionUpdated = DateTime.UtcNow,
                        VersionUpdatedBy = contact
                    };
                    d.GraphDataRelationConditions.AddObject(gdrc);
                    d.SaveChanges();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool UpdateEdgeCondition(EdgeConditionViewModel m)
        {

            try
            {
                var contact = _users.ContactID;
                var now = DateTime.Now;
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(m.id.Value, ActionPermission.Update, typeof(GraphDataRelationCondition)))
                        return false;
                    //Update
                    var gdrc = (from o in d.GraphDataRelationConditions where o.GraphDataRelationConditionID == m.id && o.VersionDeletedBy == null select o).Single();
                    if (m.JSON != null && gdrc.Condition.JSON != m.JSON)
                        gdrc.Condition.JSON = m.JSON;
                    if (m.Condition != null && gdrc.Condition.Condition != m.Condition)
                        gdrc.Condition.Condition = m.Condition;
                    if (m.OverrideProjectDataWithJsonCustomVars != null && gdrc.Condition.OverrideProjectDataWithJsonCustomVars != m.OverrideProjectDataWithJsonCustomVars)
                        gdrc.Condition.OverrideProjectDataWithJsonCustomVars = m.OverrideProjectDataWithJsonCustomVars;
                    if (m.Grouping != null && gdrc.Grouping != m.Grouping)
                        gdrc.Grouping = m.Grouping;
                    if (m.Sequence != null && gdrc.Sequence != m.Sequence)
                        gdrc.Sequence = m.Sequence;
                    if (m.JoinedBy != null && gdrc.JoinedBy != m.JoinedBy)
                        gdrc.JoinedBy = m.JoinedBy;
                    if (gdrc.Condition.EntityState == EntityState.Modified)
                    {
                        gdrc.Condition.VersionUpdated = now;
                        gdrc.Condition.VersionUpdatedBy = contact;
                    }
                    if (gdrc.EntityState == EntityState.Modified)
                    {
                        gdrc.VersionUpdated = now;
                        gdrc.VersionUpdatedBy = contact;
                    }
                    d.SaveChanges();
                    return true;
                }

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool DeleteEdgeCondition(EdgeConditionViewModel m)
        {
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(m.id.Value, ActionPermission.Delete, typeof(GraphDataRelationCondition)))
                        return false;
                    //Delete
                    var pd = (from o in d.GraphDataRelationConditions where o.GraphDataRelationConditionID == m.id && o.VersionDeletedBy == null select o).Single();
                    if (pd != null && pd.ConditionID != null)
                    {
                        try
                        {
                            d.Precondition.DeleteObject(pd.Condition);
                        }
                        catch { }
                    }
                    d.GraphDataRelationConditions.DeleteObject(pd);
                    d.SaveChanges();
                    return true;
                }

            }
            catch (Exception ex)
            {
                return false;
            }
        }



        public ContextVariableViewModel[] GetContextNames(Guid wfid)
        {
            if (wfid == Guid.Empty)
                return new ContextVariableViewModel[] { };
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    var table = d.GetTableName(typeof(GraphData));
                    return (from o in d.GraphDataRelation.Where(f => f.GraphDataGroupID == wfid)
                            join n in d.GraphData on o.FromGraphDataID equals n.GraphDataID
                            join c in d.ProjectDataTemplates on n.GraphDataID equals c.ReferenceID
                            where c.TableType == table
                            select c
                            ).Union(
                            from o in d.GraphDataRelation.Where(f => f.GraphDataGroupID == wfid)
                            join n in d.GraphData on o.ToGraphDataID equals n.GraphDataID
                            join c in d.ProjectDataTemplates on n.GraphDataID equals c.ReferenceID
                            where c.TableType == table
                            select c
                            ).Select(f =>
                            new ContextVariableViewModel { id = f.ProjectDataTemplateID, CommonName = f.CommonName, FormID = f.FormID, GraphDataID = f.ReferenceID }
                            ).ToArray();
                    
                }

            }
            catch (Exception ex)
            {
                return null;
            }
        }




        public ConditionViewModel GetCondition(Guid id)
        {
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (id == null || !CheckPermission(id, ActionPermission.Read, typeof(Precondition)))
                        return null;
                    var m = (from c in d.Precondition.Where(h => h.Version == 0 && h.VersionDeletedBy == null && h.ConditionID == id)
                             select new ConditionViewModel
                             {
                                 id = c.ConditionID,
                                 JSON = c.JSON,
                                 OverrideProjectDataWithJsonCustomVars = c.OverrideProjectDataWithJsonCustomVars,
                                 Condition = c.Condition
                             }).SingleOrDefault();
                    return m;
                }

            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public bool CreateCondition(ConditionViewModel m)
        {
            var contact = _users.ContactID;
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(null, ActionPermission.Create, typeof(Precondition)))
                        return false;
                    var c = new Precondition
                    {
                        ConditionID = m.id.Value,
                        Condition = m.Condition,
                        JSON = m.JSON,
                        OverrideProjectDataWithJsonCustomVars = m.OverrideProjectDataWithJsonCustomVars,
                        VersionUpdated = DateTime.UtcNow,
                        VersionUpdatedBy = contact
                    };
                    d.Precondition.AddObject(c);                    
                    d.SaveChanges();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool UpdateCondition(ConditionViewModel m)
        {

            try
            {
                var contact = _users.ContactID;
                var now = DateTime.Now;
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(m.id.Value, ActionPermission.Update, typeof(Precondition)))
                        return false;
                    //Update
                    var cc = (from o in d.Precondition where o.ConditionID == m.id && o.VersionDeletedBy == null select o).Single();
                    if (m.JSON != null && cc.JSON != m.JSON)
                        cc.JSON = m.JSON;
                    if (m.Condition != null && cc.Condition != m.Condition)
                        cc.Condition = m.Condition;
                    if (m.OverrideProjectDataWithJsonCustomVars != null && cc.OverrideProjectDataWithJsonCustomVars != m.OverrideProjectDataWithJsonCustomVars)
                        cc.OverrideProjectDataWithJsonCustomVars = m.OverrideProjectDataWithJsonCustomVars;                 
                    if (cc.EntityState == EntityState.Modified)
                    {
                        cc.VersionUpdated = now;
                        cc.VersionUpdatedBy = contact;
                    }                 
                    d.SaveChanges();
                    return true;
                }

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool DeleteCondition(ConditionViewModel m)
        {
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(m.id.Value, ActionPermission.Delete, typeof(Precondition)))
                        return false;
                    //Delete
                    var pd = (from o in d.Precondition where o.ConditionID == m.id && o.VersionDeletedBy == null select o).Single();                    
                    d.Precondition.DeleteObject(pd);
                    d.SaveChanges();
                    return true;
                }

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public TaskViewModel GetTask(Guid gid, Guid nid)
        {
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(gid, ActionPermission.Read, typeof(GraphDataGroup)))
                        return null;
                    var m = (from o in d.Tasks.Where(h => h.Version == 0 && h.VersionDeletedBy == null && h.GraphDataID == nid && h.GraphDataGroupID == gid).OrderByDescending(f=>f.VersionUpdated)
                             select new TaskViewModel
                             {
                                 id = o.TaskID,
                                 TaskID = o.TaskID,
                                 TaskName = o.TaskName,
                                 WorkTypeID = o.WorkTypeID,
                                 WorkCompanyID = o.WorkCompanyID,
                                 WorkContactID = o.WorkContactID,
                                 GraphDataGroupID = o.GraphDataGroupID,
                                 GraphDataID = o.GraphDataID,
                                 DefaultPriority = o.DefaultPriority,
                                 EstimatedDuration = o.EstimatedDuration,
                                 EstimatedDurationUnitID = o.EstimatedDurationUnitID,
                                 EstimatedLabourCosts = o.EstimatedLabourCosts,
                                 EstimatedCapitalCosts = o.EstimatedCapitalCosts,
                                 EstimatedValue = o.EstimatedValue,
                                 EstimatedIntangibleValue = o.EstimatedIntangibleValue,
                                 EstimatedRevenue = o.EstimatedRevenue,
                                 PerformanceMetricParameterID = o.PerformanceMetricParameterID,
                                 PerformanceMetricQuantity = o.PerformanceMetricQuantity,
                                 Comment = o.Comment
                             }).FirstOrDefault();
                    return m;
                }

            }
            catch (Exception ex)
            {
                return null;
            }
        }



        public TaskViewModel GetTask(Guid id)
        {
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (id == null || !CheckPermission(id, ActionPermission.Read, typeof(Task)))
                        return null;
                    var m = (from o in d.Tasks.Where(h => h.Version == 0 && h.VersionDeletedBy == null && h.TaskID == id)
                             select new TaskViewModel
                             {
                                 id = o.TaskID,
                                 TaskID = o.TaskID,
                                 TaskName = o.TaskName,
                                 WorkTypeID = o.WorkTypeID,
                                 WorkCompanyID = o.WorkCompanyID,
                                 WorkContactID = o.WorkContactID,
                                 GraphDataGroupID = o.GraphDataGroupID,
                                 GraphDataID = o.GraphDataID,
                                 DefaultPriority = o.DefaultPriority,
                                 EstimatedDuration = o.EstimatedDuration,
                                 EstimatedDurationUnitID = o.EstimatedDurationUnitID,
                                 EstimatedLabourCosts = o.EstimatedLabourCosts,
                                 EstimatedCapitalCosts = o.EstimatedCapitalCosts,
                                 EstimatedValue = o.EstimatedValue,
                                 EstimatedIntangibleValue = o.EstimatedIntangibleValue,
                                 EstimatedRevenue = o.EstimatedRevenue,
                                 PerformanceMetricParameterID = o.PerformanceMetricParameterID,
                                 PerformanceMetricQuantity = o.PerformanceMetricQuantity,
                                 Comment = o.Comment
                             }).SingleOrDefault();
                    return m;
                }

            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public bool CreateTask(TaskViewModel m)
        {
            var contact = _users.ContactID;
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(null, ActionPermission.Create, typeof(Task)))
                        return false;
                    var c = new Task
                    {
                        TaskID = m.id.Value,
                        TaskName = m.TaskName,
                        WorkTypeID = m.WorkTypeID,
                        WorkCompanyID = m.WorkCompanyID,
                        WorkContactID = m.WorkContactID,
                        GraphDataGroupID = m.GraphDataGroupID,
                        GraphDataID = m.GraphDataID,
                        DefaultPriority = m.DefaultPriority,
                        EstimatedDuration = m.EstimatedDuration,
                        EstimatedDurationUnitID = m.EstimatedDurationUnitID,
                        EstimatedLabourCosts = m.EstimatedLabourCosts,
                        EstimatedCapitalCosts = m.EstimatedCapitalCosts,
                        EstimatedValue = m.EstimatedValue,
                        EstimatedIntangibleValue = m.EstimatedIntangibleValue,
                        EstimatedRevenue = m.EstimatedRevenue,
                        PerformanceMetricParameterID = m.PerformanceMetricParameterID,
                        PerformanceMetricQuantity = m.PerformanceMetricQuantity,
                        Comment = m.Comment,
                        VersionUpdated = DateTime.UtcNow,
                        VersionUpdatedBy = contact
                    };
                    d.Tasks.AddObject(c);
                    d.SaveChanges();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool UpdateTask(TaskViewModel m)
        {

            try
            {
                var contact = _users.ContactID;
                var now = DateTime.Now;
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(m.id.Value, ActionPermission.Update, typeof(Task)))
                        return false;
                    //Update
                    var cc = (from o in d.Tasks where o.TaskID == m.id && o.VersionDeletedBy == null select o).Single();
                    if (m.TaskName != null && cc.TaskName != m.TaskName) cc.TaskName = m.TaskName;
                    if (m.WorkTypeID != null && cc.WorkTypeID != m.WorkTypeID) cc.WorkTypeID = m.WorkTypeID; 
                    if (m.WorkCompanyID != null && cc.WorkCompanyID != m.WorkCompanyID) cc.WorkCompanyID = m.WorkCompanyID;
                    if (m.WorkContactID != null && cc.WorkContactID != m.WorkContactID) cc.WorkContactID = m.WorkContactID;
                    if (m.GraphDataGroupID != null && cc.GraphDataGroupID != m.GraphDataGroupID) cc.GraphDataGroupID = m.GraphDataGroupID; 
                    if (m.GraphDataID != null && cc.GraphDataID != m.GraphDataID) cc.GraphDataID = m.GraphDataID; 
                    if (cc.DefaultPriority != m.DefaultPriority) cc.DefaultPriority = m.DefaultPriority; 
                    if (m.EstimatedDuration != null && cc.EstimatedDuration != m.EstimatedDuration) cc.EstimatedDuration = m.EstimatedDuration; 
                    if (m.EstimatedLabourCosts != null && cc.EstimatedLabourCosts != m.EstimatedLabourCosts) cc.EstimatedLabourCosts = m.EstimatedLabourCosts; 
                    if (m.EstimatedCapitalCosts != null && cc.EstimatedCapitalCosts != m.EstimatedCapitalCosts) cc.EstimatedCapitalCosts = m.EstimatedCapitalCosts;
                    if (m.EstimatedValue != null && cc.EstimatedValue != m.EstimatedValue) cc.EstimatedValue = m.EstimatedValue; 
                    if (m.EstimatedIntangibleValue != null && cc.EstimatedIntangibleValue != m.EstimatedIntangibleValue) cc.EstimatedIntangibleValue = m.EstimatedIntangibleValue;
                    if (m.EstimatedRevenue != null && cc.EstimatedRevenue != m.EstimatedRevenue) cc.EstimatedRevenue = m.EstimatedRevenue;
                    if (m.PerformanceMetricParameterID != null && cc.PerformanceMetricParameterID != m.PerformanceMetricParameterID) cc.PerformanceMetricParameterID = m.PerformanceMetricParameterID;
                    if (m.PerformanceMetricQuantity != null && cc.PerformanceMetricQuantity != m.PerformanceMetricQuantity) cc.PerformanceMetricQuantity = m.PerformanceMetricQuantity;
                    if (m.Comment != null && cc.Comment != m.Comment) cc.Comment = m.Comment;
                    //Not Implemented
                    d.SaveChanges();
                    return true;
                }

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool DeleteTask(TaskViewModel m)
        {
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(m.id.Value, ActionPermission.Delete, typeof(Task)))
                        return false;
                    //Delete
                    var pd = (from o in d.Tasks where o.TaskID == m.id && o.VersionDeletedBy == null select o).Single();
                    d.Tasks.DeleteObject(pd);
                    d.SaveChanges();
                    return true;
                }

            }
            catch (Exception ex)
            {
                return false;
            }
        }





        public TriggerViewModel GetTrigger(Guid id)
        {
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (id == null || !CheckPermission(id, ActionPermission.Read, typeof(Trigger)))
                        return null;
                    var m = (from o in d.Triggers.Where(h => h.Version == 0 && h.VersionDeletedBy == null && h.TriggerID == id)
                             select new TriggerViewModel
                             {
                                id = o.TriggerID,
                                TriggerID = o.TriggerID,
                                CommonName = o.CommonName,
                                TriggerTypeID = o.TriggerTypeID,
                                JsonMethod = o.JsonMethod,
                                JsonProxyApplicationID = o.JsonProxyApplicationID,
                                JsonProxyContactID = o.JsonProxyContactID,
                                JsonProxyCompanyID = o.JsonProxyCompanyID,
                                JsonAuthorizedBy = o.JsonAuthorizedBy,
                                JsonUsername = o.JsonUsername,
                                JsonPassword = o.JsonPassword,
                                JsonPasswordType = o.JsonPasswordType,
                                JSON = o.JSON,
                                SystemMethod = o.SystemMethod,
                                ConditionID = o.ConditionID,
                                ExternalURL = o.ExternalURL,
                                ExternalRequestMethod = o.ExternalRequestMethod,
                                ExternalFormType = o.ExternalFormType,
                                PassThrough = o.PassThrough,
                                //GraphDataTriggerID = o.GraphDataTriggerID,
                                //GraphDataID = o.GraphDataID,
                                //GraphDataGroupTriggerID = o.GraphDataGroupTriggerID,
                                //GraphDataGroupID = o.GraphDataGroupID,
                                //MergeProjectData = o.MergeProjectData,
                                //OnEnter = o.OnEnter,
                                //OnDataUpdate = o.OnDataUpdate,
                                //OnExit = o.OnExit,
                                //RunOnce = o.RunOnce
                             }).SingleOrDefault();
                    return m;
                }

            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public bool CreateTrigger(TriggerViewModel m)
        {
            var contact = _users.ContactID;
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(null, ActionPermission.Create, typeof(Trigger)))
                        return false;
                    var c = new Trigger
                    {
                        TriggerID = m.id.Value,
                        CommonName = m.CommonName,
                        TriggerTypeID = m.TriggerTypeID,
                        JsonMethod = m.JsonMethod,
                        JsonProxyApplicationID = m.JsonProxyApplicationID,
                        JsonProxyContactID = m.JsonProxyContactID,
                        JsonProxyCompanyID = m.JsonProxyCompanyID,
                        JsonAuthorizedBy = m.JsonAuthorizedBy,
                        JsonUsername = m.JsonUsername,
                        JsonPassword = m.JsonPassword,
                        JsonPasswordType = m.JsonPasswordType,
                        JSON = m.JSON,
                        SystemMethod = m.SystemMethod,
                        ConditionID = m.ConditionID,
                        ExternalURL = m.ExternalURL,
                        ExternalRequestMethod = m.ExternalRequestMethod,
                        ExternalFormType = m.ExternalFormType,
                        PassThrough = m.PassThrough,
                        //GraphDataTriggerID = m.GraphDataTriggerID,
                        //GraphDataID = m.GraphDataID,
                        //GraphDataGroupTriggerID = m.GraphDataGroupTriggerID,
                        //GraphDataGroupID = m.GraphDataGroupID,
                        //MergeProjectData = m.MergeProjectData,
                        //OnEnter = m.OnEnter,
                        //OnDataUpdate = m.OnDataUpdate,
                        //OnExit = m.OnExit,
                        //RunOnce = m.RunOnce,
                        VersionUpdated = DateTime.UtcNow,
                        VersionUpdatedBy = contact
                    };
                    d.Triggers.AddObject(c);
                    d.SaveChanges();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool UpdateTrigger(TriggerViewModel m)
        {

            try
            {
                var contact = _users.ContactID;
                var now = DateTime.Now;
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(m.id.Value, ActionPermission.Update, typeof(Trigger)))
                        return false;
                    //Update
                    var cc = (from o in d.Triggers where o.TriggerID == m.id && o.VersionDeletedBy == null select o).Single();
                    if (m.CommonName != null && cc.CommonName != m.CommonName) cc.CommonName = m.CommonName;
                    if (m.TriggerTypeID != null && cc.TriggerTypeID != m.TriggerTypeID) cc.TriggerTypeID = m.TriggerTypeID;
                    if (m.JsonMethod != null && cc.JsonMethod != m.JsonMethod) cc.JsonMethod = m.JsonMethod;
                    if (m.JsonProxyApplicationID != null && cc.JsonProxyApplicationID != m.JsonProxyApplicationID) cc.JsonProxyApplicationID = m.JsonProxyApplicationID;
                    if (m.JsonProxyContactID != null && cc.JsonProxyContactID != m.JsonProxyContactID) cc.JsonProxyContactID = m.JsonProxyContactID;
                    if (m.JsonProxyCompanyID != null && cc.JsonProxyCompanyID != m.JsonProxyCompanyID) cc.JsonProxyCompanyID = m.JsonProxyCompanyID;
                    if (m.JsonAuthorizedBy != null && cc.JsonAuthorizedBy != m.JsonAuthorizedBy) cc.JsonAuthorizedBy = m.JsonAuthorizedBy;
                    if (m.JsonUsername != null && cc.JsonUsername != m.JsonUsername) cc.JsonUsername = m.JsonUsername;
                    if (m.JsonPassword != null && cc.JsonPassword != m.JsonPassword) cc.JsonPassword = m.JsonPassword;
                    if (m.JsonPasswordType != null && cc.JsonPasswordType != m.JsonPasswordType) cc.JsonPasswordType = m.JsonPasswordType;
                    if (m.JSON != null && cc.JSON != m.JSON) cc.JSON = m.JSON;
                    if (m.SystemMethod != null && cc.SystemMethod != m.SystemMethod) cc.SystemMethod = m.SystemMethod;
                    if (m.ConditionID != null && cc.ConditionID != m.ConditionID) cc.ConditionID = m.ConditionID;
                    if (m.ExternalURL != null && cc.ExternalURL != m.ExternalURL) cc.ExternalURL = m.ExternalURL;
                    if (m.ExternalRequestMethod != null && cc.ExternalRequestMethod != m.ExternalRequestMethod) cc.ExternalRequestMethod = m.ExternalRequestMethod;
                    if (m.ExternalFormType != null && cc.ExternalFormType != m.ExternalFormType) cc.ExternalFormType = m.ExternalFormType;
                    if (m.PassThrough != null && cc.PassThrough != m.PassThrough) cc.PassThrough = m.PassThrough;
                    //if (m.GraphDataTriggerID != null && cc.GraphDataTriggerID != m.GraphDataTriggerID) cc.GraphDataTriggerID = m.GraphDataTriggerID;
                    //if (m.GraphDataID != null && cc.GraphDataID != m.GraphDataID) cc.GraphDataID = m.GraphDataID;
                    //if (m.GraphDataGroupTriggerID != null && cc.GraphDataGroupTriggerID != m.GraphDataGroupTriggerID) cc.GraphDataGroupTriggerID = m.GraphDataGroupTriggerID;
                    //if (m.GraphDataGroupID != null && cc.GraphDataGroupID != m.GraphDataGroupID) cc.GraphDataGroupID = m.GraphDataGroupID;
                    //if (m.MergeProjectData != null && cc.MergeProjectData != m.MergeProjectData) cc.MergeProjectData = m.MergeProjectData;
                    //if (m.OnEnter != null && cc.OnEnter != m.OnEnter) cc.OnEnter = m.OnEnter;
                    //if (m.OnDataUpdate != null && cc.OnDataUpdate != m.OnDataUpdate) cc.OnDataUpdate = m.OnDataUpdate;
                    //if (m.OnExit != null && cc.OnExit != m.OnExit) cc.OnExit = m.OnExit;
                    //if (m.RunOnce != null && cc.RunOnce != m.RunOnce) cc.RunOnce = m.RunOnce;
                    //Not Implemented
                    d.SaveChanges();
                    return true;
                }

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool DeleteTrigger(TriggerViewModel m)
        {
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(m.id.Value, ActionPermission.Delete, typeof(Trigger)))
                        return false;
                    //Delete
                    var pd = (from o in d.Triggers where o.TriggerID == m.id && o.VersionDeletedBy == null select o).Single();
                    d.Triggers.DeleteObject(pd);
                    d.SaveChanges();
                    return true;
                }

            }
            catch (Exception ex)
            {
                return false;
            }
        }

    }
}
