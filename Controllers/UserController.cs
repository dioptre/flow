using System.Web.Mvc;
using Orchard.Localization;
using Orchard;
using EXPEDIT.Flow.Services;
using Orchard.Themes;
using NKD.Helpers;
using System;
using Orchard.DisplayManagement;
using System.Web;
using Orchard.UI.Navigation;
using Orchard.Search.Models;
using Orchard.Search.Services;
using Orchard.Search.ViewModels;
using Orchard.Settings;
using Orchard.UI.Notify;
using Orchard.Indexing;
using Orchard.Logging;
using Orchard.Collections;
using Orchard.ContentManagement;
using System.Collections.Generic;
using System.Linq;
using Orchard.Mvc;
using EXPEDIT.Flow.ViewModels;
using System.Text.RegularExpressions;
using Orchard.Security;
using Orchard.Users.Services;
using Orchard.Users.Events;
using NKD.Models;


namespace EXPEDIT.Flow.Controllers {

    [Themed]
    public class UserController : Controller
    {
        public IOrchardServices Services { get; set; }
        private IFlowService _Flow { get; set; }
        private ITranslationService _Translation { get; set; }
        public ILogger Logger { get; set; }
        private readonly IContentManager _contentManager;
        private readonly ISiteService _siteService;

        public UserController(
            IOrchardServices services,
            IFlowService Flow,
            ITranslationService Translation,
            IContentManager contentManager,
            ISiteService siteService,
            IShapeFactory shapeFactory
            )
        {
            _Flow = Flow;
            _Translation = Translation;
            Services = services;
            T = NullLocalizer.Instance;
            _contentManager = contentManager;
            _siteService = siteService;
        }

        public Localizer T { get; set; }

        [Themed(false)]
        public ActionResult Index()
        {
            return View();
        }

        [Themed(false)]
        public ActionResult Preview()
        {
            return View();
        }

