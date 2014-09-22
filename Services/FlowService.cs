﻿using System;
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
using System.Threading.Tasks;


namespace EXPEDIT.Flow.Services {

    [UsedImplicitly]
    public class FlowService : IFlowService, Orchard.Users.Events.IUserEventHandler
    {

        public const string STAT_NAME_FLOW_ACCESS = "FlowAccess";
        public static Guid FLOW_MODEL_ID = new Guid("1DB0B648-D8A7-4FB9-8F3F-B2846822258C");
        public const string FS_FLOW_CONTACT_ID = "{0}:ValidFlowUser";

        private readonly IUsersService _users;
        private readonly IOrchardServices _services;
        public ILogger Logger { get; set; }

        public FlowService(
            IOrchardServices orchardServices,
            IUsersService users
            )
        {
            _users = users;
            _services = orchardServices;
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
                    (from o in d.GraphDataFileDatas where !files.Contains(o.FileDataID.Value) && o.GraphDataID == m.GraphDataID.Value select o).Delete(); //Delete redundant links
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
                    (from o in d.GraphDataLocations where o.GraphDataID == m.GraphDataID select o).Delete();
                    (from o in d.GraphDataFileDatas where o.GraphDataID == m.GraphDataID select o).Delete();
                    (from o in d.GraphDataContexts where o.GraphDataID == m.GraphDataID select o).Delete();
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
            }
            else
            {
                (from o in d.GraphDataLocations where o.GraphDataID == m.GraphDataID select o).Delete();
                (from o in d.GraphDataFileDatas where o.GraphDataID == m.GraphDataID select o).Delete();
                (from o in d.GraphDataContexts where o.GraphDataID == m.GraphDataID select o).Delete();
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
                d.GraphDataContexts.Where(f => f.GraphDataID == mid).Delete();
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

        public bool CheckPermission(Guid gid, ActionPermission permission, Type typeToCheck)
        {
            var contact = _users.ContactID;
            if (contact == null)
                contact = Guid.NewGuid();
            var application = _users.ApplicationID;
            var company = _users.DefaultContactCompanyID;
            var d = new NKDC(_users.ApplicationConnectionString, null);
            var table = d.GetTableName(typeToCheck);
            return _users.CheckPermission(new SecuredBasic
            {
                AccessorApplicationID = _users.ApplicationID,
                AccessorContactID = _users.ContactID,
                OwnerTableType = table
            }, permission);
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
                            m.TableType = d.GetTableName(typeof(GraphDataGroup));
                            //TODO: Could flag not to translate text here
                            break;
                        case SearchType.Flow:
                            if (!CheckPermission(m.DocID.Value, ActionPermission.Read, typeof(GraphData)))
                                return false;
                            m.TableType = d.GetTableName(typeof(GraphData));
                            m.TranslationQueue = (from g in d.GraphData.Where(f=>f.GraphDataID == m.DocID)
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
                                                    f.Translation.Translation
                                                  ));
                            break;
                        default:
                            return false;
                    }

                    if (!m.TranslationQueue.Any())
                        return false; //ref didnt exist;
                    else
                    {
                        var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
                        var lang = cultures.FirstOrDefault(f => f.Name.StartsWith(m.TranslationCulture));
                        using (var client = new HttpClient())
                        {
                            client.DefaultRequestHeaders.Add("X-HTTP-Method-Override", "GET");
                            foreach (var translation in m.TranslationQueue)
                            {
                                if (translation.Value.TranslatedName != null && translation.Value.TranslatedText != null)
                                    continue;

                                switch (m.SearchType)
                                {
                                    case SearchType.FlowGroup:
                                        break;
                                    case SearchType.Flow:
                                        translation.Value.OriginalText = (from o in d.GraphData where o.GraphDataID == m.DocID && o.VersionDeletedBy == null && o.Version == 0 select o.GraphContent).FirstOrDefault();
                                        break;
                                    default:
                                        return false;
                                }
                                Task<HttpResponseMessage> rName = null, rText = null;
                                Func<Task<HttpResponseMessage>, Task<string>> responseContent = async delegate(Task<HttpResponseMessage> responseAsync)
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
                                if (string.IsNullOrWhiteSpace(translation.Value.TranslatedName) && !string.IsNullOrWhiteSpace(translation.Value.OriginalName))
                                {
                                    client.DefaultRequestHeaders.Accept.Clear();
                                    var requestContent = new FormUrlEncodedContent(new[] { 
                                    new KeyValuePair<string, string>("key", "AIzaSyA7mP-819Mgz4dy6X0NIlQ6SjyzDn5QEJA") ,
                                    //new KeyValuePair<string, string>("source", "en") ,
                                    new KeyValuePair<string, string>("target", lang.TwoLetterISOLanguageName),
                                    new KeyValuePair<string, string>("q", translation.Value.OriginalName) 
                                    });
                                    rName = client.PostAsync(translateURL, requestContent);
                                }
                                if (string.IsNullOrWhiteSpace(translation.Value.TranslatedText) && !string.IsNullOrWhiteSpace(translation.Value.OriginalText))
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
                                    if (!translation.Value.TranslationDataID.HasValue)
                                    {
                                        translation.Value.TranslationDataID = Guid.NewGuid();
                                        //Insert
                                        var tx = new TranslationData
                                        {
                                            TranslationDataID = translation.Value.TranslationDataID.Value,
                                            TableType = m.TableType,
                                            ReferenceID = m.DocID,
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
                                    }
                                    else
                                    {
                                        //Update
                                        var tx = (from o in d.TranslationData where o.TranslationDataID == translation.Value.TranslationDataID && o.VersionDeletedBy == null select o).Single();
                                        tx.ReferenceName = translation.Value.OriginalName;
                                        tx.ReferenceUpdated = translation.Value.OriginalUpdated;
                                        tx.OriginCulture = m.OriginCulture ?? "en-US";
                                        tx.TranslationCulture = lang.Name; //TODO could check real culture
                                        tx.TranslationName = translation.Value.TranslatedName;
                                        tx.Translation = translation.Value.TranslatedText;
                                        tx.VersionUpdated = DateTime.UtcNow;
                                        tx.VersionUpdatedBy = contact;
                                    }
                                    if (!string.IsNullOrWhiteSpace(translation.Value.TranslatedText))
                                        d.SaveChanges();
                                }

                            }
                            d.SaveChanges();
                        }
                    }

                    m.TranslationResults = (from o in m.TranslationQueue select new TranslationViewModel { 
                        TranslationCulture = m.TranslationCulture,
                        TranslationText = o.Value.TranslatedText,
                        TranslationName = o.Value.TranslatedName,
                        id = o.Value.TranslationDataID,
                        DocID = o.Key,
                        DocName = o.Value.OriginalName
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
            return false;
        }

    }
}
