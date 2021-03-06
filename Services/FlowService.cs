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
using EXPEDIT.Flow.Models;

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
using Newtonsoft.Json.Linq;
using Orchard.Environment.Configuration;
using System.Web.Mvc;
using System.Data.Objects;



namespace EXPEDIT.Flow.Services {

    [UsedImplicitly]
    public class FlowService : IFlowService, Orchard.Users.Events.IUserEventHandler
    {

        public const string STAT_NAME_FLOW_ACCESS = "FlowAccess";
        public static Guid FLOW_MODEL_ID = new Guid("1DB0B648-D8A7-4FB9-8F3F-B2846822258C");
        public const string FS_FLOW_CONTACT_ID = "{0}:ValidFlowUser";
        public const string DASHBOARD_COMPANY = "{0}:COMPANYDASH";


        private readonly IAutomationService _automation;
        private readonly IUsersService _users;
        private readonly IOrchardServices _services;
        private readonly ShellSettings _shellSettings;
        public ILogger Logger { get; set; }

        public FlowService(
            IOrchardServices orchardServices,
            IUsersService users,
            IAutomationService automation,
            ShellSettings shellSettings
            )
        {
            _shellSettings = shellSettings;
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



        public object[] Report()
        {
            var contact = _users.ContactID;

            var report1 = new List<Tuple<string, decimal?, decimal?>>();
            var report2 = new List<Tuple<string, int?, decimal?>>();
            var report3 = new List<Tuple<string, decimal?, decimal?>>();
            var report4 = new List<Tuple<string, decimal?>>();
            using (var con = new SqlConnection(_users.ApplicationConnectionString))
            using (var cmd = new SqlCommand("E_SP_GetWorkflowData", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                //query, contact, application, s, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, start, pageSize, verified, found
                var qP = cmd.CreateParameter();
                qP.ParameterName = "@contactid";
                qP.DbType = DbType.Guid;
                qP.Value = contact;
                cmd.Parameters.Add(qP);

                con.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            report1.Add(new Tuple<string, decimal?, decimal?>(reader[0] as string, reader[1] as decimal?, reader[2] as decimal?));
                        }

                        if (reader.NextResult())
                        {
                            while (reader.Read())
                                report2.Add(new Tuple<string, int?, decimal?>(reader[0] as string, reader[1] as int?, reader[2] as decimal?));
                        }

                        if (reader.NextResult())
                        {
                            while (reader.Read())
                                report3.Add(new Tuple<string, decimal?, decimal?>(reader[0] as string, reader[1] as decimal?, reader[2] as decimal?));
                        }

                        if (reader.NextResult())
                        {
                            while (reader.Read())
                                report4.Add(new Tuple<string, decimal?>(reader[0] as string, reader[1] as decimal?));
                        }

                    }
                }
                con.Close();

            }
            
