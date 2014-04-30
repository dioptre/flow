﻿using System.Collections.Generic;
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
                }

            };
        }
    }
}