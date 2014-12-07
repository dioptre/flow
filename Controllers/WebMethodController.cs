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


using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace EXPEDIT.Flow.Controllers {

    [Themed]
    public class WebMethodController : Controller
    {      
        public IOrchardServices Services { get; set; }
        private IAutomationService _Auto { get; set; }
        public ILogger Logger { get; set; }
        private readonly IContentManager _contentManager;
        private readonly ISiteService _siteService;
        public Localizer T { get; set; }

        public WebMethodController (
            IOrchardServices services,
            IAutomationService Auto,
            IContentManager contentManager,
            ISiteService siteService,
            IShapeFactory shapeFactory
            )
        {
            _Auto = Auto;
            Services = services;
            T = NullLocalizer.Instance;
            _contentManager = contentManager;
            _siteService = siteService;
        }


        [Themed(false)]
        public ActionResult Index()
        {
            return View();
        }

        [Themed(false)]
        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        [ActionName("ExecuteMethod")]
        //[ValidateAntiForgeryToken]
        public ActionResult ExecuteMethod(string id, string reference)
        {
            //, string reference = null, string username = null, string password = null, string workflow = null,  string application = null
            if (string.IsNullOrWhiteSpace(reference))
                reference = Request.Params["reference"];
            string username = Request.Params["username"];
            string password = Request.Params["password"];
            string workflow = Request.Params["workflow"];
            string application = Request.Params["application"];


            var result = false;
            if (string.IsNullOrWhiteSpace(id))
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.MethodNotAllowed);
            var json = new StreamReader(Request.InputStream).ReadToEnd();
            if (string.IsNullOrWhiteSpace(json))
            {
                json = string.Format("{{ ReferenceID: '{0}', username: '{1}', password: '{2}', application: '{3}', GraphDataGroupID: '{4}' }}", reference, username, password, application, workflow);
            }
            var m = new AutomationViewModel
            {
                JSON = json
            };
            Guid trid;
            if (Guid.TryParse(reference, out trid))
                m.ReferenceID = trid;
            var method = id.ToUpperInvariant();
            if (!User.Identity.IsAuthenticated)
            {
                if (string.IsNullOrWhiteSpace(m.Username) || string.IsNullOrWhiteSpace(m.Password))
                    return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);
                if (!_Auto.Authenticate(m, method))
                    return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);
            }
            switch (method) {
                case "DONEXT":
                    result = _Auto.DoNext(m);
                    break;
                default:
                    return new HttpStatusCodeResult(System.Net.HttpStatusCode.NotFound);
            }
                 
            if (result)
                return new JsonHelper.JsonNetResult(true, JsonRequestBehavior.AllowGet);
            else
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.ExpectationFailed, m.Error); 
               
        }


    }
}
