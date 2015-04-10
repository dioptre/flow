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

using EXPEDIT.Flow.ViewModels;
using EXPEDIT.Flow.Models;
using NKD.Services;
using Orchard.Logging;
using System.Transactions;
using NKD.Module.BusinessObjects;
using NKD.Models;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Globalization;
using Newtonsoft.Json;

namespace EXPEDIT.Flow.Services
{
      [UsedImplicitly]
    public class TranslationService : ITranslationService
    {

        private readonly IUsersService _users;
           public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        public TranslationService(        
            IUsersService users
           )
        {
            _users = users;
            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
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
                            m.TranslationQueue = (from g in
                                                      (from g in d.GraphData.Where(f => f.Version == 0 && f.VersionDeletedBy == null)
                                                       join gg in d.GraphDataRelation.Where(f => f.GraphDataGroupID == m.DocID && f.Version == 0 && f.VersionDeletedBy == null)
                                                         on g.GraphDataID equals gg.FromGraphDataID
                                                       select g).Union(
                                                          (from g in d.GraphData.Where(f => f.Version == 0 && f.VersionDeletedBy == null)
                                                           join gg in d.GraphDataRelation.Where(f => f.GraphDataGroupID == m.DocID && f.Version == 0 && f.VersionDeletedBy == null)
                                                         on g.GraphDataID equals gg.ToGraphDataID
                                                           select g))
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
                                                      join g in d.GraphData.Where(f => f.Version == 0 && f.VersionDeletedBy == null)
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
                        var lang = cultures.OrderBy(f => f.Name).FirstOrDefault(f => f.Name.StartsWith(m.TranslationCulture));
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
                                        dynamic json = JObject.Parse(await response.Content.ReadAsStringAsync());
                                        return json.data.translations[0].translatedText ?? string.Empty;
                                    }
                                    else
                                    {
                                        Logger.Debug("Did not translate.", response);
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
                                            tx = (from o in d.TranslationData where o.TranslationDataID == translation.Value.TranslationDataID && o.Version == 0 && o.VersionDeletedBy == null select o).Single();
                                        else
                                            tx = (from o in d.TranslationData
                                                  where
                                                      o.TableType == m.TableType && o.ReferenceID == translation.Key && o.ReferenceName == translation.Value.OriginalName
                                                      && o.TranslationCulture == lang.Name &&
                                                      o.Version == 0 && o.VersionDeletedBy == null
                                                  select o).Single();
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

                    m.TranslationResults = (from o in m.TranslationQueue
                                            select new TranslationViewModel
                                            {
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
                Logger.Debug("Could not translate.", ex);
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
                    var tx = (from o in d.TranslationData where o.TranslationDataID == m.id && o.Version == 0 && o.VersionDeletedBy == null select o).Single();
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
                    var tx = (from o in d.TranslationData where o.TranslationDataID == m.id && o.Version == 0 && o.VersionDeletedBy == null select o).Single();
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
                    m.LocaleQueue = (from o in d.TranslationData.Where(f => f.TableType == LocaleViewModel.DocType && f.TranslationCulture == m.OriginalCulture && f.Version == 0 && f.VersionDeletedBy == null)
                                     join t in d.TranslationData.Where(f => f.TableType == LocaleViewModel.DocType && f.TranslationCulture == m.TranslationCulture && f.Version == 0 && f.VersionDeletedBy == null)
                                                         on o.ReferenceName equals t.ReferenceName
                                                         into gt
                                     from tx in gt.DefaultIfEmpty()
                                     select new { Original = o, Translation = tx }).Select(f =>
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
                    var lang = cultures.OrderBy(f => f.Name).FirstOrDefault(f => f.Name.StartsWith(m.TranslationCulture));

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
                                            var tx = (from o in d.TranslationData where o.TranslationDataID == translation.TranslationDataID && o.Version == 0 && o.VersionDeletedBy == null select o).Single();
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
                    var tx = (from o in d.TranslationData where o.TranslationDataID == m.id && o.Version == 0 && o.VersionDeletedBy == null && o.TableType == LocaleViewModel.DocType select o).Single();
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
                    var tx = (from o in d.TranslationData where o.TranslationDataID == m.id && o.Version == 0 && o.VersionDeletedBy == null && o.TableType == LocaleViewModel.DocType select o).Single();
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
    }
}