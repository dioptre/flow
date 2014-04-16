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
        public ActionResult Search(string q)
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
        public ActionResult Wiki(string q)
        {
            WikiViewModel m;
            m = _Flow.GetWiki(q);
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
                return new RedirectResult(System.Web.VirtualPathUtility.ToAbsolute(string.Format("~/flow/search#/article/{0}", m.GraphDataID)));
        }

        [Authorize]
        [Themed(false)]
        [HttpGet]
        public ActionResult WikiDuplicate(string id)
        {
            return new JsonHelper.JsonNetResult(_Flow.GetDuplicateWiki(id), JsonRequestBehavior.AllowGet);
        }

    }
}
