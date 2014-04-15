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
            if (string.IsNullOrWhiteSpace(q))
                return new EmptyResult();
            var results = _Flow.Search(q, ViewModels.SearchType.File, 1, 20);
            return new JsonHelper.JsonNetResult(results, JsonRequestBehavior.AllowGet);     
        }


        [Themed(true)]
        public ActionResult Wiki(string q)
        {
            dynamic media = new NullableExpandoObject();
            media.Children = _mediaLibrary.GetMediaFolders(null).Select(f=> new {folderPath = f.MediaPath, name = f.Name, lastUpdated = f.LastUpdated}).ToArray();        
            var m = new WikiViewModel { Media = media };

            //var viewModel = new MediaManagerIndexViewModel
            //{
            //    DialogMode = dialog,
            //    FolderPath = folderPath,
            //    ChildFoldersViewModel = new MediaManagerChildFoldersViewModel { Children =  },
            //    MediaTypes = _mediaLibraryService.GetMediaTypes(),
            //    CustomActionsShapes = explorerShape.Actions,
            //    CustomNavigationShapes = explorerShape.Navigation,
            //};
            return View(m);
        }
    }
}
