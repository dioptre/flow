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

namespace EXPEDIT.Flow.Services {
    
    [UsedImplicitly]
    public class FlowService : IFlowService {

        public const string STAT_NAME_FLOW_ACCESS = "FlowAccess";

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


        public IEnumerable<SearchViewModel> Search(string query, int? start = 0, int? pageSize = 20, SearchType? st = SearchType.Flow)
        {
            //if no results show wikipedia
            var application = _users.ApplicationID;
            var contact = _users.ContactID;                
            var company = _users.ApplicationCompanyID;
            var server = _users.ServerID;

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
                                         TotalRows = reader[1] as long?,
                                         Score = reader[2] as decimal?,
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

        public WikiViewModel GetWiki(string wikiName)
        {
            wikiName = wikiName.ToSlug();
            var company = _users.DefaultContactCompanyID;
            var contact = _users.ContactID;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                bool isNew;
                WikiViewModel m;
                var id = CheckNodePrivileges(d, wikiName, null, company, ActionPermission.Update, out isNew);
                if (id == null)
                    return null;
                if (string.IsNullOrWhiteSpace(wikiName) || isNew)
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
            var company = _users.DefaultContactCompanyID;
            var contact = _users.ContactID;
            var now = DateTime.UtcNow;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                bool isNew;
                var id = CheckNodePrivileges(d, m.GraphName, m.GraphDataID, company, ActionPermission.Update, out isNew);
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
                        (from o in d.GraphDataFileDatas where !files.Contains(o.FileDataID.Value) select o).Delete(); //Delete redundant links
                        var fd = (from o in d.GraphDataFileDatas where o.GraphDataID == id select o).AsEnumerable();
                        foreach (var file in files)
                        {
                            if (!fd.Any(f => f.FileDataID == file))
                            {
                                var gdc = new GraphDataFileData
                                {
                                    GraphDataFileDataID = Guid.NewGuid(),
                                    GraphDataID = g.GraphDataID,
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
                        (from o in d.GraphDataLocations where !locations.Contains(o.LocationID.Value) select o).Delete(); //Delete redundant links
                        var ld = (from o in d.GraphDataLocations where o.GraphDataID == id select o).AsEnumerable();
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
                        (from o in d.GraphDataLocations where o.GraphDataID == id select o).Delete();
                        (from o in d.GraphDataFileDatas where o.GraphDataID == id select o).Delete();
                        (from o in d.GraphDataContexts where o.GraphDataID == id select o).Delete();
                    }
                }
                else
                {
                    (from o in d.GraphDataLocations where o.GraphDataID == id select o).Delete();
                    (from o in d.GraphDataFileDatas where o.GraphDataID == id select o).Delete();
                    (from o in d.GraphDataContexts where o.GraphDataID == id select o).Delete();
                }
                g.VersionUpdated = now;
                g.VersionUpdatedBy = contact;
                d.SaveChanges();
                return true;
            }

        }

        public bool GetDuplicateNode(string wikiName)
        {
            wikiName = wikiName.ToSlug();
            var company = _users.DefaultContactCompanyID;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                return d.GraphData.Any(f=>f.GraphName == wikiName && f.VersionOwnerCompanyID==company);
            }
        }