            return new object[] { report1.ToArray(), report2.ToArray(), report3.ToArray(), report4.ToArray() };

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
                    m = (from o in d.GraphData.Where(f=>f.Version == 0 && f.VersionDeletedBy == null)
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
                    return d.GraphData.Any(f => f.Version == 0 && f.VersionDeletedBy == null && (f.GraphName == wikiName && (f.VersionOwnerCompanyID == company || f.VersionOwnerCompanyID == null)));
                else
                    return d.GraphData.Any(f => f.Version == 0 && f.VersionDeletedBy == null && (f.GraphName == wikiName && (f.VersionOwnerCompanyID == company || f.VersionOwnerCompanyID == creatorCompany)));

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
                if (id.HasValue && d.GraphDataGroups.Where(f => f.Version == 0 && f.VersionDeletedBy == null && f.GraphDataGroupID == id.Value && f.GraphDataGroupName == workflowName).Any())
                    return false;
                if (!creatorCompany.HasValue)
                    return d.GraphDataGroups.Any(f => f.Version == 0 && f.VersionDeletedBy == null && (f.GraphDataGroupName == workflowName && (f.VersionOwnerCompanyID == company || f.VersionOwnerCompanyID == null)));
                else
                    return d.GraphDataGroups.Any(f => f.Version == 0 && f.VersionDeletedBy == null && (f.GraphDataGroupName == workflowName && (f.VersionOwnerCompanyID == company || f.VersionOwnerCompanyID == creatorCompany)));
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
                var proxy = XmlRpcProxyGen.Create<ICheckPayment>();
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
                        (from o in d.GraphDataFileDatas.Where(f => f.Version == 0 && f.VersionDeletedBy == null) where !files.Contains(o.FileDataID.Value) && o.GraphDataID == m.GraphDataID.Value select o).Delete(); //Delete redundant links
                    else
                        (from o in d.GraphDataFileDatas.Where(f => f.Version == 0 && f.VersionDeletedBy == null) where o.GraphDataID == m.GraphDataID.Value select o).Delete(); //Delete redundant links
                    var fd = (from o in d.GraphDataFileDatas.Where(f => f.Version == 0 && f.VersionDeletedBy == null) where o.GraphDataID == m.GraphDataID select o).AsEnumerable();
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
                    (from o in d.GraphDataLocations.Where(f => f.Version == 0 && f.VersionDeletedBy == null) where !locations.Contains(o.LocationID.Value) && o.GraphDataID == m.GraphDataID.Value select o).Delete(); //Delete redundant links
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
                    (from o in d.GraphDataLocations.Where(f => f.Version == 0 && f.VersionDeletedBy == null) where o.GraphDataID == m.GraphDataID select o).Delete();
                    (from o in d.GraphDataFileDatas.Where(f => f.Version == 0 && f.VersionDeletedBy == null) where o.GraphDataID == m.GraphDataID select o).Delete();
                    //(from o in d.GraphDataHistories where o.GraphDataID == m.GraphDataID select o).Delete();
                    //(from o in d.TriggerGraphs where o.GraphDataID == m.GraphDataID select o).Delete();
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
                (from o in d.GraphDataLocations.Where(f => f.Version == 0 && f.VersionDeletedBy == null) where o.GraphDataID == m.GraphDataID select o).Delete();
                (from o in d.GraphDataFileDatas.Where(f => f.Version == 0 && f.VersionDeletedBy == null) where o.GraphDataID == m.GraphDataID select o).Delete();
                //(from o in d.GraphDataHistories where o.GraphDataID == m.GraphDataID select o).Delete();
                //(from o in d.TriggerGraphs where o.GraphDataID == m.GraphDataID select o).Delete();
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
                        var firstProcess = !(from o in d.GraphDataRelation.Where(f => f.Version == 0 && f.VersionDeletedBy == null) where o.GraphDataGroupID == gid.Value select o).Any();
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
                var g = (from o in d.GraphData.Where(f => f.Version == 0 && f.VersionDeletedBy == null) where o.GraphDataID == m.GraphDataID select o).Single();
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
                var g = (from o in d.GraphData.Where(f => f.Version==0 && f.VersionDeletedBy==null) where o.GraphDataID == mid select o).Single();                
                d.GraphDataRelation.Where(f => f.Version == 0 && f.VersionDeletedBy == null && f.FromGraphDataID == mid || f.ToGraphDataID == mid).Delete();
                //Don't delete the node - just its identity in the wf
                //d.GraphDataHistories.Where(f => f.Version == 0 && f.VersionDeletedBy == null && f.GraphDataID == mid).Delete();
                //d.TriggerGraphs.Where(f => f.Version == 0 && f.VersionDeletedBy == null && f.GraphDataID == mid).Delete();
                //d.ProjectPlanTaskResponses.Where(f => f.Version == 0 && f.VersionDeletedBy == null && f.ActualGraphDataID == mid).Delete();
                //d.Tasks.Where(f => f.Version == 0 && f.VersionDeletedBy == null && f.GraphDataID == mid).Delete();
                //d.GraphDataFileDatas.Where(f => f.Version == 0 && f.VersionDeletedBy == null && f.GraphDataID == mid).Delete();
                //d.GraphDataLocations.Where(f => f.Version == 0 && f.VersionDeletedBy == null && f.GraphDataID == mid).Delete();                            
                //d.GraphData.DeleteObject(g);
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
                var g = (from o in d.GraphData.Where(f => f.Version == 0 && f.VersionDeletedBy == null) where o.GraphDataID == mid select o).Single();
                if (gid.HasValue)
                    d.GraphDataRelation.Where(f => f.Version==0 && f.VersionDeletedBy==null && (f.FromGraphDataID == mid || f.ToGraphDataID == mid) && (f.GraphDataRelationID == gid.Value)).Delete();
                else
                    d.GraphDataRelation.Where(f => f.Version==0 && f.VersionDeletedBy==null && (f.FromGraphDataID == mid || f.ToGraphDataID == mid)).Delete();
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
                var wf = d.GraphDataGroups.FirstOrDefault(f=>f.GraphDataGroupID == m.GroupID && f.StartGraphDataID == null);
                if (wf != null)
                    wf.StartGraphDataID = m.FromID;
                d.SaveChanges();

                return true;
            }
        }
        public bool UpdateEdge(FlowEdgeViewModel m)
        {

            try
            {
                var contact = _users.ContactID;
                var now = DateTime.UtcNow;
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(m.id.Value, ActionPermission.Update, typeof(GraphDataRelation)))
                        return false;
                    //Update
                    var gdrc = (from o in d.GraphDataRelation.Where(f => f.Version == 0 && f.VersionDeletedBy == null) where o.GraphDataRelationID == m.id && o.VersionDeletedBy == null select o).Single();
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
                var edge = (from o in d.GraphDataRelation.Where(f => f.Version == 0 && f.VersionDeletedBy == null) where o.GraphDataRelationID == mid select o).SingleOrDefault();
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
                return (from o in d.GraphDataGroups.Where(f => f.Version == 0 && f.VersionDeletedBy == null)
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
                var obj = (from o in d.GraphDataGroups.Where(f => f.Version==0 && f.VersionDeletedBy==null) where o.GraphDataGroupID == m.GraphDataGroupID select o).SingleOrDefault();
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
                    var delete = (from o in d.SecurityWhitelists.Where(f => f.Version==0 && f.VersionDeletedBy==null)
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
                    var delete = (from o in d.SecurityWhitelists.Where(f => f.Version==0 && f.VersionDeletedBy==null)
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
                    var delete = (from o in d.SecurityBlacklists.Where(f => f.Version==0 && f.VersionDeletedBy==null)
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
            var myCompany = _users.DefaultContactCompany;
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
                    ShortBiography = r.ShortBiography,
                    DefaultCompanyID = myCompany.Item1,
                    DefaultCompanyName = myCompany.Item2
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
            var now = DateTime.UtcNow;
            var contact = _users.ContactID;
            var company = _users.DefaultContactCompanyID;
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
                if (m.DefaultCompanyID.HasValue && m.DefaultCompanyID != company && d.E_UDF_ContactCompanies(contact).Contains(m.DefaultCompanyID.Value)) //First check we are in our list
                {                    
                    //Then update if exists (versionupdated)
                    var exp = (from o in d.Experiences.Where(f => f.Version == 0 && f.VersionDeletedBy == null
                                            && (f.DateFinished == null || f.DateFinished > DateTime.UtcNow)
                                            && (f.Expiry == null || f.Expiry > DateTime.UtcNow)
                                            && (f.DateStart <= DateTime.UtcNow || f.DateStart == null)
                                           && f.CompanyID == m.DefaultCompanyID.Value && f.ContactID == contact)
                               orderby  o.VersionUpdated descending
                               select o).FirstOrDefault();
                    if (exp != null)
                    {
                        exp.VersionUpdated = now;
                        exp.VersionUpdatedBy = contact;
                    }
                    else
                    {
                        //Else add to experience
                        var cn = d.Companies.Where(f => f.Version == 0 && f.VersionDeletedBy == null && f.CompanyID == m.DefaultCompanyID.Value)
                            .Select(f => f.CompanyName).Single();
                        exp = new Experience
                        {
                            ExperienceID = Guid.NewGuid(),
                            ExperienceName = string.Format("{0} - {1}", cn, m.Username),
                            ContactID = contact,
                            CompanyID = m.DefaultCompanyID.Value,
                            DateStart = now,
                            VersionUpdated = now,
                            VersionUpdatedBy = contact,
                            VersionOwnerContactID = contact,
                            VersionOwnerCompanyID = m.DefaultCompanyID.Value
                        };
                        d.Experiences.AddObject(exp);
                    }
                }
                d.SaveChanges();
                return true;
            }           
        }

        public void Creating(UserContext context) { }

        public void Created(UserContext context) { }

        public void LoggedIn(IUser user)
        {
            CacheHelper.Cache.Remove(string.Format(FS_FLOW_CONTACT_ID, _users.ContactID));
            var ckd = CheckPayment();
            using (new TransactionScope(TransactionScopeOption.Suppress))
                if (_users.ContactID.HasValue && ckd && !_users.HasPrivateCompanyID)
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
                else if (_users.ContactID.HasValue && !ckd && !_users.HasPrivateCompanyID)
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    var cid = Guid.NewGuid();
                    var now = DateTime.UtcNow;
                    var c = new Company
                    {
                        CompanyID = cid,
                        CompanyName = "Company Name -" + cid.ToString(),
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
                    d.E_SP_CreateLicense(_users.ContactID, cid, _users.Username, null, null, null, null, DateTime.UtcNow.AddDays(-1.0), DateTime.UtcNow.AddDays(EXPEDIT.Share.Helpers.ConstantsHelper.APP_LICENSE_DEFAULTGIFTDAYS));
                }

        }

        public void LoggedOut(IUser user) { }

        public void AccessDenied(IUser user) { }

        public void ChangedPassword(IUser user) { }

        public void SentChallengeEmail(IUser user) { }

        public void ConfirmedEmail(IUser user) { }

        public void Approved(IUser user) { }

        

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

        public AutomationViewModel GetStep(Guid? sid, Guid? pid, Guid? tid, Guid? nid, Guid? gid, bool includeContent = false, bool includeDisconnected = false, bool monitor = true, string locale="en-US")
        {
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    Guid? taskID = null;
                    AutomationViewModel m = null;
                    if (sid.HasValue)
                        m = _automation.GetStep(d, sid.Value, tid, includeContent, locale);
                    //New Step
                    if (m == null)
                    {
                        m = new AutomationViewModel {};
                        m.IncludeContent = includeContent;
                        m.Locale = locale;
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
                    if (!CheckPermission(null, ActionPermission.Create, typeof(ProjectPlanTaskResponse)))
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
                        pdt = (from o in d.ProjectDataTemplates.Where(f => f.Version==0 && f.VersionDeletedBy==null) where o.ProjectDataTemplateID == m.ProjectDataTemplateID.Value select o).FirstOrDefault();
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
                    var pd = (from o in d.ProjectDatas where o.ProjectDataID == m.id && o.Version == 0 && o.VersionDeletedBy == null select o).Single();
                    if (pd.Value != m.Value)
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
                    var pd = (from o in d.ProjectDatas where o.ProjectDataID == m.id && o.Version == 0 && o.VersionDeletedBy == null select o).Single();
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
                var now = DateTime.UtcNow;
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(m.id.Value, ActionPermission.Update, typeof(GraphDataRelationCondition)))
                        return false;
                    //Update
                    var gdrc = (from o in d.GraphDataRelationConditions where o.GraphDataRelationConditionID == m.id &&o.Version ==0 && o.VersionDeletedBy == null select o).Single();
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
                    var pd = (from o in d.GraphDataRelationConditions where o.GraphDataRelationConditionID == m.id && o.Version == 0 && o.VersionDeletedBy == null select o).Single();
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
                    return (from o in d.GraphDataRelation.Where(f => f.Version==0 && f.VersionDeletedBy==null && f.GraphDataGroupID == wfid)
                            join n in d.GraphData.Where(f => f.Version==0 && f.VersionDeletedBy==null) on o.FromGraphDataID equals n.GraphDataID
                            join c in d.ProjectDataTemplates.Where(f => f.Version==0 && f.VersionDeletedBy==null) on n.GraphDataID equals c.ReferenceID
                            where c.TableType == table
                            select c
                            ).Union(
                            from o in d.GraphDataRelation.Where(f => f.Version == 0 && f.VersionDeletedBy == null && f.GraphDataGroupID == wfid)
                            join n in d.GraphData.Where(f => f.Version==0 && f.VersionDeletedBy==null) on o.ToGraphDataID equals n.GraphDataID
                            join c in d.ProjectDataTemplates.Where(f => f.Version==0 && f.VersionDeletedBy==null) on n.GraphDataID equals c.ReferenceID
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
                                 Precondition = c.Condition
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
                        Condition = m.Precondition,
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
                var now = DateTime.UtcNow;
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(m.id.Value, ActionPermission.Update, typeof(Precondition)))
                        return false;
                    //Update
                    var cc = (from o in d.Precondition where o.ConditionID == m.id && o.Version==0 && o.VersionDeletedBy == null select o).Single();
                    if (m.JSON != null && cc.JSON != m.JSON)
                        cc.JSON = m.JSON;
                    if (m.Precondition != null && cc.Condition != m.Precondition)
                        cc.Condition = m.Precondition;
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
                    var pd = (from o in d.Precondition where o.ConditionID == m.id && o.Version==0 && o.VersionDeletedBy == null select o).Single();                    
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
            if (!CheckPermission(null, ActionPermission.Create, typeof(Task)))
                return false;
            var contact = _users.ContactID;            
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
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
                var now = DateTime.UtcNow;
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(m.id.Value, ActionPermission.Update, typeof(Task)))
                        return false;
                    //Update
                    var cc = (from o in d.Tasks where o.TaskID == m.id && o.Version == 0 && o.VersionDeletedBy == null select o).Single();
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
                    if (cc.EntityState == EntityState.Modified)
                    {
                        cc.VersionUpdated = DateTime.UtcNow;
                        cc.VersionUpdatedBy = contact;
                    }
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
                    var pd = (from o in d.Tasks where o.TaskID == m.id && o.Version == 0 && o.VersionDeletedBy == null select o).Single();
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


        public TriggerViewModel GetTrigger(string commonName)
        {
            try
            {
                var contact = _users.ContactID;
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    //We are only checking the owner contacts...so dont need perms here
                    //if (!CheckPermission(id, ActionPermission.Read, typeof(Trigger)))
                    //    return null;
                    var m = (from o in d.Triggers.Where(h => h.Version == 0 && h.VersionDeletedBy == null && h.VersionOwnerContactID == contact
                                 && (
                                    (h.CommonName == commonName && commonName != null) 
                                    ||
                                    (h.CommonName == null && commonName == null) 
                                    )
                                 )
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
                                 DelaySeconds = o.DelaySeconds,
                                 DelayDays = o.DelayDays,
                                 DelayWeeks = o.DelayWeeks,
                                 DelayMonths = o.DelayMonths,
                                 DelayYears = o.DelayYears,
                                 DelayUntil = o.DelayUntil,
                                 RepeatAfterDays = o.RepeatAfterDays,
                                 Repeats = o.Repeats,
                             }).SingleOrDefault();
                    return m;
                }

            }
            catch (Exception ex)
            {
                return null;
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
                                //Commented unsafe data
                                //JsonProxyApplicationID = o.JsonProxyApplicationID,
                                //JsonProxyContactID = o.JsonProxyContactID,
                                //JsonProxyCompanyID = o.JsonProxyCompanyID,
                                //JsonAuthorizedBy = o.JsonAuthorizedBy,
                                //JsonUsername = o.JsonUsername,
                                //JsonPassword = o.JsonPassword,
                                //JsonPasswordType = o.JsonPasswordType,
                                JSON = o.JSON,
                                SystemMethod = o.SystemMethod,
                                ConditionID = o.ConditionID,
                                ExternalURL = o.ExternalURL,
                                ExternalRequestMethod = o.ExternalRequestMethod,
                                ExternalFormType = o.ExternalFormType,
                                PassThrough = o.PassThrough,
                                //TriggerGraphID = o.TriggerGraphID,
                                //GraphDataID = o.GraphDataID,
                                //GraphDataGroupTriggerID = o.GraphDataGroupTriggerID,
                                //GraphDataGroupID = o.GraphDataGroupID,
                                //MergeProjectData = o.MergeProjectData,
                                //OnEnter = o.OnEnter,
                                //OnDataUpdate = o.OnDataUpdate,
                                //OnExit = o.OnExit,
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
            var company = _users.DefaultContactCompanyID;
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(null, ActionPermission.Create, typeof(Trigger)))
                        return false;
                    var runningApplicationID = (from o in d.Applications where o.ApplicationName == _shellSettings.Name select o.ApplicationId).FirstOrDefault();
                    if (runningApplicationID == Guid.Empty || runningApplicationID == null)
                        return false;
                    var c = new Trigger
                    {
                        TriggerID = m.id.Value,
                        CommonName = m.CommonName,
                        TriggerTypeID = m.TriggerTypeID,
                        JsonMethod = m.JsonMethod ?? m.CommonName,
                        JsonProxyApplicationID = runningApplicationID,
                        JsonProxyContactID = contact,
                        JsonProxyCompanyID = company,
                        JsonAuthorizedBy = m.JsonAuthorizedBy,
                        JsonUsername = m.JsonUsername,
                        JsonPassword = m.JsonPassword,
                        JsonPasswordType = m.JsonPasswordType ?? "TEXT",
                        JSON = m.JSON,
                        SystemMethod = m.SystemMethod ?? m.CommonName,
                        ConditionID = m.ConditionID,
                        ExternalURL = m.ExternalURL,
                        ExternalRequestMethod = m.ExternalRequestMethod,
                        ExternalFormType = m.ExternalFormType,
                        PassThrough = m.PassThrough,
                        DelaySeconds = m.DelaySeconds,
                        DelayDays = m.DelayDays,
                        DelayWeeks = m.DelayWeeks,
                        DelayMonths = m.DelayMonths,
                        DelayYears = m.DelayYears,
                        DelayUntil = m.DelayUntil,
                        RepeatAfterDays = m.RepeatAfterDays,
                        Repeats = m.Repeats,
                        VersionUpdated = DateTime.UtcNow,
                        VersionUpdatedBy = contact,
                        VersionOwnerContactID = contact
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
                var now = DateTime.UtcNow;
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(m.id.Value, ActionPermission.Update, typeof(Trigger)))
                        return false;
                    //Update
                    var cc = (from o in d.Triggers where o.TriggerID == m.id && o.Version == 0 && o.VersionDeletedBy == null select o).Single();
                    var method = string.Format("{0}", m.CommonName).Trim().ToUpperInvariant();
                    if (cc.VersionOwnerContactID != contact && string.IsNullOrWhiteSpace(method))
                        return false;
                    //Commented unsafe updates AG
                    //if (m.CommonName != null && cc.CommonName != m.CommonName) cc.CommonName = m.CommonName;
                    //if (m.TriggerTypeID != null && cc.TriggerTypeID != m.TriggerTypeID) cc.TriggerTypeID = m.TriggerTypeID;
                    //if (m.JsonMethod != null && cc.JsonMethod != m.JsonMethod) cc.JsonMethod = m.JsonMethod;
                    //if (m.JsonProxyApplicationID != null && cc.JsonProxyApplicationID != m.JsonProxyApplicationID) cc.JsonProxyApplicationID = m.JsonProxyApplicationID;
                    //if (m.JsonProxyContactID != null && cc.JsonProxyContactID != m.JsonProxyContactID) cc.JsonProxyContactID = m.JsonProxyContactID;
                    //if (m.JsonProxyCompanyID != null && cc.JsonProxyCompanyID != m.JsonProxyCompanyID) cc.JsonProxyCompanyID = m.JsonProxyCompanyID;
                    if (m.JsonAuthorizedBy != null && cc.JsonAuthorizedBy != m.JsonAuthorizedBy) cc.JsonAuthorizedBy = m.JsonAuthorizedBy;
                    if (m.JsonUsername != null && cc.JsonUsername != m.JsonUsername) cc.JsonUsername = m.JsonUsername;
                    if (m.JsonPassword != null && cc.JsonPassword != m.JsonPassword) cc.JsonPassword = m.JsonPassword;
                    if (m.JsonPasswordType != null && cc.JsonPasswordType != m.JsonPasswordType) cc.JsonPasswordType = m.JsonPasswordType;
                    if (m.JSON != null && cc.JSON != m.JSON) cc.JSON = m.JSON;
                    //if (m.SystemMethod != null && cc.SystemMethod != m.SystemMethod) cc.SystemMethod = m.SystemMethod;
                    if (m.ConditionID != null && cc.ConditionID != m.ConditionID) cc.ConditionID = m.ConditionID;
                    if (m.ExternalURL != null && cc.ExternalURL != m.ExternalURL) cc.ExternalURL = m.ExternalURL;
                    if (m.ExternalRequestMethod != null && cc.ExternalRequestMethod != m.ExternalRequestMethod) cc.ExternalRequestMethod = m.ExternalRequestMethod;
                    if (m.ExternalFormType != null && cc.ExternalFormType != m.ExternalFormType) cc.ExternalFormType = m.ExternalFormType;
                    if (m.PassThrough != null && cc.PassThrough != m.PassThrough) cc.PassThrough = m.PassThrough;
                    if (m.DelaySeconds != null && cc.DelaySeconds != m.DelaySeconds) cc.DelaySeconds = m.DelaySeconds;
                    if (m.DelayDays != null && cc.DelayDays != m.DelayDays) cc.DelayDays = m.DelayDays;
                    if (m.DelayWeeks != null && cc.DelayWeeks != m.DelayWeeks) cc.DelayWeeks = m.DelayWeeks;
                    if (m.DelayMonths != null && cc.DelayMonths != m.DelayMonths) cc.DelayMonths = m.DelayMonths;
                    if (m.DelayYears != null && cc.DelayYears != m.DelayYears) cc.DelayYears = m.DelayYears;
                    if (m.DelayUntil != null && cc.DelayUntil != m.DelayUntil) cc.DelayUntil = m.DelayUntil;
                    if (m.RepeatAfterDays != null && cc.RepeatAfterDays != m.RepeatAfterDays) cc.RepeatAfterDays = m.RepeatAfterDays;
                    if (m.Repeats != null && cc.Repeats != m.Repeats) cc.Repeats = m.Repeats;

                    if (cc.EntityState == EntityState.Modified)
                    {
                        cc.VersionUpdated = DateTime.UtcNow;
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
                    var pd = (from o in d.Triggers where o.TriggerID == m.id && o.Version == 0 && o.VersionDeletedBy == null select o).Single();
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



        public void Cleanup()
        {
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                var cleanBefore = DateTime.UtcNow.AddDays(-1);
                var oldWorkflows = (from o in d.GraphDataGroups
                                    where !(from r in d.GraphDataRelation select r.GraphDataGroupID).Contains(o.GraphDataGroupID)
                                    && o.VersionUpdated < cleanBefore
                                    select o);
                if (oldWorkflows.Any())
                    oldWorkflows.Delete();               
            }

        }




        public bool GetTriggerGraph(TriggerGraphViewModel m)
        {
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    if (m.GraphDataID == null)
                        return false;
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(m.GraphDataID, ActionPermission.Read, typeof(GraphData)))
                        return false;
                     m.triggerGraphs = (from o in d.Triggers.Where(h => h.Version == 0 && h.VersionDeletedBy == null)
                                            join gt in d.TriggerGraphs.Where(f=>f.Version == 0 && f.VersionDeletedBy == null)
                                            on o.TriggerID equals gt.TriggerID
                                            join c in d.Precondition.Where(f=>f.Version == 0 && f.VersionDeletedBy == null)
                                            on o.ConditionID equals c.ConditionID into cond
                                            from condition in cond.DefaultIfEmpty() 
                                            where gt.GraphDataID == m.GraphDataID && gt.GraphDataGroupID == m.GraphDataGroupID
                                           
                             select new TriggerGraphViewModel
                             {
                                 id = gt.TriggerGraphID,                                 
                                 TriggerID = o.TriggerID,
                                 CommonName = o.CommonName,
                                 TriggerTypeID = o.TriggerTypeID,
                                 JsonMethod = o.JsonMethod,
                                 JSON = o.JSON,
                                 SystemMethod = o.SystemMethod,
                                 ExternalURL = o.ExternalURL,
                                 ExternalRequestMethod = o.ExternalRequestMethod,
                                 ExternalFormType = o.ExternalFormType,
                                 TriggerGraphID = gt.TriggerGraphID,
                                 GraphDataID = gt.GraphDataID,
                                 GraphDataGroupID = gt.GraphDataGroupID,
                                 MergeProjectData = gt.MergeProjectData,
                                 OnEnter = gt.OnEnter,
                                 OnDataUpdate = gt.OnDataUpdate,
                                 OnExit = gt.OnExit,
                                 PassThrough = o.PassThrough,
                                 DelaySeconds = o.DelaySeconds,
                                 DelayDays = o.DelayDays,
                                 DelayWeeks = o.DelayWeeks,
                                 DelayMonths = o.DelayMonths,
                                 DelayYears = o.DelayYears,
                                 DelayUntil = o.DelayUntil,
                                 RepeatAfterDays = o.RepeatAfterDays,
                                 Repeats = o.Repeats,
                                 ConditionID = o.ConditionID,
                                 Condition =  (condition != null) ? condition.Condition : null,
                                 ConditionJSON = (condition != null) ? condition.JSON : null,
                                 OverrideProjectDataWithJsonCustomVars = (condition != null) ? condition.OverrideProjectDataWithJsonCustomVars : null
                             }).ToArray();
                    return true;
                }

            }
            catch (Exception ex)
            {
                return false;
            }
        }


        public bool CreateTriggerGraph(TriggerGraphViewModel m)
        {
            var contact = _users.ContactID;
            var company = _users.DefaultContactCompanyID;
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(null, ActionPermission.Create, typeof(Trigger)))
                        return false;
                    var runningApplicationID = (from o in d.Applications where o.ApplicationName == _shellSettings.Name select o.ApplicationId).FirstOrDefault();
                    if (runningApplicationID == Guid.Empty || runningApplicationID == null)
                        return false;

                    Precondition c  = null;

                    if (!string.IsNullOrWhiteSpace(m.Condition))
                    {
                        if (m.ConditionID.HasValue && d.Precondition.Any(f =>f.Version == 0 && f.VersionDeletedBy == null && f.ConditionID == m.ConditionID))
                        {
                            //Update
                            c = d.Precondition.Where(f => f.Version == 0 && f.VersionDeletedBy == null && f.ConditionID == m.ConditionID).Single();
                            if (m.Condition != c.Condition || m.JSON != c.JSON || m.OverrideProjectDataWithJsonCustomVars != c.OverrideProjectDataWithJsonCustomVars)
                            {
                                c.Condition = m.Condition;
                                c.JSON = m.ConditionJSON;
                                c.OverrideProjectDataWithJsonCustomVars = m.OverrideProjectDataWithJsonCustomVars;
                                c.VersionUpdated = DateTime.UtcNow;
                                c.VersionUpdatedBy = contact;
                            }
                        }
                        else
                        {
                            //Insert
                            c = new Precondition
                            {
                                ConditionID = m.ConditionID ?? Guid.NewGuid(),
                                Condition = m.Condition,
                                JSON = m.ConditionJSON,
                                OverrideProjectDataWithJsonCustomVars = m.OverrideProjectDataWithJsonCustomVars,
                                VersionUpdated = DateTime.UtcNow,
                                VersionUpdatedBy = contact
                            };
                            d.Precondition.AddObject(c);
                        }
                    }

                    var t = new Trigger
                    {
                        TriggerID = m.TriggerID ?? Guid.NewGuid(),
                        CommonName = m.id.Value.ToString(),
                        TriggerTypeID = m.TriggerTypeID,
                        JsonMethod = m.JsonMethod ?? m.CommonName,
                        //Removed for security
                        //JsonProxyApplicationID = runningApplicationID,
                        //JsonProxyContactID = contact,
                        //JsonProxyCompanyID = company,
                        //JsonAuthorizedBy = m.JsonAuthorizedBy,
                        //JsonUsername = m.JsonUsername,
                        //JsonPassword = m.JsonPassword,
                        //JsonPasswordType = m.JsonPasswordType ?? "TEXT",
                        JSON = m.JSON,
                        SystemMethod = "USER", // m.SystemMethod ?? m.CommonName,
                        ConditionID = ((c != null) ? c.ConditionID : default(Guid?)),
                        ExternalURL = m.ExternalURL,
                        ExternalRequestMethod = m.ExternalRequestMethod,
                        ExternalFormType = m.ExternalFormType,
                        PassThrough = m.PassThrough,    
                        DelaySeconds = m.DelaySeconds,
                        DelayDays = m.DelayDays,
                        DelayWeeks = m.DelayWeeks,
                        DelayMonths = m.DelayMonths,
                        DelayYears = m.DelayYears,
                        DelayUntil = m.DelayUntil,
                        RepeatAfterDays = m.RepeatAfterDays,
                        Repeats = m.Repeats,
                        VersionUpdated = DateTime.UtcNow,
                        VersionUpdatedBy = contact
                    };
                    d.Triggers.AddObject(t);
                    var g = new TriggerGraph
                    {
                        TriggerGraphID = m.id.Value,
                        TriggerID = t.TriggerID,
                        GraphDataID = m.GraphDataID,
                        GraphDataGroupID = m.GraphDataGroupID,
                        MergeProjectData = m.MergeProjectData,
                        OnDataUpdate = m.OnDataUpdate,
                        OnEnter = m.OnEnter,
                        OnExit = m.OnExit,
                        VersionUpdated = DateTime.UtcNow,
                        VersionUpdatedBy = contact
                    };
                    d.TriggerGraphs.AddObject(g);
                    d.SaveChanges();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool UpdateTriggerGraph(TriggerGraphViewModel m)
        {

            try
            {
                var contact = _users.ContactID;
                var now = DateTime.UtcNow;
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    var gt = (from o in d.TriggerGraphs.Where(f=>f.TriggerGraphID == m.id.Value && f.Version==0 && f.VersionDeletedBy == null)
                                   select o).Single();
                    if (gt == null || gt.GraphDataID == null)
                        return false;
                    if (!CheckPermission(gt.GraphDataID, ActionPermission.Update, typeof(GraphData)))
                        return false;
                    //Update
                    var cc = (from o in d.Triggers where o.TriggerID == gt.TriggerID && o.VersionDeletedBy == null select o).Single();
                    //Commented unsafe updates AG
                    //if (m.CommonName != null && cc.CommonName != m.CommonName) cc.CommonName = m.CommonName;
                    if (m.TriggerTypeID != null && cc.TriggerTypeID != m.TriggerTypeID) cc.TriggerTypeID = m.TriggerTypeID;
                    if (m.JsonMethod != null && cc.JsonMethod != m.JsonMethod) cc.JsonMethod = m.JsonMethod;
                    //if (m.JsonProxyApplicationID != null && cc.JsonProxyApplicationID != m.JsonProxyApplicationID) cc.JsonProxyApplicationID = m.JsonProxyApplicationID;
                    //if (m.JsonProxyContactID != null && cc.JsonProxyContactID != m.JsonProxyContactID) cc.JsonProxyContactID = m.JsonProxyContactID;
                    //if (m.JsonProxyCompanyID != null && cc.JsonProxyCompanyID != m.JsonProxyCompanyID) cc.JsonProxyCompanyID = m.JsonProxyCompanyID;
                    //if (m.JsonAuthorizedBy != null && cc.JsonAuthorizedBy != m.JsonAuthorizedBy) cc.JsonAuthorizedBy = m.JsonAuthorizedBy;
                    //if (m.JsonUsername != null && cc.JsonUsername != m.JsonUsername) cc.JsonUsername = m.JsonUsername;
                    //if (m.JsonPassword != null && cc.JsonPassword != m.JsonPassword) cc.JsonPassword = m.JsonPassword;
                    //if (m.JsonPasswordType != null && cc.JsonPasswordType != m.JsonPasswordType) cc.JsonPasswordType = m.JsonPasswordType;
                    if (m.JSON != null && cc.JSON != m.JSON) cc.JSON = m.JSON;
                    //if (m.SystemMethod != null && cc.SystemMethod != m.SystemMethod) cc.SystemMethod = m.SystemMethod;                    
                    if (m.ExternalURL != null && cc.ExternalURL != m.ExternalURL) cc.ExternalURL = m.ExternalURL;
                    if (m.ExternalRequestMethod != null && cc.ExternalRequestMethod != m.ExternalRequestMethod) cc.ExternalRequestMethod = m.ExternalRequestMethod;
                    if (m.ExternalFormType != null && cc.ExternalFormType != m.ExternalFormType) cc.ExternalFormType = m.ExternalFormType;
                    if (m.PassThrough != null && cc.PassThrough != m.PassThrough) cc.PassThrough = m.PassThrough;
                    //if (m.GraphDataID != null && gt.GraphDataID != m.GraphDataID) gt.GraphDataID = m.GraphDataID;
                    //if (m.GraphDataGroupID != null && gt.GraphDataGroupID != m.GraphDataGroupID) gt.GraphDataGroupID = m.GraphDataGroupID;
                    if (m.MergeProjectData != null && gt.MergeProjectData != m.MergeProjectData) gt.MergeProjectData = m.MergeProjectData;
                    if (m.OnEnter != null && gt.OnEnter != m.OnEnter) gt.OnEnter = m.OnEnter;
                    if (m.OnDataUpdate != null && gt.OnDataUpdate != m.OnDataUpdate) gt.OnDataUpdate = m.OnDataUpdate;
                    if (m.OnExit != null && gt.OnExit != m.OnExit) gt.OnExit = m.OnExit;
                    if (m.DelaySeconds != null && cc.DelaySeconds != m.DelaySeconds) cc.DelaySeconds = m.DelaySeconds;
                    if (m.DelayDays != null && cc.DelayDays != m.DelayDays) cc.DelayDays = m.DelayDays;
                    if (m.DelayWeeks != null && cc.DelayWeeks != m.DelayWeeks) cc.DelayWeeks = m.DelayWeeks;
                    if (m.DelayMonths != null && cc.DelayMonths != m.DelayMonths) cc.DelayMonths = m.DelayMonths;
                    if (m.DelayYears != null && cc.DelayYears != m.DelayYears) cc.DelayYears = m.DelayYears;
                    if (m.DelayUntil != null && cc.DelayUntil != m.DelayUntil) cc.DelayUntil = m.DelayUntil;
                    if (m.RepeatAfterDays != null && cc.RepeatAfterDays != m.RepeatAfterDays) cc.RepeatAfterDays = m.RepeatAfterDays;                    
                    if (m.Repeats != null && cc.Repeats != m.Repeats) cc.Repeats = m.Repeats;

                    if (m.Condition != null)
                    {
                        var condition = d.Precondition.Where(f => f.Version == 0 && f.VersionDeletedBy == null && f.ConditionID == m.ConditionID).FirstOrDefault();
                        if (cc.ConditionID == null)
                        {
                            if (condition == null)
                            {
                                var pc = new Precondition
                                {
                                    ConditionID = m.ConditionID ?? Guid.NewGuid(),
                                    Condition = m.Condition,
                                    JSON = m.ConditionJSON,
                                    OverrideProjectDataWithJsonCustomVars = m.OverrideProjectDataWithJsonCustomVars,
                                    VersionUpdated = DateTime.UtcNow,
                                    VersionUpdatedBy = contact
                                };
                                d.Precondition.AddObject(pc);
                            }
                            else
                            {
                                cc.ConditionID = condition.ConditionID;
                                if (condition.Condition != null && condition.Condition != m.Condition) condition.Condition = m.Condition;
                                if (condition.JSON != null && condition.JSON != m.ConditionJSON) condition.JSON = m.ConditionJSON;
                                if (condition.OverrideProjectDataWithJsonCustomVars != null && condition.OverrideProjectDataWithJsonCustomVars != m.OverrideProjectDataWithJsonCustomVars) condition.OverrideProjectDataWithJsonCustomVars = m.OverrideProjectDataWithJsonCustomVars;
                                
                            }
                        }
                        else
                        {
                            if (cc.Condition.Condition != null && cc.Condition.Condition != m.Condition) cc.Condition.Condition = m.Condition;
                            if (cc.Condition.JSON != null && cc.Condition.JSON != m.ConditionJSON) cc.Condition.JSON = m.ConditionJSON;
                            if (cc.Condition.OverrideProjectDataWithJsonCustomVars != null && cc.Condition.OverrideProjectDataWithJsonCustomVars != m.OverrideProjectDataWithJsonCustomVars) cc.Condition.OverrideProjectDataWithJsonCustomVars = m.OverrideProjectDataWithJsonCustomVars;
                        }
                    }

                    if (cc.EntityState == EntityState.Modified)
                    {
                        cc.VersionUpdated = DateTime.UtcNow;
                        cc.VersionUpdatedBy = contact;
                    }
                    if (cc.Condition != null && cc.Condition.EntityState == EntityState.Modified)
                    {
                        cc.Condition.VersionUpdated = DateTime.UtcNow;
                        cc.Condition.VersionUpdatedBy = contact;
                    }
                    if (gt.EntityState == EntityState.Modified)
                    {
                        gt.VersionUpdated = DateTime.UtcNow;
                        gt.VersionUpdatedBy = contact;
                    }
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

        public bool DeleteTriggerGraph(TriggerGraphViewModel m)
        {
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (m.id == null)
                        return false;
                    //Delete
                    var pd = (from o in d.TriggerGraphs where o.TriggerGraphID == m.id && o.Version==0 && o.VersionDeletedBy == null select o).Single();
                    if (!CheckPermission(pd.GraphDataID, ActionPermission.Delete, typeof(GraphData)))
                        return false;
                    if (pd.Trigger.Condition != null && d.Triggers.Where(f=>f.Version ==0 && f.VersionDeletedBy == null && f.ConditionID==pd.Trigger.ConditionID).Count() == 1)
                        d.Precondition.DeleteObject(pd.Trigger.Condition);
                    d.Triggers.DeleteObject(pd.Trigger);
                    d.TriggerGraphs.DeleteObject(pd);
                    d.SaveChanges();
                    return true;
                }

            }
            catch (Exception ex)
            {
                return false;
            }
        }







        public CompanyViewModel[] GetCompany(CompanyViewModel m)
        {
            try
            {
                var contact = _users.ContactID;
                var now = DateTime.UtcNow;
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    d.ContextOptions.LazyLoadingEnabled = false;
                    var companies = (from o in d.E_UDF_ContactCompanies(contact) select o).Except(new Guid?[]{ _users.ApplicationCompanyID}).ToArray(); //is recursive
                    var mycompanies = (from r in
                                           (from c in d.Companies.Where(f => f.Version == 0 && f.VersionDeletedBy == null
                                               && companies.Contains(f.CompanyID))
                                            join p in d.CompanyRelations.Where(f => f.Version == 0 && f.VersionDeletedBy == null)
                                            on c.CompanyID equals p.CompanyID into pc
                                            from parent in pc.DefaultIfEmpty()
                                            join e in d.Experiences.Where(f => f.Version == 0 && f.VersionDeletedBy == null
                                                && (f.DateFinished == null || f.DateFinished > DateTime.UtcNow)
                                                && (f.Expiry == null || f.Expiry > DateTime.UtcNow)
                                                && (f.DateStart <= DateTime.UtcNow || f.DateStart == null)
                                            )
                                            on c.CompanyID equals e.CompanyID into ex
                                            from experience in ex.DefaultIfEmpty()

                                            select new
                                            {
                                                id = c.CompanyID,
                                                ParentCompanyID = (parent != null) ? parent.ParentCompanyID : default(Guid?),
                                                CompanyName = c.CompanyName,
                                                CountryID = c.CountryID,
                                                PrimaryContactID = c.PrimaryContactID,
                                                Comment = c.Comment,
                                                ContactID = (experience != null) ? experience.ContactID : null
                                            }).ToArray()                                        
                                       group r by new { r.id, r.ParentCompanyID, r.CompanyName, r.CountryID, r.PrimaryContactID, r.Comment }
                                           into g                                           
                                           select new CompanyViewModel
                                           {
                                               id = g.Key.id,
                                               ParentCompanyID = g.Key.ParentCompanyID,
                                               CompanyName = g.Key.CompanyName,
                                               CountryID = g.Key.CountryID,
                                               PrimaryContactID = g.Key.PrimaryContactID,
                                               Comment = g.Key.Comment,
                                               PeopleEnum = g.Select(f => f.ContactID)
                                           });


                    return mycompanies.ToArray();
                }

            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public bool CreateCompany(CompanyViewModel m)
        {
            var contact = _users.ContactID;
            var company = _users.DefaultContactCompanyID;
            var now = DateTime.UtcNow;
            var old = new DateTime(1970, 1, 1);
            if (string.IsNullOrWhiteSpace(m.CompanyName) || !m.id.HasValue)
                return false;
            try
            {
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(null, ActionPermission.Create, typeof(Company)))
                        return false;
                    var c = new Company
                    {
                        CompanyID = m.id.Value,
                        CompanyName = m.CompanyName + "*",
                        CountryID = m.CountryID,
                        PrimaryContactID = contact,
                        Comment = m.Comment,
                        VersionUpdated = now,
                        VersionUpdatedBy = contact,
                        VersionOwnerContactID = contact,
                        VersionOwnerCompanyID = company
                    };
                    d.Companies.AddObject(c);
                    if (m.ParentCompanyID.HasValue && m.ParentCompanyID != m.id)
                    {
                        var cr = new CompanyRelation
                        {
                            CompanyRelationID = Guid.NewGuid(),
                            ParentCompanyID = m.ParentCompanyID.Value,
                            CompanyID = m.id.Value,
                            VersionOwnerContactID = contact,
                            VersionOwnerCompanyID = company
                        };
                        d.CompanyRelations.AddObject(cr);
                    }
                    if (m.PeopleArray == null)
                        m.PeopleArray = new Guid?[] { };
                    foreach (var added in m.PeopleArray.Union(new Guid?[] { contact }).ToArray())
                    {
                        var username = d.Contacts.Where(f => f.Version == 0 && f.VersionDeletedBy == null && f.ContactID == added).Select(f => f.Username).Single();
                        var exp = new Experience
                        {
                            ExperienceID = Guid.NewGuid(),
                            ExperienceName = string.Format("{0} - {1}", m.CompanyName, username),
                            ContactID = added,
                            CompanyID = m.id,
                            DateStart = now,
                            VersionUpdated = old,
                            VersionUpdatedBy = contact,
                            VersionOwnerContactID = contact,
                            VersionOwnerCompanyID = company
                        };
                        d.Experiences.AddObject(exp);
                    }

                    //Dashboard
                    if (!string.IsNullOrWhiteSpace(m.Dashboard))
                    {
                        var dashID = string.Format(DASHBOARD_COMPANY, m.id);
                        m.Dashboard = m.Dashboard.Trim();

                        var dash = new MetaData
                        {
                            MetaDataID = Guid.NewGuid(),
                            MetaDataType = dashID,
                            ContentToIndex = m.Dashboard,
                            VersionOwnerCompanyID = company,
                            VersionOwnerContactID = contact,
                            VersionUpdated = now,
                            VersionUpdatedBy = contact
                        };
                        d.MetaDatas.AddObject(dash);
                    }

                    d.SaveChanges();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool UpdateCompany(CompanyViewModel m)
        {

            try
            {
                if (!m.id.HasValue || m.id.Value == _users.ApplicationCompanyID)
                    return false;
                var old = new DateTime(1970, 1, 1);
                var contact = _users.ContactID;
                var company = _users.DefaultContactCompanyID;
                var now = DateTime.UtcNow;
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {
                    if (!CheckPermission(m.id, ActionPermission.Update, typeof(Company)))
                        return false;
                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    var c = d.Companies.Where(f=>f.Version == 0 && f.VersionDeletedBy == null && f.CompanyID == m.id).Single();
                    
                    //Update hierarchy
                    var oldRelations = (from o in d.CompanyRelations.Where(f => f.Version == 0 && f.VersionDeletedBy == null)
                                        where o.CompanyID == m.id
                                        select o);
                    if (oldRelations.Any())
                        oldRelations.Delete();
                    if (m.ParentCompanyID.HasValue && m.id != m.ParentCompanyID)
                    {
                        var cr = new CompanyRelation
                        {
                            CompanyRelationID = Guid.NewGuid(),
                            ParentCompanyID = m.ParentCompanyID.Value,
                            CompanyID = m.id.Value,
                            VersionUpdated = now,
                            VersionUpdatedBy = contact,
                            VersionOwnerContactID = contact,
                            VersionOwnerCompanyID = company
                        };
                        d.CompanyRelations.AddObject(cr);
                    }

                    if (m.CountryID != null && c.CountryID != m.CountryID) c.CountryID = m.CountryID;
                    if (m.Comment != null && c.Comment != m.Comment) c.Comment = m.Comment;
                    if (!string.IsNullOrWhiteSpace(m.CompanyName))
                    {
                        m.CompanyName = m.CompanyName.Trim();
                        if (m.CompanyName[m.CompanyName.Length - 1] != '*')
                            m.CompanyName += "*";
                        if (c.CompanyName != m.CompanyName) c.CompanyName = m.CompanyName;
                    }
                    if (c.EntityState == EntityState.Modified)
                    {
                        c.VersionUpdated = now;
                        c.VersionUpdatedBy = contact;
                    }

                    if (m.PeopleArray != null)
                    {
                        m.PeopleArray = m.PeopleArray.Union(new Guid?[] { contact }).ToArray();
                        //Add
                        var unknown = (from o in m.PeopleArray
                                       where
                                           !d.Experiences.Where(f => f.Version == 0 && f.VersionDeletedBy == null 
                                            && (f.DateFinished == null || f.DateFinished > DateTime.UtcNow)
                                            && (f.Expiry == null || f.Expiry > DateTime.UtcNow)
                                            && (f.DateStart <= DateTime.UtcNow || f.DateStart == null)
                                           && f.CompanyID == m.id).Select(f => f.ContactID).Contains(o)
                                       select o);
                        foreach (var added in unknown)
                        {
                            var username = d.Contacts.Where(f=>f.Version == 0 && f.VersionDeletedBy == null && f.ContactID == added).Select(f=>f.Username).Single();
                            var exp = new Experience
                            {
                                ExperienceID = Guid.NewGuid(),
                                ExperienceName = string.Format("{0} - {1}", c.CompanyName, username),
                                ContactID = added,
                                CompanyID = m.id,
                                DateStart = now,
                                VersionUpdated = old,
                                VersionUpdatedBy = contact,
                                VersionOwnerContactID = contact,
                                VersionOwnerCompanyID = company
                            };
                            d.Experiences.AddObject(exp);
                        }
                        //Remove
                        var retired = (from o in d.Experiences.Where(f => f.Version == 0 && f.VersionDeletedBy == null 
                                            && (f.DateFinished == null || f.DateFinished > DateTime.UtcNow)
                                            && (f.Expiry == null || f.Expiry > DateTime.UtcNow)
                                            && (f.DateStart <= DateTime.UtcNow || f.DateStart == null)
                                           && f.CompanyID == m.id) where !m.PeopleArray.Contains(o.ContactID) select o);
                        foreach (var retire in retired)
                        {
                            retire.DateFinished = now;
                            retire.Expiry = now;
                            retire.VersionUpdated = now;
                            retire.VersionUpdatedBy = contact;
                        }


                    }

                    var dashID = string.Format(DASHBOARD_COMPANY, m.id);
                    //Dashboard
                    var dash = (from o in d.MetaDatas where o.Version == 0 && o.VersionDeletedBy == null && o.MetaDataType == dashID select o).FirstOrDefault();
                    if (m.Dashboard != null)
                        m.Dashboard = m.Dashboard.Trim();
                    if (string.Empty == m.Dashboard)
                    {
                        if (dash != null)
                            d.MetaDatas.DeleteObject(dash);
                    }
                    else if (!string.IsNullOrWhiteSpace(m.Dashboard))
                    {                        
                        if (dash == null)
                        {
                            dash = new MetaData
                            {
                                MetaDataID = Guid.NewGuid(),
                                MetaDataType = dashID,
                                ContentToIndex = m.Dashboard,
                                VersionOwnerCompanyID = company,
                                VersionOwnerContactID = contact,
                                VersionUpdated = now,
                                VersionUpdatedBy = contact
                            };
                            d.MetaDatas.AddObject(dash);
                        }
                        else if (dash.ContentToIndex != m.Dashboard)
                        {
                            dash.ContentToIndex = m.Dashboard;
                            dash.VersionUpdated = now;
                            dash.VersionUpdatedBy = contact;
                        }
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

        public bool DeleteCompany(CompanyViewModel m)
        {
            try
            {
                if (!m.id.HasValue || m.id.Value == _users.ApplicationCompanyID)
                    return false;
                var contact = _users.ContactID;
                var now = DateTime.UtcNow;
                using (new TransactionScope(TransactionScopeOption.Suppress))
                {

                    var d = new NKDC(_users.ApplicationConnectionString, null);
                    if (!CheckPermission(m.id, ActionPermission.Delete, typeof(Company)) ||
                        !d.Companies.Where(f => f.PrimaryContactID==contact && f.CompanyID==m.id && f.Version == 0 && f.VersionDeletedBy == null ).Any())
                        return false;
                    //Lets just go ahead and delete with versioning
                    //Only the owner can delete
                    (from o in d.Experiences.Where(f => f.Version == 0 && f.VersionDeletedBy == null) where o.CompanyID == m.id select o)
                        .Update(retire => new Experience
                        {
                            DateFinished = now,
                            Expiry = now,
                            VersionUpdated = now,
                            VersionUpdatedBy = contact
                        });
                    //(from o in d.CompanyRelations.Where(f=>f.Version == 0 && f.VersionDeletedBy == null) where o.CompanyID == m.id || o.ParentCompanyID == m.id select o).Delete();
                    //var companies = (from o in d.Companies.Where(f=>f.Version == 0 && f.VersionDeletedBy == null) where o.CompanyID == m.id && o.PrimaryContactID == contact select o);
                    //if (!companies.Any())
                    //    return false;
                    //companies.Delete();
                    return true;
                }

            }
            catch (Exception ex)
            {
                return false;
            }
        }



        public IEnumerable<Dictionary<string, object>> GetResponseData(Guid wfid)
        {
            if (!CheckPermission(wfid, ActionPermission.Read, typeof(GraphDataGroup)))
                return new Dictionary<string,object>[] { };
            var application = _users.ApplicationID;
            var contact = _users.ContactID;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
               
                using (var con = new SqlConnection(_users.ApplicationConnectionString))
                using (var cmd = new SqlCommand("E_SP_GetResponseData", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;


                    var qG = cmd.CreateParameter();
                    qG.ParameterName = "@wfid";
                    qG.DbType = DbType.Guid;
                    qG.Value = wfid;
                    cmd.Parameters.Add(qG);

                    var qA = cmd.CreateParameter();
                    qA.ParameterName = "@applicationid";
                    qA.DbType = DbType.Guid;
                    qA.Value = application;
                    cmd.Parameters.Add(qA);

                    var qC = cmd.CreateParameter();
                    qC.ParameterName = "@contactid";
                    qC.DbType = DbType.Guid;
                    qC.Value = contact;
                    cmd.Parameters.Add(qC);


                    con.Open();
                    try
                    {
                        using (var reader = new DataReaderEnumerable(cmd.ExecuteReader()))
                        {
                            return reader.Serialize();
                        }
                    }
                    finally
                    {
                        con.Close();
                    }

                }

            }

        }


        public SelectListItem[] GetWorkflows(string startsWith)
        {

            if (startsWith == null || startsWith.Length == 1)
                return new SelectListItem[] { };
            var application = _users.ApplicationID;
            var companies = _users.GetCompanies().Select(f => (Guid?)f.Key).ToArray();
            var defaultCompany = _users.ApplicationCompanyID;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null, false);
                d.ContextOptions.LazyLoadingEnabled = false;
                return (from o in d.GraphDataGroups
                        where o.GraphDataGroupName.StartsWith(startsWith) && 
                        (!o.VersionOwnerCompanyID.HasValue || companies.Contains(o.VersionOwnerCompanyID) || o.VersionOwnerCompanyID == defaultCompany)
                        orderby o.GraphDataGroupName ascending
                        select o).Take(20).AsEnumerable()
                        .Select(f =>
                             new SelectListItem { Text = f.GraphDataGroupName, Value = string.Format("{0}", f.GraphDataGroupID) })
                             .ToArray()
                        ;
            }
        }

        public SelectListItem[] GetWorkflows(Guid[] workflowIDs)
        {

            if (workflowIDs == null || workflowIDs.Length == 0)
                return new SelectListItem[] { };
            var application = _users.ApplicationID;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null, false);
                d.ContextOptions.LazyLoadingEnabled = false;
                return (from o in d.GraphDataGroups
                        where workflowIDs.Contains(o.GraphDataGroupID)
                        orderby o.GraphDataGroupName ascending
                        select o).AsEnumerable()
                        .Select(f =>
                             new SelectListItem { Text = f.GraphDataGroupName, Value = string.Format("{0}", f.GraphDataGroupID) })
                             .ToArray()
                        ;
            }
        }

        public CompanyViewModel GetDashboard(Guid id)
        {
            if (id == Guid.Empty)
            {
                id = _users.DefaultContactCompanyID;
            }
            if (!_users.GetCompanies().Any(f=>f.Key == id))  {
                return null;
            }
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                var dashID = string.Format(DASHBOARD_COMPANY, id);
                var dash = new CompanyViewModel { };
                dash.Dashboard = (from m in d.MetaDatas.Where(f => f.Version == 0 && f.VersionDeletedBy == null && f.MetaDataType == dashID) select m.ContentToIndex).FirstOrDefault();
                if (string.IsNullOrWhiteSpace(dash.Dashboard) || dash.Dashboard == "<div>&nbsp;</div>" || dash.Dashboard == "<br data-mce-bogus=\"1\">")
                    return null;
                var company = (from c in d.Companies.Where(f => f.CompanyID == id && f.Version == 0 && f.VersionDeletedBy == null) select c).FirstOrDefault();
                if (company == null)
                    return null;
                dash.id = company.CompanyID;
                dash.CompanyName = company.CompanyName;
                return dash;
                
            }
                    
        }

        public bool CopyWorkflow(FlowViewModel m)
        {
            var application = _users.ApplicationID;
            var contact = _users.ContactID;
            var company = _users.ApplicationCompanyID;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                ObjectParameter wfid = new ObjectParameter("newworkflowid", typeof(Guid?));
                d.E_SP_DuplicateWorkflow(contact, application, m.id, m.VersionOwnerContactID, m.VersionOwnerCompanyID, wfid);
                if (wfid.Value as Guid? == null)
                    return false;
                else
                {
                    m.id = (Guid)wfid.Value;
                    return true;
                }

            }

        }
    }
}