        [Themed(false)]
        public ActionResult Searches(string q)
        {
            int page;
            int pageSize;
            List<Tuple<double, double, double, double>> polygons = new List<Tuple<double, double, double, double>>();
            DateTime? minDate = default(DateTime?);
            DateTime? maxDate = default(DateTime?);
            foreach (var key in Request.Params.AllKeys)
            {
                if (Regex.IsMatch(key, @"tags\[[0-9]?\]\[l\]", RegexOptions.Compiled | RegexOptions.IgnoreCase))
                {
                    string l = Request.Params[key];
                    if (string.IsNullOrWhiteSpace(l))
                        continue;
                    var la = l.Split(new[] { ",", "(", ")", " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (la.Length != 4)
                        continue;
                    double tL1, tL2, tL3, tL4;
                    if (!double.TryParse(la[0], out tL1))
                        continue;
                    if (!double.TryParse(la[1], out tL2))
                        continue;
                    if (!double.TryParse(la[2], out tL3))
                        continue;
                    if (!double.TryParse(la[3], out tL4))
                        continue;
                    polygons.Add(new Tuple<double, double, double, double>(tL1, tL2, tL3, tL4));
                }
                else if (Regex.IsMatch(key, @"tags\[[0-9]?\]\[d\]", RegexOptions.Compiled | RegexOptions.IgnoreCase))
                {
                    string dt = Request.Params[key];
                    if (string.IsNullOrWhiteSpace(dt))
                        continue;
                    var dta = dt.Split(new[] { "-" }, StringSplitOptions.RemoveEmptyEntries);
                    if (dta.Length != 2)
                        continue;
                    int tempTS;
                    if (!int.TryParse(dta[0], out tempTS))
                        continue;
                    var tMin = DateHelper.UnixTimestampToDate(tempTS);
                    if (!minDate.HasValue || tMin < minDate)
                        minDate = tMin;
                    if (!int.TryParse(dta[1], out tempTS))
                        continue;
                    var tMax = DateHelper.UnixTimestampToDate(tempTS);
                    if (!maxDate.HasValue || tMax > maxDate)
                        maxDate = tMax;
                }
            }

            string viewport = null;
            if (polygons.Count > 0)
            {
                viewport = polygons.CreateMultiRectangle().ToString();
            }

            string query = Request.Params["keywords"];
            if (string.IsNullOrWhiteSpace(query) || query == "undefined")
                query = null;
            if (string.IsNullOrWhiteSpace(query))
                query = null;
            string type = Request.Params["type"];
            SearchType st = SearchType.Flow;
            if (string.IsNullOrWhiteSpace(type) || type == "undefined" || type == "process")
                st = SearchType.Flow;
            else if (type == "file")
                st = SearchType.File;
            else if (type == "model")
                st = SearchType.Model;
            else if (type == "flowlocation")
                st = SearchType.FlowLocation;
            else if (type == "workflow")
                st = SearchType.FlowGroup;

            bool pFound = int.TryParse(Request.Params["page"], out page);
            bool psFound = int.TryParse(Request.Params["pageSize"], out pageSize);
            var results = _Flow.Search(
                query,
                (pFound && psFound) ? (page * pageSize) + 1 : default(int?),
                psFound ? pageSize : default(int?),
                st,
                minDate,
                maxDate,
                viewport
            );
            return new JsonHelper.JsonNetResult(new { search = results }, JsonRequestBehavior.AllowGet);
        }


        [Themed(true)]
        [HttpGet]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public ActionResult Wiki(string id)
        {
            WikiViewModel m;
            Guid nid;
            if (Guid.TryParse(id, out nid))
                m = _Flow.GetWiki(null, nid);
            else
                m = _Flow.GetWiki(id, null);
            if (m == null)
                return new HttpUnauthorizedResult("Unauthorized access to protected article.");
            return View(m);
        }

        [ValidateInput(false)]
        [Themed(true)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Wiki(WikiViewModel m)
        {
            var ok = _Flow.SubmitWiki(ref m);
            if (!ok)
                return View(m);
            else
                return new RedirectResult(System.Web.VirtualPathUtility.ToAbsolute(string.Format("~/flow/#/graph/{0}", m.GraphDataID)));
        }

        [Themed(false)]
        [HttpGet]
        [ActionName("NodeDuplicate")]
        public ActionResult NodeDuplicate(string id)
        {
            string gid = Request.Params["guid"];
            Guid? guid = null;
            Guid tgid;
            if (Guid.TryParse(gid, out tgid))
                guid = tgid;
            return new JsonHelper.JsonNetResult(_Flow.GetDuplicateNode(id, guid), JsonRequestBehavior.AllowGet);
        }

        [Themed(false)]
        [HttpGet]
        [ActionName("WorkflowDuplicate")]
        public ActionResult WorkflowDuplicate(string id)
        {
            string gid = Request.Params["guid"];
            Guid? guid = null;
            Guid tgid;
            if (Guid.TryParse(gid, out tgid))
                guid = tgid;
            return new JsonHelper.JsonNetResult(_Flow.GetDuplicateWorkflow(id, guid), JsonRequestBehavior.AllowGet);
        }

        [Themed(false)]
        [HttpGet]
        [ActionName("WorkflowDuplicateID")]
        public ActionResult WorkflowDuplicateID(string id)
        {
            Guid gid;
            Guid temp;
            if (Guid.TryParse(id, out temp))
            {
                gid = temp;
                return new JsonHelper.JsonNetResult(_Flow.GetDuplicateWorkflow(gid), JsonRequestBehavior.AllowGet);
            }
            else
            {
                return new JsonHelper.JsonNetResult(false, JsonRequestBehavior.AllowGet);
            }
        }

        [Themed(false)]
        [HttpGet]
        [ActionName("NodeDuplicateID")]
        public ActionResult NodeDuplicateID(string id)
        {
            Guid gid;
            Guid temp;
            if (Guid.TryParse(id, out temp))
            {
                gid = temp;
                return new JsonHelper.JsonNetResult(_Flow.GetDuplicateNode(gid), JsonRequestBehavior.AllowGet);
            }
            else
            {
                return new JsonHelper.JsonNetResult(false, JsonRequestBehavior.AllowGet);
            }
        }

        [Themed(false)]
        [HttpGet]
        [ActionName("Nodes")]
        public ActionResult GetNode(string id)
        {

            string group = Request.Params["groupid"];
            Guid? gid = null;
            Guid tgid;
            if (Guid.TryParse(group, out tgid))
                gid = tgid;
            if (string.IsNullOrWhiteSpace(id))
                return new JsonHelper.JsonNetResult(_Flow.GetNode(null, null, gid, false, true), JsonRequestBehavior.AllowGet);
            Guid temp;
            string name = null;
            FlowGroupViewModel result = null;
            if (!Guid.TryParse(id, out temp))
                result = _Flow.GetNode(name, null, gid, true, false);
            else
                result = _Flow.GetNode(null, temp, gid, true, false);
            if (result == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden); //Unauthorized redirects which is not so good fer ember
            return new JsonHelper.JsonNetResult(result, JsonRequestBehavior.AllowGet);
        }

        [Themed(false)]
        [HttpPost]
        [ActionName("Nodes")]
        public ActionResult CreateNode(FlowViewModel m)
        {
            //if (!User.Identity.IsAuthenticated)
            //    return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);
            if (m.node != null)
            {
                if (m.GraphDataID.HasValue)
                    m.node.GraphDataID = m.GraphDataID;
                m = m.node;
            }
            if (_Flow.CreateNode(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed);
        }

        [Themed(false)]
        [HttpPut]
        [ActionName("Nodes")]
        public ActionResult UpdateNode(FlowViewModel m)
        {
            //if (!User.Identity.IsAuthenticated)
            //    return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);
            if (m.node != null)
            {
                if (m.GraphDataID.HasValue)
                    m.node.GraphDataID = m.GraphDataID;
                m = m.node;
            }
            if (_Flow.UpdateNode(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed);
        }

        [Themed(false)]
        [HttpDelete]
        [ActionName("Nodes")]
        public ActionResult DeleteNode(FlowViewModel m)
        {
            //if (!User.Identity.IsAuthenticated)
            //    return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);
            if (m.node != null && m.node.id != null)
                m = m.node;
            if (!m.GraphDataID.HasValue)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            if (_Flow.UnlinkNode(m.GraphDataID.Value, m.workflows.FirstOrDefault()))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed);
        }


        [Themed(false)]
        [HttpGet]
        [ActionName("Edges")]
        public ActionResult GetEdge(string id)
        {
            return new EmptyResult();
        }

        [Themed(false)]
        [HttpPost]
        [ActionName("Edges")]
        public ActionResult CreateEdge(FlowEdgeViewModel m)
        {
            //if (!User.Identity.IsAuthenticated)
            //    return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);
            if (m.edge != null && m.edge.id != null)
                m = m.edge;
            if (_Flow.CreateEdge(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed);
        }


        [Themed(false)]
        [HttpPut]
        [ActionName("Edges")]
        public ActionResult UpdateEdge(FlowEdgeViewModel m)
        {
            if (m.edge != null && m.id != null)
            {
                m.edge.id = m.id;
                m = m.edge;
            }
            if (_Flow.UpdateEdge(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed);
        }

        [Themed(false)]
        [HttpDelete]
        [ActionName("Edges")]
        public ActionResult DeleteEdge(FlowEdgeViewModel m)
        {
            //if (!User.Identity.IsAuthenticated)
            //    return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);
            if (m.edge != null && m.edge.id != null)
                m = m.edge;
            if (!m.GraphDataRelationID.HasValue)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            if (_Flow.DeleteEdge(m.GraphDataRelationID.Value))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed);
        }

        [Themed(false)]
        [HttpGet]
        [ActionName("MyUserInfo")]
        public ActionResult GetMyUserInfo(string id)
        {
            return new JsonHelper.JsonNetResult(_Flow.GetMyInfo(), JsonRequestBehavior.AllowGet);
        }



        [Themed(false)]
        [HttpGet]
        [ActionName("Workflows")]
        public ActionResult GetWorkflow(string id)
        {
            FlowEdgeWorkflowViewModel m;
            Guid gid;
            if (Guid.TryParse(id, out gid))
                m = _Flow.GetWorkflow(gid);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed);
            if (m == null)
                return new HttpUnauthorizedResult("Unauthorized access to protected workflow.");
            else
                return new JsonHelper.JsonNetResult(new { workflow = m }, JsonRequestBehavior.AllowGet);

        }

        [Themed(false)]
        [HttpPost]
        [ActionName("Workflows")]
        public ActionResult CreateWorkflow(FlowEdgeWorkflowViewModel m)
        {
            //if (!User.Identity.IsAuthenticated)
            //    return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);
            if (m.workflow != null)
            {
                if (m.GraphDataGroupID.HasValue)
                    m.workflow.GraphDataGroupID = m.GraphDataGroupID;
                m = m.workflow;
            }
            if (_Flow.CreateWorkflow(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed);
        }

        [Themed(false)]
        [HttpPut]
        [ActionName("Workflows")]
        public ActionResult UpdateWorkflow(FlowEdgeWorkflowViewModel m)
        {
            //if (!User.Identity.IsAuthenticated)
            //    return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);
            if (m.workflow != null)
            {
                if (m.GraphDataGroupID.HasValue)
                    m.workflow.GraphDataGroupID = m.GraphDataGroupID;
                m = m.workflow;
            }
            if (_Flow.UpdateWorkflow(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed);
        }



        [Themed(false)]
        [HttpGet]
        [ActionName("MyWorkflows")]
        public ActionResult GetMyWorkflows(string id)
        {
            return new JsonHelper.JsonNetResult(new { myWorkflows = _Flow.GetMyWorkflows() }, JsonRequestBehavior.AllowGet);
        }

        [Themed(false)]
        [HttpGet]
        [ActionName("MyNodes")]
        public ActionResult GetMyNodes(string id)
        {
            return new JsonHelper.JsonNetResult(new { myNodes = _Flow.GetMyNodes() }, JsonRequestBehavior.AllowGet);
        }

        [Themed(false)]
        [HttpGet]
        [ActionName("MyFiles")]
        public ActionResult GetMyFiles(string id)
        {
            return new JsonHelper.JsonNetResult(new { myFiles = _Flow.GetMyFiles() }, JsonRequestBehavior.AllowGet);

        }

        [Themed(false)]
        [HttpGet]
        [ActionName("MySecurityLists")]
        public ActionResult GetMySecurityLists(string id, string type)
        {
            return new JsonHelper.JsonNetResult(new { mySecurityLists = _Flow.GetMySecurityLists(type) }, JsonRequestBehavior.AllowGet);

        }

        [Themed(false)]
        [HttpGet]
        [ActionName("MyLicenses")]
        public ActionResult GetMyLicenses(string id)
        {
            Guid? gid = null;
            Guid temp;
            if (Guid.TryParse(id, out temp))
                gid = temp;
            return new JsonHelper.JsonNetResult(new { myLicenses = _Flow.GetMyLicenses(gid) }, JsonRequestBehavior.AllowGet);
        }


        [Themed(false)]
        [HttpPut]
        [ActionName("MyLicenses")]
        public ActionResult AssignLicense(LicenseViewModel m)
        {
            if (!User.Identity.IsAuthenticated)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);
            if (m.myLicense != null)
            {
                if (m.LicenseID.HasValue)
                    m.myLicense.LicenseID = m.LicenseID;
                m = m.myLicense;
            }
            if (!m.LicenseeGUID.HasValue || !m.LicenseID.HasValue)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            if (_Flow.AssignLicense(m.LicenseeGUID.Value, m.LicenseID.Value))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed);
        }


        [Themed(false)]
        [HttpGet]
        [ActionName("MyProfiles")]
        public ActionResult GetMyProfiles(string id)
        {
            var profile = _Flow.GetMyProfile();
            if (profile != null)
                return new JsonHelper.JsonNetResult(new { myProfiles = new[] { profile } }, JsonRequestBehavior.AllowGet);
            else
                return new JsonHelper.JsonNetResult(new { myProfiles = new object[] { } }, JsonRequestBehavior.AllowGet);
        }


        [Themed(false)]
        [HttpPut]
        [ActionName("MyProfiles")]
        public ActionResult UpdateMyProfile(UserProfileViewModel m)
        {
            if (!User.Identity.IsAuthenticated)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);
            if (m.myProfile != null)
            {
                if (m.ContactID.HasValue)
                    m.myProfile.ContactID = m.ContactID;
                m = m.myProfile;
            }
            if (_Flow.UpdateProfile(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed);
        }

        [Authorize]
        [Themed(false)]
        [HttpPost]
        [ActionName("MySecurityLists")]
        public ActionResult CreateMySecurityLists(SecurityViewModel m)
        {
            if (m.mySecurityList != null)
            {
                if (m.SecurityID.HasValue)
                    m.mySecurityList.SecurityID = m.SecurityID;
                m = m.mySecurityList;
            }
            if (_Flow.CreateSecurity(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed);

        }

        [Authorize]
        [Themed(false)]
        [HttpDelete]
        [ActionName("MySecurityLists")]
        public ActionResult DeleteMySecurityLists(SecurityViewModel m)
        {
            if (m.mySecurityList != null && m.mySecurityList.SecurityID != null)
                m = m.mySecurityList;
            if (!m.SecurityID.HasValue)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            if (_Flow.DeleteSecurity(m.SecurityID.Value, m.SecurityTypeID))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed);

        }


        [Authorize]
        [Themed(true)]
        public ActionResult Test()
        {
            return View();
        }



        [Themed(false)]
        [HttpGet]
        public ActionResult CheckWorkflowPermission(string id, ActionPermission permission)
        {
            Guid gid;
            Guid temp;
            if (Guid.TryParse(id, out temp))
            {
                gid = temp;
                return new JsonHelper.JsonNetResult(_Flow.CheckWorkflowPermission(gid, permission), JsonRequestBehavior.AllowGet);
            }
            else
            {
                return new JsonHelper.JsonNetResult(false, JsonRequestBehavior.AllowGet);
            }
        }


        [Themed(false)]
        [HttpGet]
        public ActionResult CheckNodePermission(string id, ActionPermission permission)
        {
            Guid gid;
            Guid temp;
            if (Guid.TryParse(id, out temp))
            {
                gid = temp;
                return new JsonHelper.JsonNetResult(_Flow.CheckNodePermission(gid, permission), JsonRequestBehavior.AllowGet);
            }
            else
            {
                return new JsonHelper.JsonNetResult(false, JsonRequestBehavior.AllowGet);
            }
        }


        [Authorize]
        [Themed(false)]
        [HttpPost]
        [ActionName("Translations")]
        public ActionResult CreateTranslation(TranslationViewModel m)
        {
            //return new EmptyResult();
            return UpdateTranslation(m);
        }

        [Authorize]
        [Themed(false)]
        [HttpPut]
        [ActionName("Translations")]
        public ActionResult UpdateTranslation(TranslationViewModel m)
        {
            if (m.translation != null && m.id != null)
            {
                m.translation.id = m.id;
                m = m.translation;
            }
            m.SearchType = SearchType.Flow;
            if (string.IsNullOrWhiteSpace(m.DocType) || m.DocType == "undefined" || m.DocType == "process" || m.DocType == "E_GraphData")
                m.SearchType = SearchType.Flow;
            else if (m.DocType == "workflow" || m.DocType == "E_GraphDataGroup")
                m.SearchType = SearchType.FlowGroup;
            if (_Translation.UpdateTranslation(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed);
        }

        [Themed(false)]
        [HttpGet]
        [ActionName("Translations")]
        public ActionResult GetTranslation(TranslationViewModel m)
        {
            if (m.translation != null && m.translation.id != null)
                m = m.translation;
            if (!string.IsNullOrWhiteSpace(Request.Params["Refresh"]))
                m.Refresh = true;
            m.SearchType = SearchType.Flow;
            m.DocType = string.Format("{0}", m.DocType).ToLowerInvariant();
            if (m.DocType == "node" || string.IsNullOrWhiteSpace(m.DocType) || m.DocType == "undefined" || m.DocType == "process")
                m.SearchType = SearchType.Flow;
            else if (m.DocType == "file")
                m.SearchType = SearchType.File;
            else if (m.DocType == "model")
                m.SearchType = SearchType.Model;
            else if (m.DocType == "flowlocation")
                m.SearchType = SearchType.FlowLocation;
            else if (m.DocType == "workflow")
                m.SearchType = SearchType.FlowGroup;
            else if (m.DocType == "flows")
                m.SearchType = SearchType.Flows;
            if ((m.id.HasValue || m.DocID.HasValue) && _Translation.GetTranslation(m))
                return new JsonHelper.JsonNetResult(new { translations = m.TranslationResults }, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed);
        }

        [Authorize]
        [Themed(false)]
        [HttpDelete]
        [ActionName("Translations")]
        public ActionResult DeleteTranslation(TranslationViewModel m)
        {
            if (!m.id.HasValue)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            m.SearchType = SearchType.Flow;
            if (string.IsNullOrWhiteSpace(m.DocType) || m.DocType == "undefined" || m.DocType == "process" || m.DocType == "E_GraphData")
                m.SearchType = SearchType.Flow;
            else if (m.DocType == "workflow" || m.DocType == "E_GraphDataGroup")
                m.SearchType = SearchType.FlowGroup;
            if (_Translation.DeleteTranslation(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed);

        }


        [Authorize]
        [Themed(false)]
        [HttpPost]
        [ActionName("Locales")]
        public ActionResult CreateLocale(LocaleViewModel m)
        {
            if (!Services.Authorizer.Authorize(StandardPermissions.SiteOwner))
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Unauthorized);
            if (m.locale != null)
                m = m.locale;
            if (_Translation.CreateLocale(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed);
        }

        [Authorize]
        [Themed(false)]
        [HttpPut]
        [ActionName("Locales")]
        public ActionResult UpdateLocale(LocaleViewModel m)
        {
            if (!Services.Authorizer.Authorize(StandardPermissions.SiteOwner))
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Unauthorized);
            if (m.locale != null && m.id != null)
            {
                m.locale.id = m.id;
                m = m.locale;
            }
            if (_Translation.UpdateLocale(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed);
        }

        [Themed(false)]
        [HttpGet]
        [ActionName("Locales")]
        public ActionResult GetLocale(LocaleViewModel m)
        {
            if (m.locale != null && m.locale.id != null)
                m = m.locale;
            if (!string.IsNullOrWhiteSpace(Request.Params["Refresh"]))
            {
                if (!Services.Authorizer.Authorize(StandardPermissions.SiteOwner))
                    return new HttpStatusCodeResult(System.Net.HttpStatusCode.Unauthorized);
                m.Refresh = true;
            }
            if (_Translation.GetLocale(m))
                return new JsonHelper.JsonNetResult(new { locales = m.LocaleQueue }, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed);
        }

        [Authorize]
        [Themed(false)]
        [HttpDelete]
        [ActionName("Locales")]
        public ActionResult DeleteLocale(LocaleViewModel m)
        {
            if (!Services.Authorizer.Authorize(StandardPermissions.SiteOwner))
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Unauthorized);
            if (!m.id.HasValue)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            if (_Translation.DeleteLocale(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed);

        }



        [Themed(false)]
        [HttpGet]
        [ActionName("Steps")]
        public ActionResult GetStep(string id)
        {
            string project = Request.Params["projectID"];
            Guid? pid = null;
            Guid tpid;
            if (Guid.TryParse(project, out tpid))
                pid = tpid;

            string wf = Request.Params["workflowID"];
            Guid? gid = null;
            Guid tgid;
            if (Guid.TryParse(wf, out tgid))
                gid = tgid;

            string node = Request.Params["nodeID"];
            Guid? nid = null;
            Guid tnid;
            if (Guid.TryParse(node, out tnid))
                nid = tnid;

            string task = Request.Params["taskID"];
            Guid? tid = null;
            Guid ttid;
            if (Guid.TryParse(task, out ttid))
                tid = ttid;

            string includeContent = Request.Params["includeContent"];
            bool? ic = null;
            bool tic;
            if (bool.TryParse(includeContent, out tic))
                ic = tic;

            string locale = Request.Params["localeSelected"];
            string l = null;
            if (!string.IsNullOrWhiteSpace(locale))
                l = locale.Trim();


            Guid? sid = null;
            Guid tsid;
            if (Guid.TryParse(id, out tsid))
                sid = tsid;
            if (sid.HasValue || pid.HasValue || tid.HasValue || gid.HasValue)
            {
                var result = _Flow.GetStep(sid, pid, tid, nid, gid, ic ?? sid.HasValue, false, false, l);
                if (result == null)
                    return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden); //Unauthorized redirects which is not so good fer ember
                return new JsonHelper.JsonNetResult(new { steps = new object[] { result } }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return new JsonHelper.JsonNetResult(new { steps = _Flow.GetMySteps() }, JsonRequestBehavior.AllowGet);
            }
        }




        [Themed(false)]
        [HttpGet]
        [ActionName("Projects")]
        public ActionResult GetProject(string id)
        {
            var result = _Flow.GetProject(Guid.Parse(id));

            if (result == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden); //Unauthorized redirects which is not so good fer ember
            return new JsonHelper.JsonNetResult(new { projects = new object[] { result } }, JsonRequestBehavior.AllowGet);
        }

        [Themed(false)]
        [HttpGet]
        [ActionName("ProjectData")]
        public ActionResult GetProjectData(string id)
        {
            var ids = Request.Params["ids[]"];
            ProjectDataViewModel[] result = new ProjectDataViewModel[] { };
            if (!string.IsNullOrWhiteSpace(id))
            {
                result = _Flow.GetProjectData(new Guid[] { Guid.Parse(id) });

            }
            else if (!string.IsNullOrWhiteSpace(ids))
            {
                result = _Flow.GetProjectData((from o in ids.Split(',') select Guid.Parse(o)).ToArray());
            }

            if (result == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden); //Unauthorized redirects which is not so good fer ember
            return new JsonHelper.JsonNetResult(new { projectData = result }, JsonRequestBehavior.AllowGet);
        }

        [Authorize]
        [Themed(false)]
        [HttpPost]
        [ActionName("ProjectData")]
        public ActionResult CreateProjectData(ProjectDataViewModel m)
        {
            if (m.projectDatum != null)
                m = m.projectDatum;
            if (_Flow.CreateProjectData(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed, m.Error);
        }

        [Authorize]
        [Themed(false)]
        [HttpPut]
        [ActionName("ProjectData")]
        public ActionResult UpdateProjectData(ProjectDataViewModel m)
        {
            if (m.projectDatum != null && m.id != null)
            {
                m.projectDatum.id = m.id;
                m = m.projectDatum;
            }
            if (_Flow.UpdateProjectData(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed, m.Error);
        }

        [Authorize]
        [Themed(false)]
        [HttpDelete]
        [ActionName("ProjectData")]
        public ActionResult DeleteProjectData(ProjectDataViewModel m)
        {
            if (!Services.Authorizer.Authorize(StandardPermissions.SiteOwner))
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Unauthorized);
            if (!m.id.HasValue)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            if (_Flow.DeleteProjectData(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed, m.Error);

        }


        [Themed(false)]
        [HttpGet]
        [ActionName("EdgeConditions")]
        public ActionResult GetEdgeCondition(string id)
        {
            var ids = Request.Params["ids[]"];
            EdgeConditionViewModel[] result = new EdgeConditionViewModel[] { };
            if (!string.IsNullOrWhiteSpace(id))
            {
                result = _Flow.GetEdgeCondition(new Guid[] { Guid.Parse(id) });

            }
            else if (!string.IsNullOrWhiteSpace(ids))
            {
                result = _Flow.GetEdgeCondition((from o in ids.Split(',') select Guid.Parse(o)).ToArray());
            }

            if (result == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden); //Unauthorized redirects which is not so good fer ember
            return new JsonHelper.JsonNetResult(new { edgeConditions = result }, JsonRequestBehavior.AllowGet);
        }

        [Authorize]
        [Themed(false)]
        [HttpPost]
        [ActionName("EdgeConditions")]
        public ActionResult CreateEdgeCondition(EdgeConditionViewModel m)
        {

            if (m.edgeCondition != null)
                m = m.edgeCondition;
            if (_Flow.CreateEdgeCondition(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed, m.Error);
        }

        [Authorize]
        [Themed(false)]
        [HttpPut]
        [ActionName("EdgeConditions")]
        public ActionResult UpdateEdgeCondition(EdgeConditionViewModel m)
        {
            if (m.edgeCondition != null && m.id != null)
            {
                m.edgeCondition.id = m.id;
                m = m.edgeCondition;
            }
            if (_Flow.UpdateEdgeCondition(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed, m.Error);
        }

        [Authorize]
        [Themed(false)]
        [HttpDelete]
        [ActionName("EdgeConditions")]
        public ActionResult DeleteEdgeCondition(EdgeConditionViewModel m)
        {
            if (!m.id.HasValue)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            if (_Flow.DeleteEdgeCondition(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed, m.Error);

        }

        [Authorize]
        [Themed(false)]
        [HttpGet]
        [ActionName("ContextNames")]
        public ActionResult GetContextNames(string wfid)
        {

            ContextVariableViewModel[] result = null;
            if (!string.IsNullOrWhiteSpace(wfid))
            {
                result = _Flow.GetContextNames(Guid.Parse(wfid));

            }
            if (result == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden); //Unauthorized redirects which is not so good fer ember
            return new JsonHelper.JsonNetResult(new { contextNames = result }, JsonRequestBehavior.AllowGet);
        }

        [Themed(false)]
        [ActionName("Conditions")]
        public ActionResult GetCondition(string id)
        {
            ConditionViewModel result = _Flow.GetCondition(Guid.Parse(id));
            if (result == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden); //Unauthorized redirects which is not so good fer ember
            return new JsonHelper.JsonNetResult(new { conditions = new ConditionViewModel[] { result } }, JsonRequestBehavior.AllowGet);
        }

        [Authorize]
        [Themed(false)]
        [HttpPost]
        [ActionName("Conditions")]
        public ActionResult CreateCondition(ConditionViewModel m)
        {

            if (m.condition != null)
                m = m.condition;
            if (_Flow.CreateCondition(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed, m.Error);
        }

        [Authorize]
        [Themed(false)]
        [HttpPut]
        [ActionName("Conditions")]
        public ActionResult UpdateCondition(ConditionViewModel m)
        {
            if (m.condition != null && m.id != null)
            {
                m.condition.id = m.id;
                m = m.condition;
            }
            if (_Flow.UpdateCondition(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed, m.Error);
        }

        [Authorize]
        [Themed(false)]
        [HttpDelete]
        [ActionName("Conditions")]
        public ActionResult DeleteCondition(ConditionViewModel m)
        {
            if (!m.id.HasValue)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            if (_Flow.DeleteCondition(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed, m.Error);

        }

        [Authorize]
        [Themed(false)]
        [ActionName("Tasks")]
        public ActionResult GetTask(string id)
        {
            TaskViewModel result = null;
            if (string.IsNullOrWhiteSpace(id))
            {
                string wf = Request.Params["GraphDataGroupID"];
                Guid? gid = null;
                Guid tgid;
                if (Guid.TryParse(wf, out tgid))
                    gid = tgid;

                string node = Request.Params["GraphDataID"];
                Guid? nid = null;
                Guid tnid;
                if (Guid.TryParse(node, out tnid))
                    nid = tnid;

                if (!nid.HasValue || !gid.HasValue)
                    return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

                result = _Flow.GetTask(gid.Value, nid.Value);

            }
            else
            {
                result = _Flow.GetTask(Guid.Parse(id));
            }
            if (result == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden); //Unauthorized redirects which is not so good fer ember
            return new JsonHelper.JsonNetResult(new { tasks = new object[] { result } }, JsonRequestBehavior.AllowGet);

        }

        [Authorize]
        [Themed(false)]
        [HttpPost]
        [ActionName("Tasks")]
        public ActionResult CreateTask(TaskViewModel m)
        {

            if (m.task != null)
                m = m.task;
            if (_Flow.CreateTask(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed, m.Error);
        }

        [Authorize]
        [Themed(false)]
        [HttpPut]
        [ActionName("Tasks")]
        public ActionResult UpdateTask(TaskViewModel m)
        {
            if (m.task != null && m.id != null)
            {
                m.task.id = m.id;
                m = m.task;
            }
            if (_Flow.UpdateTask(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed, m.Error);
        }

        [Authorize]
        [Themed(false)]
        [HttpDelete]
        [ActionName("Tasks")]
        public ActionResult DeleteTask(TaskViewModel m)
        {
            if (!m.id.HasValue)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            if (_Flow.DeleteTask(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed, m.Error);

        }


        [Authorize]
        [Themed(false)]
        [ActionName("Triggers")]
        public ActionResult GetTrigger(string id)
        {
            TriggerViewModel result = null;
            if (string.IsNullOrWhiteSpace(id))
            {
                string cn = Request.Params["CommonName"];
                if (cn == null)
                    return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
                else if (cn == string.Empty)
                    cn = null;
                result = _Flow.GetTrigger(cn);
                if (result == null)
                    return null;

            }
            else
            {
                result = _Flow.GetTrigger(Guid.Parse(id));
            }
            if (result == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden); //Unauthorized redirects which is not so good fer ember
            return new JsonHelper.JsonNetResult(new { triggers = new object[] { result } }, JsonRequestBehavior.AllowGet);

        }



        [Authorize]
        [Themed(false)]
        [HttpPost]
        [ActionName("Triggers")]
        public ActionResult CreateTrigger(TriggerViewModel m)
        {

            if (m.trigger != null)
                m = m.trigger;
            if (_Flow.CreateTrigger(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed, m.Error);
        }

        [Authorize]
        [Themed(false)]
        [HttpPut]
        [ActionName("Triggers")]
        public ActionResult UpdateTrigger(TriggerViewModel m)
        {
            if (m.trigger != null && m.id != null)
            {
                m.trigger.id = m.id;
                m = m.trigger;
            }
            if (_Flow.UpdateTrigger(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed, m.Error);
        }

        [Authorize]
        [Themed(false)]
        [HttpDelete]
        [ActionName("Triggers")]
        public ActionResult DeleteTrigger(TriggerViewModel m)
        {
            if (!m.id.HasValue)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            if (_Flow.DeleteTrigger(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed, m.Error);

        }

        [Authorize]
        [Themed(false)]
        [HttpGet]
        [ActionName("Reports")]
        public ActionResult GetReports()
        {
            return new JsonHelper.JsonNetResult(_Flow.Report(), JsonRequestBehavior.AllowGet);

        }

        [Authorize]
        [Themed(false)]
        [ActionName("Companies")]
        public ActionResult GetCompany(CompanyViewModel m)
        {
            var result = _Flow.GetCompany(m);
            if (result != null)
                return new JsonHelper.JsonNetResult(new { companies = result }, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden); //Unauthorized redirects which is not so good fer ember

        }


        [Authorize]
        [Themed(false)]
        [HttpPost]
        [ActionName("Companies")]
        public ActionResult CreateCompany(CompanyViewModel m)
        {
            if (m.company != null)
                m = m.company;
            if (_Flow.CreateCompany(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed, m.Error);
        }

        [Authorize]
        [Themed(false)]
        [HttpPut]
        [ActionName("Companies")]
        public ActionResult UpdateCompany(CompanyViewModel m)
        {
            if (m.company != null && m.id != null)
            {
                m.company.id = m.id;
                m = m.company;
            }
            if (_Flow.UpdateCompany(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed, m.Error);
        }

        [Authorize]
        [Themed(false)]
        [HttpDelete]
        [ActionName("Companies")]
        public ActionResult DeleteCompany(CompanyViewModel m)
        {
            if (!m.id.HasValue)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            if (_Flow.DeleteCompany(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed, m.Error);

        }



        [Authorize]
        [Themed(false)]
        [ActionName("TriggerGraphs")]
        public ActionResult GetTriggerGraph(TriggerGraphViewModel m)
        {
            if (_Flow.GetTriggerGraph(m))
                return new JsonHelper.JsonNetResult(new { triggerGraphs = m.triggerGraphs }, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden); //Unauthorized redirects which is not so good fer ember
        }

        [Authorize]
        [Themed(false)]
        [HttpPost]
        [ActionName("TriggerGraphs")]
        public ActionResult CreateTriggerGraph(TriggerGraphViewModel m)
        {
            if (m.triggerGraph != null)
                m = m.triggerGraph;
            if (_Flow.CreateTriggerGraph(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed, m.Error);
        }
        [Authorize]
        [Themed(false)]
        [HttpPut]
        [ActionName("TriggerGraphs")]
        public ActionResult UpdateTriggerGraph(TriggerGraphViewModel m)
        {
            if (m.triggerGraph != null && m.id != null)
            {
                m.triggerGraph.id = m.id;
                m = m.triggerGraph;

            }
            if (_Flow.UpdateTriggerGraph(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed, m.Error);
        }
        
        [Authorize]
        [Themed(false)]
        [HttpDelete]
        [ActionName("TriggerGraphs")]
        public ActionResult DeleteTriggerGraph(TriggerGraphViewModel m)
        {
            if (!m.id.HasValue)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            if (_Flow.DeleteTriggerGraph(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed, m.Error);

        }


        [Authorize]
        [Themed(false)]
        [ActionName("ResponseData")]
        public ActionResult GetResponseData(string id)
        {
            Guid gid;
            if (string.IsNullOrWhiteSpace(id) || !Guid.TryParse(id, out gid))
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            var result = _Flow.GetResponseData(gid);
            if (result != null)
                return new JsonHelper.JsonNetResult(new { responseData = result.ToArray() }, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden); //Unauthorized redirects which is not so good fer ember
        }

        [ValidateInput(false)]
        [Authorize]
        [Themed(false)]
        public JsonResult GetWorkflows(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Json(new SelectListItem[] { }, JsonRequestBehavior.AllowGet);
            Guid tid;
            if (id.Contains(',') || Guid.TryParse(id, out tid))
            {
                List<Guid> lid = new List<Guid>();
                var ids = id.Split(',');
                foreach (var gid in ids)
                {
                    if (Guid.TryParse(gid, out tid))
                        lid.Add(tid);
                    return Json(_Flow.GetWorkflows(lid.ToArray()), JsonRequestBehavior.AllowGet);
                }
            }
            return Json(_Flow.GetWorkflows(id), JsonRequestBehavior.AllowGet);
        }


        [Authorize]
        [Themed(false)]
        [ActionName("Dashboards")]
        public ActionResult GetDashboard(string id)
        {
            Guid gid;
            Guid.TryParse(id, out gid);
            var result = _Flow.GetDashboard(gid);

            return new JsonHelper.JsonNetResult(new { dashboard = result ?? new object() }, JsonRequestBehavior.AllowGet);
        }

        [Authorize]
        [Themed(false)]
        [HttpPost]
        [ActionName("CopyWorkflow")]
        public ActionResult CopyWorkflow(FlowViewModel m)
        {
            var result = _Flow.CopyWorkflow(m);
            if (result)
                return new JsonHelper.JsonNetResult(new { copyWorkflow = m }, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden); //Unauthorized redirects which is not so good fer ember
        }

    }
}