        /// <summary>
        /// For now lets only support one page per name.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="nodeName"></param>
        /// <param name="permission"></param>
        /// <param name="isNew"></param>
        /// <returns></returns>
        internal Guid? CheckNodePrivileges(NKDC d, string nodeName, Guid? nodeID, Guid companyOwner, NKD.Models.ActionPermission permission, out bool isNew)
        {
            var table = d.GetTableName(typeof(GraphData));
            LinqModels.MinimumGraphData root;
            isNew = false;
            try
            {
                root = (from o in d.GraphData
                        where ((o.GraphName == nodeName && o.VersionOwnerCompanyID == companyOwner) || o.GraphDataID == nodeID) && o.Version == 0 && o.VersionDeletedBy == null 
                        select new LinqModels.MinimumGraphData { 
                            GraphDataID = o.GraphDataID, 
                            VersionAntecedentID = o.VersionAntecedentID, 
                            VersionOwnerCompanyID = o.VersionOwnerCompanyID, 
                            VersionOwnerContactID = o.VersionOwnerContactID 
                        }
                        ).SingleOrDefault();
            }
            catch
            {
                //This should never happen
                return null;
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
            if (!isNew)
            {
                var stat = (from o in d.StatisticDatas
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
            return id;
        }

        public FlowGroupViewModel GetNodeGroup(string name, Guid? nid, Guid? gid, bool includeContent = false)
        {
            name = name.ToSlug();
            var company = _users.DefaultContactCompanyID;
            var contact = _users.ContactID;
            var now = DateTime.UtcNow;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                if (string.IsNullOrWhiteSpace(name) && !nid.HasValue && !gid.HasValue)
                    nid = (from o in d.GraphData where (o.VersionOwnerCompanyID == company && o.VersionDeletedBy == null && o.Version == 0) select o.GraphDataID).FirstOrDefault();
                if ((!nid.HasValue || nid == default(Guid)) && !gid.HasValue)
                    nid = (from o in d.GraphData where (o.GraphName == name && o.VersionOwnerCompanyID == company) select o.GraphDataID).SingleOrDefault();
                if ((!nid.HasValue || nid == default(Guid)) && gid.HasValue)
                    nid = (from o in d.GraphDataRelation
                           where o.GraphDataGroupID == gid &&
                              o.VersionOwnerCompanyID == company && o.VersionDeletedBy == null && o.Version == 0
                              && o.GraphDataOriginal.VersionOwnerCompanyID == company && o.GraphDataOriginal.VersionDeletedBy == null && o.GraphDataOriginal.Version == 0
                           select o.GraphDataOriginal.GraphDataID).FirstOrDefault();
                if ((!nid.HasValue || nid == default(Guid)) && gid.HasValue)
                    nid = (from o in d.GraphDataRelation
                                where o.GraphDataGroupID == gid &&
                                  o.VersionOwnerCompanyID == company && o.VersionDeletedBy == null && o.Version == 0
                                  && o.GraphDataRelated.VersionOwnerCompanyID == company && o.GraphDataRelated.VersionDeletedBy == null && o.GraphDataRelated.Version == 0
                                  select o.GraphDataRelated.GraphDataID).FirstOrDefault();
                if ((!nid.HasValue || nid == default(Guid)))
                    return null;
                bool isNew; 
                //We check permission and use identical settings for rest
                var id = CheckNodePrivileges(d, null, nid, company, ActionPermission.Read, out isNew);
                if (!id.HasValue || isNew || nid != id)
                    return null;
                var disconnected = (from o in d.GraphData where                                          
                                        o.VersionOwnerCompanyID == company && o.VersionDeletedBy == null && o.Version == 0
                                        && !o.GraphDataOrigin.Any() && !o.GraphDataRelation.Any()
                                       select o);

                if (!gid.HasValue && !(from o in d.GraphDataRelation
                                       where o.FromGraphDataID == nid || o.ToGraphDataID == nid && o.VersionDeletedBy == null && o.Version == 0
                                       select o.GraphDataGroupID).Any())
                {
                    if (includeContent)
                        return new FlowGroupViewModel { Nodes = disconnected.Select(g => new FlowViewModel
                             {
                                 GraphDataID = g.GraphDataID,
                                 GraphName = g.GraphName,
                                 GraphData = g.GraphContent
                             }).ToArray()};
                    else
                        return new FlowGroupViewModel
                        {
                            Nodes = disconnected.Select(g => new FlowViewModel
                            {
                                GraphDataID = g.GraphDataID,
                                GraphName = g.GraphName
                            }).ToArray()
                        };

                }
                if (!gid.HasValue)
                    gid = (from o in d.GraphDataRelation
                           where  o.FromGraphDataID == nid || o.ToGraphDataID == nid && o.VersionDeletedBy == null && o.Version == 0
                           select o.GraphDataGroupID).FirstOrDefault();
                 var nodes = (from o in d.GraphDataRelation
                              where
                              o.GraphDataGroupID == gid &&
                              o.VersionOwnerCompanyID == company && o.VersionDeletedBy == null && o.Version == 0
                              && o.GraphDataOriginal.VersionOwnerCompanyID == company && o.GraphDataOriginal.VersionDeletedBy == null && o.GraphDataOriginal.Version == 0
                              select o.GraphDataOriginal)
                              .Union(
                                  from o in d.GraphDataRelation
                                  where
                                  o.GraphDataGroupID == gid &&
                                  o.VersionOwnerCompanyID == company && o.VersionDeletedBy == null && o.Version == 0
                                  && o.GraphDataRelated.VersionOwnerCompanyID == company && o.GraphDataRelated.VersionDeletedBy == null && o.GraphDataRelated.Version == 0
                                  select o.GraphDataRelated).Union(disconnected);

                FlowGroupViewModel m = new FlowGroupViewModel();
                 if (includeContent)
                 {
                     m.Nodes = nodes.Select(g => new FlowViewModel
                     {
                         GraphDataID = g.GraphDataID,
                         GraphName = g.GraphName,
                         GraphData = g.GraphContent
                     }).ToArray();
                 }
                 else
                 {
                     m.Nodes = nodes.Select(g => new FlowViewModel
                     {
                         GraphDataID = g.GraphDataID,
                         GraphName = g.GraphName
                     }).ToArray();
                 }
                 m.Edges = (from o in d.GraphDataRelation
                            where o.GraphDataGroupID == gid && o.VersionOwnerCompanyID == company && o.VersionDeletedBy == null && o.Version == 0
                            select new FlowEdgeViewModel
                            {
                                GraphDataRelationID = o.GraphDataRelationID,
                                FromID = o.FromGraphDataID,
                                ToID = o.ToGraphDataID,
                                GroupID = o.GraphDataGroupID,
                                Weight = o.Weight,
                                RelationTypeID = o.RelationTypeID,
                                Sequence = o.Sequence
                            }).ToArray();

                return m;
            }
        }
       
       
        public FlowViewModelDetailed GetNode(string name, Guid? nid, bool includeContent = false)
        {
            name = name.ToSlug();
            if (string.IsNullOrWhiteSpace(name) && !nid.HasValue)
                return null;
            var company = _users.DefaultContactCompanyID;
            var contact = _users.ContactID;
            var now = DateTime.UtcNow;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                if (!nid.HasValue)
                    nid = (from o in d.GraphData where (o.GraphName == name && o.VersionOwnerCompanyID == company) select o.GraphDataID).SingleOrDefault();
                if (nid == default(Guid))
                    return null;
                bool isNew;
                var id = CheckNodePrivileges(d, null, nid, company, ActionPermission.Read, out isNew);
                if (!id.HasValue || isNew || nid != id)
                    return null;
                FlowViewModelDetailed m;
                if (includeContent)
                {
                    var g = (from o in d.GraphData where o.GraphDataID == id && o.VersionDeletedBy == null && o.Version == 0 select o).Single();
                    m = new FlowViewModelDetailed
                    {
                        GraphDataID = g.GraphDataID,
                        GraphName = g.GraphName,
                        GraphData = g.GraphContent,
                        Children = (from o in g.GraphDataRelation where o.VersionDeletedBy == null && o.Version==0 select 
                                        new FlowEdgeViewModel {
                                            GraphDataRelationID = o.GraphDataRelationID,
                                            FromID = o.FromGraphDataID,
                                            ToID = o.ToGraphDataID,
                                            GroupID = o.GraphDataGroupID,
                                            Weight = o.Weight,
                                            RelationTypeID = o.RelationTypeID,
                                            Sequence = o.Sequence
                                        }).ToArray(),
                        Parents = (from o in g.GraphDataOrigin where o.VersionDeletedBy == null && o.Version==0 select 
                                        new FlowEdgeViewModel {
                                            GraphDataRelationID = o.GraphDataRelationID,
                                            FromID = o.FromGraphDataID,
                                            ToID = o.ToGraphDataID,
                                            GroupID = o.GraphDataGroupID,
                                            Weight = o.Weight,
                                            RelationTypeID = o.RelationTypeID,
                                            Sequence = o.Sequence}).ToArray()
                    };
                }
                else
                {
                    m = (from o in d.GraphData where o.GraphDataID == id select
                    new FlowViewModelDetailed
                    {
                        GraphDataID = o.GraphDataID,
                        GraphName = o.GraphName
                    }).Single();
                    m.Parents = (from o in d.GraphDataRelation
                                  where o.ToGraphDataID == id && o.VersionDeletedBy == null && o.Version == 0
                                  select
                                      new FlowEdgeViewModel
                                      {
                                          GraphDataRelationID = o.GraphDataRelationID,
                                          FromID = o.FromGraphDataID,
                                          ToID = o.ToGraphDataID,
                                          GroupID = o.GraphDataGroupID,
                                          Weight = o.Weight,
                                          RelationTypeID = o.RelationTypeID,
                                          Sequence = o.Sequence
                                      }).ToArray();
                    m.Children = (from o in d.GraphDataRelation
                                  where o.FromGraphDataID == id && o.VersionDeletedBy == null && o.Version == 0
                                  select
                                      new FlowEdgeViewModel
                                      {
                                          GraphDataRelationID = o.GraphDataRelationID,
                                          FromID = o.FromGraphDataID,
                                          ToID = o.ToGraphDataID,
                                          GroupID = o.GraphDataGroupID,
                                          Weight = o.Weight,
                                          RelationTypeID = o.RelationTypeID,
                                          Sequence = o.Sequence
                                      }).ToArray();
                }
                return m;
            }
        }

        public bool CreateNode(FlowViewModel m)
        {
            m.GraphName = m.GraphName.ToSlug();
            if (!m.GraphDataID.HasValue || string.IsNullOrWhiteSpace(m.GraphName))
                return false;
            var company = _users.DefaultContactCompanyID;
            var contact = _users.ContactID;
            var now = DateTime.UtcNow;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                bool isNew;
                var id = CheckNodePrivileges(d, m.GraphName, m.GraphDataID, company, ActionPermission.Create, out isNew);
                if (!id.HasValue || !isNew)
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
                d.SaveChanges();
                return true;
            }
        }

