using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Xml.Linq;
using JetBrains.Annotations;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Aspects;
using Orchard.Core.Common.Models;
using Orchard.Core.XmlRpc;
using Orchard.Core.XmlRpc.Models;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Mvc.Extensions;
using Orchard.Security;
using Orchard.Mvc.Html;
using Orchard.Core.Title.Models;
using Newtonsoft.Json;
using EXPEDIT.Share.Helpers;
using System.Dynamic;
using ImpromptuInterface;
using NKD.Services;
using EXPEDIT.Flow.ViewModels;
using NKD.Helpers;

namespace EXPEDIT.Flow.Services {
    [UsedImplicitly]
    public class XmlRpcHandler : IXmlRpcHandler {
        private readonly IContentManager _contentManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly IMembershipService _membershipService;
        private readonly RouteCollection _routeCollection;
        private readonly IFlowService _Flow;
        private readonly IUsersService _users;

        public XmlRpcHandler(IContentManager contentManager,
            IAuthorizationService authorizationService, 
            IMembershipService membershipService, 
            RouteCollection routeCollection,
            IFlowService Flow,
            IUsersService users) {
            _contentManager = contentManager;
            _authorizationService = authorizationService;
            _membershipService = membershipService;
            _routeCollection = routeCollection;
            _Flow = Flow;
            _users = users;
            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        public void SetCapabilities(XElement options) {
            const string manifestUri = "http://schemas.expedit.com.au/services/manifest/Flow";
            options.SetElementValue(XName.Get("supportsSlug", manifestUri), "Yes");
        }

        public void Process(XmlRpcContext context) {
            var urlHelper = new UrlHelper(context.ControllerContext.RequestContext, _routeCollection);
            if (context.Request.MethodName == "Flow.search")
            {
                dynamic parameters = JsonConvert.DeserializeObject<ExpandoObject>(string.Format("{0}", context.Request.Params[0].Value));
                IUser user = validateUser(parameters.Username, parameters.Password);
                foreach (var driver in context._drivers)
                    driver.Process(parameters.Username);
                var result = _Flow.Search(parameters.Query, (int)parameters.Start, (int)parameters.PageSize, (SearchType)parameters.SearchType);
                context.Response = new XRpcMethodResponse().Add(result);
            }

        }    

        private IUser validateUser(string userName, string password) {
            IUser user = _membershipService.ValidateUser(userName, password);
            if (user == null) {
                throw new Orchard.OrchardCoreException(T("The username or e-mail or password provided is incorrect."));
            }

            return user;
        }

        
    }
}
