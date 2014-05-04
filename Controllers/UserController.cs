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
using Orchard.MediaLibrary.Services;
using EXPEDIT.Flow.ViewModels;

namespace EXPEDIT.Flow.Controllers {
    
    [Themed]
    public class UserController : Controller {
        public IOrchardServices Services { get; set; }
        private IFlowService _Flow { get; set; }
        public ILogger Logger { get; set; }
        private readonly IContentManager _contentManager;
        private readonly ISiteService _siteService;
        private readonly IMediaLibraryService _mediaLibrary;

        public UserController(
            IOrchardServices services,
            IFlowService Flow,
            IContentManager contentManager,
            ISiteService siteService,
            IShapeFactory shapeFactory,
            IMediaLibraryService mediaLibrary
            )
        {
            _Flow = Flow;
            Services = services;
            T = NullLocalizer.Instance;

            _contentManager = contentManager;
            _siteService = siteService;
            _mediaLibrary = mediaLibrary;
        }

        public Localizer T { get; set; }

        [Themed(true)]
        public ActionResult Index()
        {
            return View();
        }

        [Themed(false)]
        public ActionResult Searches(string q)
        {
            int page;
            int pageSize;
            string query = Request.Params["keywords"];
            if (string.IsNullOrWhiteSpace(query) || query == "undefined")
                query = null;
            if (string.IsNullOrWhiteSpace(query))
                query = null;
            string type = Request.Params["type"];
            SearchType st = SearchType.Flow;
            if (string.IsNullOrWhiteSpace(type) || type == "undefined")
                st = SearchType.Flow;
            else if (type == "file")
                st = SearchType.File;
            else if (type == "model")
                st = SearchType.Model;
            else if (type == "flowlocation")
                st = SearchType.FlowLocation;

            bool pFound = int.TryParse(Request.Params["page"], out page);
            bool psFound = int.TryParse(Request.Params["pageSize"], out pageSize);
            var results = _Flow.Search(
                query,
                (pFound && psFound) ? (page * pageSize) + 1 : default(int?),
                psFound ? pageSize : default(int?),
                st
            );
            return new JsonHelper.JsonNetResult(new { search = results} , JsonRequestBehavior.AllowGet);
        }


        [Authorize]
        [Themed(true)]
        [HttpGet]
        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")] 
        public ActionResult Wiki(string id)
        {
            WikiViewModel m;
            m = _Flow.GetWiki(id);
            if (m == null)
                return new HttpUnauthorizedResult("Unauthorized access to protected article.");
            return View(m);
        }
     
        [Authorize]
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

        [Authorize]
        [Themed(false)]
        [HttpGet]
        public ActionResult NodeDuplicate(string id)
        {
            return new JsonHelper.JsonNetResult(_Flow.GetDuplicateNode(id), JsonRequestBehavior.AllowGet);
        }

        [Authorize]
        [Themed(false)]
        [HttpGet]
        [ActionName("Nodes")]
        public ActionResult GetNode(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return new JsonHelper.JsonNetResult(_Flow.GetNode(null, null, null, false), JsonRequestBehavior.AllowGet);
            Guid temp;
            if (!Guid.TryParse(id, out temp))
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed);
            return new JsonHelper.JsonNetResult(_Flow.GetNode(null, temp, null, true), JsonRequestBehavior.AllowGet);
        }

        [Authorize]
        [Themed(false)]
        [HttpPost]
        [ActionName("Nodes")]
        public ActionResult CreateNode(FlowViewModel m)
        {
            if (m.node != null && m.node.id != null)
                m = m.node;
            if (_Flow.CreateNode(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed);    
        }

        [Authorize]
        [Themed(false)]
        [HttpPut]
        [ActionName("Nodes")]
        public ActionResult UpdateNode(FlowViewModel m)
        {
            if (m.node != null && m.node.id != null)
                m = m.node;
            if (_Flow.UpdateNode(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed);    
        }

        [Authorize]
        [Themed(false)]
        [HttpDelete]
        [ActionName("Nodes")]
        public ActionResult DeleteNode(FlowViewModel m)
        {
            if (m.node != null && m.node.id != null)
                m = m.node;
            if (_Flow.DeleteNode(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed);   
        }

    
        [Authorize]
        [Themed(false)]
        [HttpPost]
        [ActionName("Edges")]
        public ActionResult CreateEdge(FlowEdgeViewModel m)
        {
            if (m.edge != null && m.edge.id != null)
                m = m.edge;
            if (_Flow.CreateEdge(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed);   
        }

        [Authorize]
        [Themed(false)]
        [HttpDelete]
        [ActionName("Edges")]
        public ActionResult DeleteEdge(FlowEdgeViewModel m)
        {
            if (m.edge != null && m.edge.id != null)
                m = m.edge;
            if (_Flow.DeleteEdge(m))
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed);   
        }

        [Themed(true)]
        public ActionResult Test()
        {
            return View();
        }

    }
}
