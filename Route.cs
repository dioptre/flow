using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Routing;
using Orchard.Mvc.Routes;

namespace EXPEDIT.Flow
{
    public class Routes : IRouteProvider
    {
        public void GetRoutes(ICollection<RouteDescriptor> routes)
        {
            foreach (var routeDescriptor in GetRoutes())
                routes.Add(routeDescriptor);
        }

        public IEnumerable<RouteDescriptor> GetRoutes()
        {
            return new[] {
                new RouteDescriptor {
                    Priority = 5,
                    Route = new Route(
                        "Flow/{controller}/{action}/{id}",
                        new RouteValueDictionary {
                            {"area", "EXPEDIT.Flow"},
                            {"controller", "User"},
                            {"action", "Index"}
                        },
                        new RouteValueDictionary {
                            {"area", "EXPEDIT.Flow"},
                            {"controller", "User"}
                        },
                        new RouteValueDictionary {
                            {"area", "EXPEDIT.Flow"}
                        },
                        new MvcRouteHandler())
                },
                 new RouteDescriptor {
                    Priority = 5,
                    Route = new Route(
                        "Flow/{action}/{id}/{name}/{contactid}",
                        new RouteValueDictionary {
                            {"area", "EXPEDIT.Flow"},
                            {"controller", "User"}                            
                        },
                        new RouteValueDictionary {
                            {"area", "EXPEDIT.Flow"},
                            {"controller", "User"},                          
                        },
                        new RouteValueDictionary {
                            {"area", "EXPEDIT.Flow"},
                            {"controller", "User"}
                        },
                        new MvcRouteHandler())
                },
                 new RouteDescriptor {
                    Priority = 5,
                    Route = new Route(
                        "Flow/{action}/{id}/{name}",
                        new RouteValueDictionary {
                            {"area", "EXPEDIT.Flow"},
                            {"controller", "User"}                            
                        },
                        new RouteValueDictionary {
                            {"area", "EXPEDIT.Flow"},
                            {"controller", "User"},                          
                        },
                        new RouteValueDictionary {
                            {"area", "EXPEDIT.Flow"},
                            {"controller", "User"}
                        },
                        new MvcRouteHandler())
                },
                 new RouteDescriptor {
                    Priority = 5,
                    Route = new Route(
                        "Flow/{action}/{id}",
                        new RouteValueDictionary {
                            {"area", "EXPEDIT.Flow"},
                            {"controller", "User"}                            
                        },
                        new RouteValueDictionary {
                            {"area", "EXPEDIT.Flow"},
                            {"controller", "User"},                          
                        },
                        new RouteValueDictionary {
                            {"area", "EXPEDIT.Flow"},
                            {"controller", "User"}
                        },
                        new MvcRouteHandler())
                },
                new RouteDescriptor {
                        Priority = 50,
                        Route = new Route(
                            "Flow/searches/{*q}",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "searches"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                },
                new RouteDescriptor {
                        Priority = 5,
                        Route = new Route(
                            "Flow/nodes",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "nodes"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                },
                new RouteDescriptor {
                        Priority = 5,
                        Route = new Route(
                            "Flow/nodegroups",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "nodegroups"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                },
                new RouteDescriptor {
                        Priority = 5,
                        Route = new Route(
                            "Flow",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "index"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                },
                new RouteDescriptor {
                        Priority = 5,
                        Route = new Route(
                            "Flow/edges",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "edges"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                },
                new RouteDescriptor {
                        Priority = 5,
                        Route = new Route(
                            "Flow/Wiki",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "wiki"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                },
                new RouteDescriptor {
                        Priority = 5,
                        Route = new Route(
                            "Flow/MyUserInfo",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "myuserinfo"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                },
                new RouteDescriptor {
                        Priority = 5,
                        Route = new Route(
                            "Flow/workflows",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "workflows"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                },
                new RouteDescriptor {
                        Priority = 5,
                        Route = new Route(
                            "Flow/MyFiles",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "myfiles"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                },
                new RouteDescriptor {
                        Priority = 5,
                        Route = new Route(
                            "Flow/MyNodes",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "mynodes"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                },
                new RouteDescriptor {
                        Priority = 5,
                        Route = new Route(
                            "Flow/MyWorkflows",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "MyWorkflows"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                },
                new RouteDescriptor {
                        Priority = 5,
                        Route = new Route(
                            "Flow/MySecurityLists",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "MySecurityLists"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                },
                new RouteDescriptor {
                        Priority = 5,
                        Route = new Route(
                            "Flow/MyLicenses",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "MyLicenses"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                },
                new RouteDescriptor {
                        Priority = 5,
                        Route = new Route(
                            "Flow/MyProfiles",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "MyProfiles"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                },
                new RouteDescriptor {
                        Priority = 5,
                        Route = new Route(
                            "Flow/NodeDuplicate",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "NodeDuplicate"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                },
                new RouteDescriptor {
                        Priority = 5,
                        Route = new Route(
                            "Flow/WorkflowDuplicate",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "WorkflowDuplicate"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                },
                new RouteDescriptor {
                        Priority = 5,
                        Route = new Route(
                            "Flow/NodeDuplicateID",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "NodeDuplicateID"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                },
                new RouteDescriptor {
                        Priority = 5,
                        Route = new Route(
                            "Flow/WorkflowDuplicateID",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "WorkflowDuplicateID"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                },
                new RouteDescriptor {
                        Priority = 5,
                        Route = new Route(
                            "Flow/CheckNodePermission",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "CheckNodePermission"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                },
                new RouteDescriptor {
                        Priority = 5,
                        Route = new Route(
                            "Flow/CheckWorkflowPermission",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "CheckWorkflowPermission"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                },
                new RouteDescriptor {
                        Priority = 5,
                        Route = new Route(
                            "Flow/Preview",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "Preview"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                },
                new RouteDescriptor {
                        Priority = 5,
                        Route = new Route(
                            "Flow/translations",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "translations"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                },
                new RouteDescriptor {
                        Priority = 5,
                        Route = new Route(
                            "Flow/locales",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "locales"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                },
                 new RouteDescriptor {
                    Priority = 15,
                    Route = new Route(
                        "Flow/WebMethod/{id}",
                        new RouteValueDictionary {
                            {"area", "EXPEDIT.Flow"},
                            {"controller", "WebMethod"},
                            {"action", "ExecuteMethod"}
                        },
                        new RouteValueDictionary {
                            {"area", "EXPEDIT.Flow"},
                            {"controller", "WebMethod"}
                        },
                        new RouteValueDictionary {
                            {"area", "EXPEDIT.Flow"}
                        },
                        new MvcRouteHandler())
                },
                 new RouteDescriptor {
                    Priority = 15,
                    Route = new Route(
                        "Flow/WebMethod/{id}/{reference}",
                        new RouteValueDictionary {
                            {"area", "EXPEDIT.Flow"},
                            {"controller", "WebMethod"},
                            {"action", "ExecuteMethod"}
                        },
                        new RouteValueDictionary {
                            {"area", "EXPEDIT.Flow"},
                            {"controller", "WebMethod"}
                        },
                        new RouteValueDictionary {
                            {"area", "EXPEDIT.Flow"}
                        },
                        new MvcRouteHandler())
                },
                new RouteDescriptor {
                        Priority = 5,
                        Route = new Route(
                            "Flow/steps",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "steps"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                },
                new RouteDescriptor {
                        Priority = 5,
                        Route = new Route(
                            "Flow/projects",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "projects"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                },
                new RouteDescriptor {
                        Priority = 5,
                        Route = new Route(
                            "Flow/projectData",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "projectData"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                },                
                new RouteDescriptor {
                        Priority = 5,
                        Route = new Route(
                            "Flow/EdgeConditions",
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"},
                                {"controller", "User"},
                                {"action", "EdgeConditions"}
                            },
                            null,
                            new RouteValueDictionary {
                                {"area", "EXPEDIT.Flow"}
                            },
                            new MvcRouteHandler())
                }

            };
        }
    }
}