        public bool UpdateNode(FlowViewModel m)
        {
            m.GraphName = m.GraphName.ToSlug();
            if (!m.GraphDataID.HasValue || string.IsNullOrWhiteSpace(m.GraphName))
                return false;
            var company = _users.DefaultContactCompanyID;
            var contact = _users.ContactID;
            var now = DateTime.UtcNow;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                bool isNew;
                var id = CheckNodePrivileges(d, m.GraphName, m.GraphDataID, company, ActionPermission.Update, out isNew);
                if (!id.HasValue || isNew || id != m.GraphDataID)
                    return false;
                var g = (from o in d.GraphData where o.GraphDataID == m.GraphDataID select o).Single();
                if (string.IsNullOrWhiteSpace(m.GraphName) && m.GraphName != g.GraphName)
                {
                    g.GraphName = m.GraphName;
                }
                if (string.IsNullOrWhiteSpace(m.GraphData) && m.GraphData != g.GraphContent)
                {
                    g.GraphContent = m.GraphData;
                }
                g.VersionUpdated = now;
                g.VersionUpdatedBy = contact;                
                d.SaveChanges();
                return true;
            }
        }
        
        public bool DeleteNode(FlowViewModel m)
        {
            m.GraphName = m.GraphName.ToSlug();
            if (!m.GraphDataID.HasValue || string.IsNullOrWhiteSpace(m.GraphName))
                return false;
            var company = _users.DefaultContactCompanyID;
            var contact = _users.ContactID;
            var now = DateTime.UtcNow;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                bool isNew;
                var id = CheckNodePrivileges(d, m.GraphName, m.GraphDataID, company, ActionPermission.Delete, out isNew);
                if (!id.HasValue || isNew || id != m.GraphDataID)
                    return false;
                var g = (from o in d.GraphData where o.GraphDataID == m.GraphDataID select o).Single();
                d.GraphDataRelation.Where(f => f.FromGraphDataID == m.GraphDataID || f.ToGraphDataID == m.GraphDataID).Delete();
                d.GraphDataContexts.Where(f => f.GraphDataID == m.GraphDataID).Delete();
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
            var contact = _users.ContactID;
            var now = DateTime.UtcNow;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                bool isNew;
                var id = CheckNodePrivileges(d, null, m.FromID, company, ActionPermission.Update, out isNew);
                if (id == null || isNew)
                    return false;
                id = CheckNodePrivileges(d, null, m.ToID, company, ActionPermission.Update, out isNew);
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

        public bool DeleteEdge(FlowEdgeViewModel m)
        {
            if (!m.GraphDataRelationID.HasValue || !m.FromID.HasValue || !m.ToID.HasValue || !m.GroupID.HasValue)
                return false;
            var company = _users.DefaultContactCompanyID;
            var contact = _users.ContactID;
            var now = DateTime.UtcNow;
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                var d = new NKDC(_users.ApplicationConnectionString, null);
                bool isNew;
                var id = CheckNodePrivileges(d, null, m.FromID, company, ActionPermission.Update, out isNew);
                if (id == null || isNew)
                    return false;
                id = CheckNodePrivileges(d, null, m.ToID, company, ActionPermission.Update, out isNew);
                if (id == null || isNew)
                    return false;
                var disconnects = (from o in d.GraphDataRelation where (o.FromGraphDataID == m.FromID && o.ToGraphDataID == m.ToID && o.GraphDataGroupID == m.GroupID) || o.GraphDataRelationID == m.GraphDataRelationID select o);
                foreach (var g in disconnects)
                    d.GraphDataRelation.DeleteObject(g);
                d.SaveChanges();
                return true;
            }
        }
    

       
    }
}
