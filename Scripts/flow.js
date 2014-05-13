Ember.FEATURES["query-params"] = true;


App = Ember.Application.create({
    // LOG_TRANSITIONS: true,
    rootElement: '#emberapphere'
});

App.Router.map(function () {
    this.route('graph', {path: 'process/:id'});
    this.route('wikipedia', { path: "/wikipedia/:id" });
    this.route('search');
});

// App.LoadingRoute = Ember.Route.extend({
//   activate: function() {
//     this._super();
//     return Pace.restart();
//   },
//   deactivate: function() {
//     this._super();
//     return Pace.stop();
//   }
// });


// App.ApplicationRoute = Ember.Route.extend({
//   actions: {
//     loading: function() {
//       NProgress.start();
//       this.router.one('didTransition', function() {
//         return setTimeout((function() {
//           return NProgress.done();
//         }), 50);
//       });
//       return true;
//     },
//     error: function() {
//       return setTimeout((function() {
//         return NProgress.done();
//       }), 50);
//     }
//   }
// });



App.ApplicationRoute = Ember.Route.extend({
      actions: {
      //  openModal: function(viewName, param1, param2) {
      //      // param1 - optional callback or data object
      //      // param2 - optional callback, only if data object is used for param 1

      //      // Names for modals should be made up in the following way
      //      // ===>>> Controller Name + 'Modal' + Name for modal

      //      // Step 1: Get Controller Name
      //      var controllerName = viewName.match(/^(.*)Modal(.*)$/)[1]; // This gets controller Name from model


      //      // Step 3: Update Query parmas
      //      this.set('controller.m', viewName);  // Add to URL

      //       // Check if param 1 is a function
      //      if (typeof param1 === 'function') {
      //          this.controllerFor(viewName).set('callbackData', param1);
      //      } else if (param1 !== null) {
      //          // Must be data if not function
      //          this.controllerFor(viewName).set('model', param1)

      //          // Check if param 2 is used
      //          if (param2 !== null) {
      //              this.controllerFor(viewName).set('callbackData', param2);
      //          }
      //      }


      //      return this.render(viewName, {
      //          into: 'application',
      //          outlet: 'modal'
      //      });
      //  },

      //  closeModal: function() {
      //    this.set('controller.m', ""); // Clean up URL

      //    return this.disconnectOutlet({
      //      outlet: 'modal',
      //      parentView: 'application'
      //    }); // Remove from outlet
      //  }
      }
});

//App.ModalController = Ember.ObjectController.extend({
//    actions: {
//        close: function () {
//            return this.send('closeModal');
//        }
//    }
//});

// A tiny bit more modal code
//App.ModalDialogComponent = Ember.Component.extend({
//    model: null,
//    title: 'Modal Title',
//    btnClose: "Close",
//    btnSubmit: 'Submit',
//    actions: {
//        close: function () {
//            return this.sendAction();
//        },
//        submit: function () {
//            return this.sendAction('submit');
//        }
//    }
//});


//App.Modal = Ember.Mixin.create({
//    callbackData: null,
//    actions: {
//        close: function () {
//            return this.send('closeModal');
//        },
//        submit: function () {
//            console.log(this.get('model.content'));
//            if (this.callbackData) {
//                this.callbackData(this.get('model'));
//            }
//            return this.send('closeModal');
//        }
//    }
//});

//App.GraphModalNewWorkflowController = Ember.ObjectController.extend(App.Modal, {

//});


App.ApplicationController = Ember.Controller.extend({
    currentPathDidChange: function () {
        App.set('currentPath', this.get('currentPath'));
    }.observes('currentPath'), // This set the current path App.get('currentPath');
    m: '',
    queryParams: ['m'],
    needs: ['graph', 'wikipedia', 'search']
    // Add some code here to watch for modals and open them if it happens
});


App.ApplicationAdapter = DS.RESTAdapter.extend({
    namespace: 'flow',
    headers: {
        __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
    },
    generateIdForRecord: function (store, record) {
        var uuid = NewGUID();
        return uuid;
    }
});





App.IndexRoute = Ember.Route.extend({
    beforeModel: function () {
        this.transitionTo("search");
    }
});



App.Search = DS.Model.extend({
    "Row": DS.attr(''),
    "TotalRows": DS.attr(''),
    "Score": DS.attr(''),
    "ReferenceID": DS.attr(''),
    "TableType": DS.attr(''),
    "Title": DS.attr(''),
    "Description": DS.attr(''),
    "SpatialJSON": DS.attr(''),
    "InternalURL": DS.attr(''),
    "ExternalURL": DS.attr(''),
    "ResourcePath": function(){
        return '/share/file/' + this.get('ReferenceID');
    }.property('ReferenceID'),
    "Author": DS.attr(''),
    "Updated": DS.attr('')
});


App.SearchSerializer = DS.RESTSerializer.extend({
    extractArray: function (store, type, payload, id, requestType) {
        var results = payload.search;
        results.forEach(function (result) {
            result.id = NewGUID();
        });
        payload = { Searches: results };
        return this._super(store, type, payload, id, requestType);
    }
});



App.SearchController = Ember.ObjectController.extend({
    needs: ['graphResults','mapResults','fileResults'],
    queryParams: ['keywords', 'tags', 'graph', 'file', 'map', 'pageGraph', 'pageFile', 'pageMap', 'pageSize'],
    graph: true,
    file: true,
    map: false,
    activeResultsClass: function(){
        var i = 0;
        if (this.get('graph')) i++;
        if (this.get('file')) i++;
        if (this.get('map')) i++;
        $(window).trigger('redrawMap');
        if (i===0) return '';
        return 'span' + (12 / i);
    }.property('graph', 'file', 'map'),
    pageGraph: 0,
    pageFile: 0,
    pageMap: 0,
    pageSize: 50,
    pageSizes: [5, 10, 20, 50, 100, 200],
    keywords: '',
    tags: [],
    sched_date_from: "",
    sched_date_to: "",
    searchLocation: "",
    searchText: "",
    dateModal: false,
    actions: {
        toggleDateModal: function(){
            this.toggleProperty('dateModal');
        },
        next: function (i) {
            this.incrementProperty(i);
            //console.log('next Page for ', i)
        },
        prev: function (i) {
            this.decrementProperty(i);
            //console.log('previous Page for ', i)
        },
        newProcess: function () {
            this.transitionToRoute('graph', NewGUID());
        },
        search: function () {
            this.loadAll();
            // On button click. Transition node.
            // this.transitionToRoute('search', 0, temp)
        },
        deleteTag: function (tag) {
            this.get('tags').removeObject(tag);
        },
        showDateModal: function () {
            return Bootstrap.ModalManager.show('dateModal');
        },
        addDate: function () {
            var controller = this;
            var f = this.get('sched_date_from')
            var t = this.get('sched_date_to');
            var a = [f, t].map(function (i) { return moment.utc(i).format("DD/MM/YYYY") }).join(' - ');
            this.get('tags').addObject({
                n: a,
                d: f + '-' + t
            });

            return this.toggleProperty('dateModal');
        },
        showLocationModal: function () {
            return Bootstrap.ModalManager.show('locationModal');
        },
        addLocation: function () {
            var controller = this;
            var location = controller.get('searchLocation');
            if (location === '') {
                location = 'Custom Location';
            }

            this.get('tags').addObject({
                n: location,
                l: lastMapUpdates
            });
            return Bootstrap.ModalManager.hide('locationModal');
        }
    },
    loadGraph: function(){
        var controller = this;
        if (this.get('graph')) {
            controller.set('controllers.graphResults.loading', true);
            this.store.find('search', {
                page: this.get('pageGraph'),
                keywords: this.get('keywords'),
                tags: this.get('tags'),
                type: 'flow',
                pagesize: this.get('pageSize')
            }).then(function (res) {
                controller.set('controllers.graphResults.results', res.get('content'));
                controller.set('controllers.graphResults.loading', false);
            });
        }
    }.observes('graph', 'pageGraph'),
    loadMap: function(){
        var controller = this;
        if (this.get('map')) {
            controller.set('controllers.mapResults.loading', true);
            this.store.find('search', {
                page: this.get('pageMap'),
                keywords: this.get('keywords'),
                tags: this.get('tags'),
                type: 'flowlocation',
                pagesize: this.get('pageSize')
            }).then(function (res) {
                controller.set('controllers.mapResults.results', res.get('content'));
                controller.set('controllers.mapResults.loading', false);
            });
        }
    }.observes('map', 'pageMap'),
    loadFile: function(){
        var controller = this;
        if (this.get('file')) {
            controller.set('controllers.fileResults.loading', true);
            this.store.find('search', {
                page: this.get('pageFile'),
                keywords: this.get('keywords'),
                tags: this.get('tags'),
                type: 'file',
                pagesize: this.get('pageSize')
            }).then(function (res) {
                controller.set('controllers.fileResults.results', res.get('content'));
                controller.set('controllers.fileResults.loading', false);
            });
        }
    }.observes('file', 'pageFile'),
    loadAll: function(){
        //console.log('loading all');
        this.loadGraph();
        this.loadMap();
        this.loadFile();
    }.observes('pageSize').on('didInsertElement'), // the did insert element here doesn't actuall work that why the view is setup below to kickoff the initial search
    searchQuery: function () { // this builds the search query
        var controller = this;
        this.set('pageGraph', 0);
        this.set('pageFile', 0);
        this.set('pageMap', 0);
        Ember.run.debounce(this, controller.loadAll, 1000);
    }.observes('keywords'),
    componentURI: function () {
        var args = this.get('keywords');
        if (typeof args === 'string' && args !== null && args.length > 0)
            return args.replace(/ /g, '_');
        else
            return '';
    }.property('keywords')
});


App.SearchView = Ember.View.extend({
    didInsertElement: function () {
        this.get('controller').send('search');
        // this code is here since you can't watch for didInsertElement in Controllers
    }
});






App.GraphResultsController = Ember.ObjectController.extend({
    needs: 'search',
    next: false,
    prev: false,
    loading: true,
    page: Ember.computed.alias('controllers.search.pageGraph'),
    pageSize: Ember.computed.alias('controllers.search.pageSize'),
    resultsUpdated: function(){

        if (this.get('results')[0]){
            // Total Rows
            var totalRows = this.get('results')[0].get('TotalRows');
            var pageSize = this.get('pageSize');
            var page = this.get('page');

            // From ths info it can be calculate if next and prev should be true or false
            this.set('prev', (page > 0));

            var nOfPages = Math.ceil(totalRows / pageSize) - 1;
            this.set('next', (nOfPages > page));
        }

    }.observes('results'),
    results: []
});


App.FileResultsController = Ember.ObjectController.extend({
    needs: 'search',
    next: false,
    prev: false,
    loading: true,
    page: Ember.computed.alias('controllers.search.pageFile'),
    pageSize: Ember.computed.alias('controllers.search.pageSize'),
    resultsUpdated: function(){

        if (this.get('results')[0]){

            // Total Rows
            var totalRows = this.get('results')[0].get('TotalRows');
            var pageSize = this.get('pageSize');
            var page = this.get('page');

            // From ths info it can be calculate if next and prev should be true or false
            this.set('prev', (page > 0));

            var nOfPages = Math.ceil(totalRows / pageSize) - 1;
            this.set('next', (nOfPages > page));

        }


    }.observes('results'),
    results: []
});

App.MapResultsController = Ember.Controller.extend({
    _visi: function () {
        if (this.get('results').length === 0) {
            this.set('visi', false);
        }
        if (this.loading) {
            this.set('visi', false);
        }
        else {
            this.set('visi', true);
        }
    }.observes('loading', 'results'),
    visi: true,
    needs: 'search',
    next: false,
    prev: false,
    loading: true,
    page: Ember.computed.alias('controllers.search.pageMap'),
    pageSize: Ember.computed.alias('controllers.search.pageSize'),
    resultsUpdated: function () {

        if (this.get('results')[0]) {
            // Total Rows
            var totalRows = this.get('results')[0].get('TotalRows');
            var pageSize = this.get('pageSize');
            var page = this.get('page');

            // From ths info it can be calculate if next and prev should be true or false
            this.set('prev', (page > 0));

            var nOfPages = Math.ceil(totalRows / pageSize) - 1;
            this.set('next', (nOfPages > page));
        }

    }.observes('results'),
    results: [],
    resultsGeo: function () {
        var results = this.get('results');
        var geos = {};
        $.each(results, function (i, a) {
            var geo = JSON.parse(a.get('SpatialJSON'));
            if (!geos[geo.id]) {
                geos[geo.id] = { name: '<a href="/flow/#/graph/' + a.get('ReferenceID') + '">' + a.get('Title') + '</a>', id: geo.id, geo: geo.data };
            }
            else {
                geos[geo.id].name += '<br/><a href="/flow/#/graph/' + a.get('ReferenceID') + '">' + a.get('Title') + '</a>';
            }
        });

        return geos;

    }.property('results')
});


App.MapResultComponent = Ember.Component.extend({
    map: null,
    visi: false,
    _id: null,
    id: function () {
        if (this._id === null)
            this._id = NewGUID();
        return this._id;
    }.property(),
    resultsGeo: [], //Expects geo (point data), name, id in array
    mapReady: false,
    intialize: function () {
        var component = this;
        if (component.get('visi'))
            component.update();
        //alert(component._id);
        $(window).on('redrawMap', function () {
            component.loading();
        });
    }.on('didInsertElement'),
    update: function () {
        var component = this;
        Ember.RSVP.allSettled([deferredMap.promise]).then(function (array) {
            var geos = component.get('resultsGeo');
            var updateMapComponent = function () {
                DeleteShapes(component.map);
                $.each(geos, function (i, a) {
                    var geoData = ParseGeographyData(a.geo);
                    AddMarkerSingle(component.map, GetFirstLocation(geoData), false, a.name, a.id);
                });
                window.setTimeout(function () { google.maps.event.trigger(component.map, 'resize'); }, 100);
                RefocusMap(component.map);
            };
            if (component.map === null) {
                component.map = SetupMap(component._id);
                window.setTimeout(updateMapComponent, 100);
            }
            else
                window.setTimeout(updateMapComponent, 1);
        });
    }.observes('resultsGeo'),
    loading: function () {
        var tempmap = this.get('map');
        if (typeof google != 'undefined' && typeof tempmap != 'undefined' && tempmap !== null) {
            if (this.get('visi'))
                $("#" + this.get('id')).show();
            else
                $("#" + this.get('id')).hide();
            window.setTimeout(function () { google.maps.event.trigger(tempmap, 'resize'); RefocusMap(tempmap); }, 100);
        }
    }.observes('visi').on('parentViewDidChange')
});

var drawing = false;
var isMapSetup = false, isMapLoaded=false, isMapInitialized = false;
var cmap;
var deferredMap = Ember.RSVP.defer();
function MapInitialize() {
    if (!isMapInitialized) {
        LoadScript(mapHelper);
        //Ember.run.later(function () { deferredMap.resolve("Map Loaded"); },1800); //Moved to NKD Map JS
    }
}


function LoadMap() {
    if (!isMapLoaded) {
        isMapLoaded = true;
        // LoadScript('https://maps.googleapis.com/maps/api/js?v=3.exp&sensor=false&callback=MapInitialize');
        LoadScript('http://maps.googleapis.com/maps/api/js?libraries=drawing&sensor=true&callback=MapInitialize');

    }
}
Ember.run.scheduleOnce('afterRender', this, LoadMap); //HACK Todo


var lastMapUpdates;
function OnMapUpdate(map, event, center, viewport) {
    cmap = map;
    if (event.eventType == "EDITED") {
        if (event.eventSource.type == "marker") {
            DeleteExceptedShape(map, event.eventSource);
        }
        else {
            DeleteExceptedShapeTypes(map);
        }
    }
    if (event.eventType == "BOUNDS_CHANGED") {
        //console.log(event);
        lastMapUpdates = viewport;
    }
}





//(function () {
//    App.DateModal = Bootstrap.BsModalComponent.extend({
//        didInsertElement: function () {
//            this._super();
//        },
//        map: null,
//        becameVisible: function () {
//            this._super();
//            if (!isMapSetup) {
//                LoadMap();
//                isMapSetup = true;
//                if (drawing)
//                    this.map = SetupDrawingMap('map-search');
//                else
//                    this.map = SetupMap('map-search');
//                RedrawMap(this.map);
//                var smap = this.map;
//                $("#searchLocation").autocomplete({
//                    delay: 100,
//                    source: function (request, response) {
//                        $.ajax({
//                            url: "/share/getlocations/" + request.term,
//                            type: "GET",
//                            dataType: "json",
//                            //data: { id: request.term },
//                            success: function (data) {
//                                response($.map(data, function (item) {
//                                    return {
//                                        label: item.Text, value: item.Text, id: item.Value
//                                    };
//                                }));
//                            }
//                        });
//                    },
//                    focus: function (event, ui) {
//                        AddGeographyUnique(smap, JSON.parse(ui.item.id).spatial, false, ui.item.label, true, NewGUID());
//                        RefocusMap(smap);
//                        smap.setZoom(9);
//                    },
//                    response: function (event, ui) {
//                        if (ui.content.length > 0) {
//                            AddGeographyUnique(smap, JSON.parse(ui.content[0].id).spatial, false, ui.content[0].label, true, NewGUID());
//                            RefocusMap(smap);
//                            smap.setZoom(9);
//                        }
//                        else {
//                            GetAddressLocation($("#searchLocation").val(), function (latlng) {
//                                DeleteShapes(smap);
//                                AddMarkerSingle(smap, latlng, false, $("#searchLocation").val(), NewGUID());
//                                RefocusMap(smap);
//                                smap.setZoom(15);
//                            });
//                        }

//                    }
//                });
//            }
//        }
//    });

//    Ember.Handlebars.helper('flow-modal', App.DateModal);
//}).call(this);


App.GraphRoute = Ember.Route.extend({
    model: function (params) {
        var id = params.id;
        if (id) {
            id = id.toLowerCase(); // just in case
            return Ember.RSVP.hash({
                data: this.store.find('node', { id: id }),
                selected: id,
                content: '',
                label: '',
                editing: false  // This gets passed to visjs to enable/disable editing dependig on context
            });
        } else {
            return Ember.RSVP.hash({
                data: this.store.find('node'),
                content: '',
                label: 'Create new Workflow!',
                editing: true
            });
        }
    },
    afterModel: function (m) {
        var sel = m.selected;
        if (sel) { // this means it's probably a wiki article
            m.node = this.store.getById('node', sel);
            if (m.node) {
                m.content = m.node.get('content');
                m.label = m.node.get('label');
            }
        } else {
            var nodes = Enumerable.From(m.data.content).Select("$._data").ToArray(); // this is to clean up ember data
            m.data = { nodes: nodes, edges: [] };
            m.selected = '';
        }
    },
    actions: {
        toggleWorkflowNameModal: function (save) {
            this.toggleProperty('controller.workflowNameModal');

            if (!this.get('controller.workflowNameModal')) {
                // must have been just closed - save results
                //console.log('save it');
            }


        }
    }
    //,setupController: function (controller, model) {
    //    controller.set('model', model);
    //    // Check if workflow name is defined, otherwise popup
    //    Ember.run.scheduleOnce('afterRender', this, function () {
    //        if (model.workflowName === null) {
    //            controller.toggleProperty('workflowNameModal');
    //        }
    //    });
    //}
});


App.GraphController = Ember.ObjectController.extend({
    editing: true,
    workflowName: null,
    workflowID: null,
    workflowNameModal: false,
    validateWorkflowName: false,
    graphDataLte2: Ember.computed.lte('graphData.length', 2),
    graphData : null,
    graphDataTrigger : function () {
        // get data bitch equiv
        var _this = this;
        var array = { nodes: [], edges: [] };
        var depthMax = 15; // currently depthMax is limited to 1 unless the data is already in ember store
        var nodeMax = -1;
        var prime = {};
        prime.edges = [];
        prime.workflows = [];
        var edgePromises = [];
        var workflowPromises = [];
        var data = this.get('model.data').content;
        prime.nodes = Enumerable.From(data).Select(
            function (f) {
                edgePromises.push(f.get('edges'));
                workflowPromises.push(f.get('workflows'));
                return {
                    id: f.get('id'), label: f.get('label'), shape: f.get('shape'), group: f.get('group')
                }
            }).ToArray();
        var addEdge = function (edges) {
            if (edges.get('length') > 0) {
                edges.forEach(function (edge) {
                    prime.edges.push({ id: edge.get('id'), from: edge.get('from'), to: edge.get('to'), color: edge.get('color'), width: edge.get('width'), style: edge.get('style') });
                });
            }
        };
        var getWorkflow = function (workflows) {
            if (workflows.get('length') > 0) {
                workflows.forEach(function (workflow) {
                    prime.workflows.push({ id: workflow.get('id'), name: workflow.get('name'), label: workflow.get('label')});
                });
            }
        };
        Ember.RSVP.allSettled([Ember.RSVP.map(edgePromises, addEdge), Ember.RSVP.map(workflowPromises, getWorkflow)])
            .then(function () {
                //debugger;
                if (!Enumerable.From(prime.workflows).Any("f=>f.id=='" + _this.get("workflowID") + "'")) {
                    var newwf = Enumerable.From(prime.workflows).FirstOrDefault();
                    if (typeof newwf !== 'undefined' && newwf) {
                        _this.set("workflowID", newwf.id);
                        _this.set("workflowName", newwf.label);
                    }
                    else {
                        _this.set("workflowID", null);
                        _this.set("workflowName", null);
                        Ember.run.scheduleOnce('afterRender', _this.get('model'), 'send', 'toggleWorkflowModal');                                
                        //_this.send('toggleWorkflowModal');
                    }
                }
                //Enumerable.From(data.get('workflows')).Where("f=>f.get('
                //var data = recurseGraphData(sel, array, this, 1, depthMax, nodeMax, 'node');
                //console.log(data);
                // var model = this.get('model')
                //m.data = data;
                //return data;
                _this.set('graphData', prime);
            });
    }.observes('model', 'selected', 'node.@each.workflows'),
    checkWorkflowName: function () {
        var _this = this;
        if (!_this.get('workflowName') || typeof _this.get('workflowName') !== 'string' || _this.get('workflowName').trim().length < 1) {
            _this.set('validateWorkflowName', 'Name required.');
            return;
        }
        _this.set('loadingWorkflowName', true);
        return new Ember.RSVP.Promise(function (resolve, reject) {
            jQuery.getJSON('/Flow/User/WorkflowDuplicate/' + encodeURIComponent(_this.get('workflowName').trim())
              ).then(function (data) {
                  _this.set('loadingWorkflowName', false);
                 Ember.run(null, resolve, data);
              }, function (jqXHR) {
                  jqXHR.then = null; // tame jQuery's ill mannered promises
                  Ember.run(null, reject, jqXHR);
              });
        }).then(function (value) {
            _this.set('validateWorkflowName', value ? 'Name already in use.' : false);
        });

    }.observes('workflowName'),
    loadingWorkflowName: false,
    changeSelected: function () {
        this.transitionToRoute('graph', this.get('model.selected'));
    }.observes('model.selected'),
    actions: {
        toggleWorkflowModal: function () {
            this.toggleProperty('workflowNameModal');
        }
    }
});


function recurseGraphData(id, array, _this, depth, depthMax, nodeMax, store) {
    //AGTODO
    //return array;
    if (typeof id == 'undefined')
        return array;

    //TODO: nodemax based on sequence (priority) in edges
    if (!nodeMax || nodeMax < 0 || array.nodes.length < nodeMax) {
        var node = _this.store.getById(store, id);
        //debugger;

        if (!node)
            return array;
        //console.log('this should happen twice')

        array.nodes.push({
            id: node.get('id'),
            label: node.get('label'),
            content: node.get('content')
        });

        //var tid = node.get('id');

        var edges = Enumerable.From(node.get('edges').content).OrderBy("$.get('sequence')").ToArray();


        if (depth <= depthMax && edges.length !== undefined) {

            //console.log('this should happen once')

            edges.forEach(function (edge) {
                if (!edge.get('from') || !edge.get('to'))
                    return;

                array.edges.push({
                    id: edge.get('id'),
                    from: edge.get('from'),
                    to: edge.get('to')
                });


                // Check if id has already been processed
                array = recurseGraphData(edge.get('to'), array, _this, depth + 1, depthMax, nodeMax, store);

            });
        }
    }

    return array;
}


App.VizEditorComponent = Ember.Component.extend({
    editing: false,
    toggleEditing: function () {
        if (this.graph !== null) {
            this.graph.setOptions({
                dataManipulation: this.get('editing')
            });
        }
    }.observes('editing'),
    data: null,
    vizDataSet: { nodes: new vis.DataSet(), edges: new vis.DataSet() },
    selected: '',
    graph: null,
    setup: function () {

        var _this = this;

        var container = $('<div>').appendTo(this.$())[0];
        var data = this.get('vizDataSet');
        var options = {
            physics: {barnesHut: {enabled: false}},
            stabilize: false,
            stabilizationIterations: 1,
            dataManipulation: this.get('editing'),
            onAdd: function (data, callback) {


                data.id = NewGUID();
                var newNode =  App.Node.store.createRecord('node', data)

                // This creates a nice modal:
                _this.sendAction('openModal', 'graphModalNewWorkflow', newNode, function(){
                    // This is what happens on submit button!!!
                    newNode.save().then(function(){
                        alert('Succesfully added!')
                    }, function(){
                        alert('Unsuccesful add!')
                    });
                })


            },
            onDelete: function (data, callback) {

                debugger;

                callback(data);
            },
            onEdit: function (data, callback) {
                debugger;

            },
            onConnect: function (data, callback) {
                function saveLink() {
                    data.id = NewGUID();
                    data.GroupID = NewGUID();
                    App.Node.store.createRecord('edge', data).save().then(function(){
                        callback(data);
                    })

                }

                debugger;

                if (data.from == data.to) {
                    var r = confirm("Do you want to connect the node to itself?");
                    if (r == true) {
                        saveLink()
                    }
                }
                else {
                    saveLink()
                }
            }
        };

        // Initialise vis.js
        this.graph = new vis.Graph(container, data, options);

        // This sets the new selected item on click
        this.graph.on('click', function (data) {
            if (data.nodes.length > 0) {
                _this.set('selected', data.nodes[0]);
            }
        });

        $(window).resize(function () {
            _this.graph.redraw(); // This makes the graph responsive!!!
        });
    },
    dataUpdates: function () {

        if (this.graph === null) {
            this.setup(); // graph hasn't been initialised yet
        }

        var md = this.get('data'); // has to be synched with data
        var d = this.get('vizDataSet');


        // Step 1: remove nodes which aren't in the d set anymore
        var delNodes = d.nodes.get({
            filter: function (i) {
                var yes = true;
                md.nodes.forEach(function (j) {
                    if (i.id === j.id) { yes = false; }
                });
                return yes;
            }
        });
        d.nodes.remove(delNodes);


        // Step 2: add all the new nodes & update nodes
        d.nodes.update(md.nodes);


        // Now same thing for edges
        var delEdges = d.edges.get({
            filter: function (i) {
                var yes = true;
                md.edges.forEach(function (j) {
                    if (i.id === j.id) { yes = false; }
                });
                return yes;
            }
        });
        d.edges.remove(delEdges);


        // This is longer than Step 2 for nodes, as edges with no exisiting nodes need to be filtered out first
        var newEdges = md.edges.filter(function (edge) {
            return (d.nodes.get(edge.from) !== null && d.nodes.get(edge.to) !== null);
        });
        d.edges.update(newEdges);


        // Make sure all items are displayed in view
        this.graph.zoomExtent();


    }.observes('data').on('didInsertElement')
});

    // didInsertElement: function () {

    //     var options = {
    //         dataManipulation: true,
    //         keyboard: true,
    //         onAdd: function (data, callback) {
    //             var span = document.getElementById('operation');
    //             var idInput = document.getElementById('node-id');
    //             var labelInput = document.getElementById('node-label');
    //             var saveButton = document.getElementById('saveButton');
    //             var cancelButton = document.getElementById('cancelButton');
    //             var div = document.getElementById('graph-popUp');
    //             span.innerHTML = "Add Node";
    //             idInput.value = data.id;
    //             labelInput.value = data.label;
    //             saveButton.onclick = saveData.bind(this, data, callback);
    //             cancelButton.onclick = clearPopUp.bind();
    //             div.style.display = 'block';
    //         },
    //         onDelete: function (data, callback) {

    //             // Delete all nodes
    //             $.each(data.nodes, function (i, a) {
    //                 //console.log("nodes: ", i, a)
    //                 var node = App.Node.store.getById('node', a);
    //                 node.deleteRecord();
    //                 // node.save(); //not working, but should maybe need to connect to api first
    //             })


    //             // Delete all nodes
    //             $.each(data.edges, function (i, a) {
    //                 //console.log("edges: ", i, a)
    //                 var edge = App.Edge.store.getById('edge', a);
    //                 edge.deleteRecord();
    //                 // edge.save();
    //             })



    //             callback(data);
    //         },
    //         onEdit: function (data, callback) {
    //             var span = document.getElementById('operation');
    //             var idInput = document.getElementById('node-id');
    //             var labelInput = document.getElementById('node-label');
    //             var saveButton = document.getElementById('saveButton');
    //             var cancelButton = document.getElementById('cancelButton');
    //             var div = document.getElementById('graph-popUp');
    //             span.innerHTML = "Edit Node";
    //             idInput.value = data.id;
    //             labelInput.value = data.label;
    //             saveButton.onclick = saveData.bind(this, data, callback);
    //             cancelButton.onclick = clearPopUp.bind();
    //             div.style.display = 'block';
    //         },
    //         onConnect: function (data, callback) {
    //             function saveLink() {
    //                 data.id = NewGUID();
    //                 data.groupid = sessionGroupID;
    //                 App.Node.store.createRecord('edge', data).save()
    //                 callback(data);
    //             }

    //             if (data.from == data.to) {
    //                 var r = confirm("Do you want to connect the node to itself?");
    //                 if (r == true) {
    //                     saveLink()
    //                 }
    //             }
    //             else {
    //                 saveLink()
    //             }
    //         }
    //     };

    //     var container = this.$().find('#mygraph')[0];

    //     // Data was created in the route
    //     graph = new vis.Graph(container, data, options);

    //     $(window).resize(function(){
    //         graph.redraw();
    //     })

    //     graph.on('click', function (data) {
    //         //console.log(data, 'click event')
    //         if (data.nodes.length > 0) {
    //             App.Node.store.findQuery('node', { id: data.nodes[0] }).then(function (updated) {
    //                 var c = updated.get('content');
    //                 if (c && c[0]) {
    //                     var record = App.Node.store.getById('node', data.nodes[0]);
    //                     record.set('content', c[0].get('content'))
    //                     $('#flowItem').html(filterData(record.get('content')));
    //                     $('#flowEditLink').attr('href', '/flow/wiki/' + record.get('label'));
    //                     $('#flowEditLink').html('Edit ' + record.get('label'));
    //                     $('#flowEdit').show();

    //                 }
    //             });


    //         }
    //     })


    // }



function clearPopUp() {
    var saveButton = document.getElementById('saveButton');
    var cancelButton = document.getElementById('cancelButton');
    saveButton.onclick = null;
    cancelButton.onclick = null;
    var div = document.getElementById('graph-popUp');
    div.style.display = 'none';

}

function saveData(data, callback) {
    var idInput = document.getElementById('node-id');
    var labelInput = document.getElementById('node-label');
    var div = document.getElementById('graph-popUp');
    data.id = idInput.value;
    data.label = labelInput.value;
    clearPopUp();

    //console.log('Save data function: ', data)

    if (App.Node.store.hasRecordForId('node', data.id)) {
        // already exist - just update record
        var record = App.Node.store.getById('node', data.id);

        record.set('label', data.label);
        record.save();

    } else {
        App.Node.store.createRecord('node', data).save();
    }

    callback(data);

}


DS.RESTAdapter.reopen({
    namespace: 'flow'
});


App.EdgeSerializer = DS.RESTSerializer.extend({
    extractArray: function (store, type, payload, id, requestType) {
        return []; //makes sure it does nothing - edges are created as part of the node serializer
    }
});


App.NodeSerializer = DS.RESTSerializer.extend({
    extractArray: function (store, type, payload, id, requestType) {
        //AGTODO
        //return [];
        var nodes = payload.Nodes;
        var edges = payload.Edges;
        var workflows = payload.Workflows;

        // If there are no nodes, just return null for everything
        if (nodes === null) {
            payload = { "Nodes": [], "Edges": [], "Workflows": [], "Files": [], "Locations": [], "Contexts":[], "WorkTypes" : []};
            return this._super(store, type, payload, id, requestType);
        }

        nodes.forEach(function (node) {
            if (!node.edges)
                node.edges = [];
            if (!node.workflows)
                node.workflows = [];
        });

        if (workflows) {
            workflows.forEach(function (workflow) {
                if (edges) {
                    edges.forEach(function (edge) {
                        nodes.forEach(function (node) {
                            if (edge.from == node.id) {
                                node.edges.push(edge.id);                                
                            }
                            if (edge.from == node.id || edge.to == node.id) {
                                if (edge.GroupID == workflow.id && !Enumerable.From(node.workflows).Any("f=>f==\'" + edge.GroupID + "\'"))
                                    node.workflows.push(workflow.id);
                            }
                        });
                    });
                }
                else {
                    edges = [];
                }
            });
        }
        else {
            workflows = [];
        }

        nodes = nodes.map(function (a) {
            return {
                label: a.label,
                content: a.content,
                id: a.id,
                edges: a.edges,
                workflows: a.workflows
            };
        });

        payload = { "Nodes": nodes, "Edges": edges, "Workflows": workflows };

        //if (!edges || (edges.length === 0 && nodes.length === 1)) {
        //    delete payload.Edges;
        //}

        return this._super(store, type, payload, id, requestType);
    }
});


App.Node = DS.Model.extend({
    label: DS.attr('string'),
    content: DS.attr('string'),
    edges: DS.hasMany('edge', { async: true }),
    workflows: DS.hasMany('workflow', { async: true }),
    shape: function() {
        return 'ellipse'; // can also us circle
    }.property(),
    group: function() {
        return 'x'; // any string, will be grouped
    }.property()
});


App.Edge = DS.Model.extend({
    from: DS.attr(),
    to: DS.attr(),
    style: function(){
        return 'arrow'; // Type available ['arrow','dash-line','arrow-center']
    }.property(),
    width: function(){
        return 1;
    }.property(),
    color: function(){
        return 'gray';
    }.property(),
    GroupID: DS.attr(),
    Related: DS.attr('date'),
    RelationTypeID: DS.attr(),
    Weight: DS.attr(),
    Sequence: DS.attr(),
});

App.Workflow = DS.Model.extend({
    name: DS.attr('string'),
    label: function () {
        var temp = this.get('name');
        if (temp)
            return temp.replace('_', ' ');
        else
            return null;
    }.property(),
    comment: DS.attr('string'),
});

App.Wikipedia = DS.Model.extend({
    label: DS.attr('string'),
    content: DS.attr('string'),
    edges: DS.hasMany('edge')
});


App.WikipediaRoute = Ember.Route.extend({
    model: function (params) {
        //console.log(params.id);
        return Ember.RSVP.hash({
            graphData: this.store.findQuery('wikipedia', params.id),
            selected: params.id,
            content: '',
            title: ((typeof params.id === 'string' && params.id !== null && params.id.length > 0) ? params.id.replace(/_/g, ' ') : params.id)
        });
    },
    afterModel: function (m) {
        var sel = m.selected;
        var array = { nodes: [], edges: [] };
        var depthMax = 1; // currently depthMax is limited to 1 unless the data is already in ember store
        var nodeMax = 35;
        var data = recurseGraphData(sel, array, this, 1, depthMax, nodeMax, 'wikipedia');
        m.graphData = data;
        m.content = this.store.getById('wikipedia', sel).get('content');
    }
});


App.WikipediaController = Ember.ObjectController.extend({
    changeSelected: function () {
        //console.log('Selection changed, should redirect!')
        this.transitionToRoute('wikipedia', this.get('model.selected'));
    }.observes('model.selected')
});

App.WikipediaAdapter = DS.Adapter.extend({
    find: function (store, type, id) {
        return this.findMany(store, type, id);
    },
    findMany: function (store, type, ids) {
        return this.findQuery(store, type, ids);
    },
    findQuery: function (store, type, query, array) {
        var id = query;
        return new Ember.RSVP.Promise(function (resolve, reject) {
            var html;
            var recurse = function (key, val, parent) {
                if (key == '_') {
                    html = val;
                }
                else if (val instanceof Object) {
                    $.each(val, function (key, val) {
                        return recurse(key, val, parent);
                    });
                }
                return null;
            };

            var url = 'http://en.wikipedia.org/w/api.php?format=json&action=query&titles=' + encodeURIComponent(id) + '&prop=revisions&rvprop=content';
            jQuery.getJSON("http://query.yahooapis.com/v1/public/yql?" +
                "q=select%20content%20from%20data.headers%20where%20url%3D%22" +
                encodeURIComponent(url) +
                "%22&format=json&diagnostics=true&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys&callback=?"
              ).then(function (data) {
                  $.each(data, function (key, val) {
                      recurse(key, val, id);
                      if (html)
                          return false;
                  });

                  var edges = [];
                  var content = '';
                  if (html) {
                      content = InstaView.convert(html);
                      var leaves = html.match(/\[\[.*?\]\]/igm);
                      $.each(leaves, function (key, val) {
                          var leaf = '';
                          if (val.indexOf('|') > -1)
                              leaf = val.replace(/\[\[(.*)?\|.*/, "$1");
                          else if (val.indexOf('[' > -1))
                              leaf = val.replace(/\[\[(.*)?\]\]/, "$1");
                          else leaf = val;
                          if (leaf) {
                              edges.push({ id: id + '-' + leaf, from: id, to: leaf.replace(/ /g,'_') });
                          }
                      });
                  }
                  //edges = Enumerable.From(edges).GroupBy("$.id", "", "key,e=>{id: key, from: e.source[0].get('from'), to: e.source[0].get('to')}").ToArray()
                  edges = Enumerable.From(edges)
                      .Where("$.to.search(/^(file|image|category):.*/i)!==0")
                      .GroupBy("$.id", "", "key,e=>{id: key, from: e.source[0].from, to: e.source[0].to}")
                      .ToArray();
                  //oldNodes = Enumerable.From(App.Wikipedia.store.all('wikipedia').content).Select("$.id").ToArray();
                  //oldEdges = Enumerable.From(App.Wikipedia.store.all('edges').content).Select("$.id").ToArray();
                  //Enumerable.From(edges).Where(function (f) {
                  //    if (oldNodes.indexOf(f.to) > -1) {
                  //        //Update
                  //        //var node = App.Wikipedia.store.get('wikipedia', id);
                  //        //node.set("content", );
                  //        //post.save();
                  //    } else {
                  //        //Insert
                  //        if (id == f.to)
                  //            isNew = true;
                  //        App.Wikipedia.store.push('wikipedia', { id: f.to, label: f.to, content: content });
                  //    }
                  //});
                  //Enumerable.From(edges).Where(function (f) {
                  //    if (oldEdges.indexOf(f.id) > -1) {
                  //        //Update
                  //    } else {
                  //        //Insert
                  //        App.Wikipedia.store.push('edge', f);
                  //    }
                  //});
                  //if (!isNew) {
                  //    var node = App.Wikipedia.store.getById('wikipedia', id);
                  //    if (node) {
                  //        App.Wikipedia.store.deleteRecord(node);
                  //        App.Wikipedia.store.createRecord('wikipedia', { id: id, label: id, content: content });
                  //    }
                  //}



                  var edgeids = Enumerable.From(edges).Select("$.id").ToArray();
                  var sequence = 1;
                  Enumerable.From(edges).ForEach(function (f) { f.sequence = sequence; sequence++; App.Wikipedia.store.push('edge', f); });
                  Enumerable.From(edges).Where("$.to!='" + id.replace("'","\\\'") + "'").ForEach(function (f) { App.Wikipedia.store.push('wikipedia', { id: f.to, label: f.to.replace(/_/g,' ') }); });
                  App.Wikipedia.store.push('wikipedia', { id: id, label: id.replace(/_/g, ' '), edges: edgeids, content: content });
                  if (typeof array === 'undefined')
                      Ember.run(null, resolve, { id: id, label: id.replace(/_/g, ' '), content: content, edges: edgeids });
                  else {
                      var toReturn = { Nodes: [{ id: id, label: id.replace(/_/g, ' '), content: content, edges: edgeids }], Edges: edges };
                      Ember.run(null, resolve, toReturn );
                  }
              }, function (jqXHR) {
                  jqXHR.then = null; // tame jQuery's ill mannered promises
                  Ember.run(null, reject, jqXHR);
              });
        });
    },
    generateIdForRecord: function (store, record) {
        var uuid = NewGUID();
        return uuid;
    }
});





function filterData(data) {
    if (typeof data == 'undefined' || !data) return '';
    // filter all the nasties out
    // no body tags
    data = data.replace(/<?\/body[^>]*>/g, '');
    // no linebreaks
    data = data.replace(/[\r|\n]+/g, '');
    // no comments
    data = data.replace(/<--[\S\s]*?-->/g, '');
    // no noscript blocks
    data = data.replace(/<noscript[^>]*>[\S\s]*?<\/noscript>/g, '');
    // no script blocks
    data = data.replace(/<script[^>]*>[\S\s]*?<\/script>/g, '');
    // no self closing scripts
    data = data.replace(/<script.*\/>/g, '');
    //no refs
    data = data.replace(/<ref>/g, '');
    data = data.replace(/<\/ref>/g, '');
    //no source
    data = data.replace(/<source>/g, '');
    data = data.replace(/<\/source>/g, '');
    //no references
    data = data.replace(/<references\/>/g, '');
    //empty points
    data = data.replace(/<\/em><li><\/em>/ig, '<\/li>'); //HACK:?

    // [... add as needed ...]
    return '<div class=\'filteredData\'>' + data + '</div>';
}


Ember.Handlebars.helper('safehtml', function (item, options) {
    var escaped = '';
    if (this.results && this.results.length) {
        var obj = Enumerable.From(options.contexts[0].results).Where("$.get('id')=='" + options.data.keywords.result.id + "'").FirstOrDefault();
        if (obj) {
            escaped = filterData('' + obj.get(options.data.properties[0].split('.')[1]));
            return new Handlebars.SafeString(escaped);
        }
    }
    else {
        escaped = filterData('' + options.contexts[0].get(options.data.properties[0]));
        return new Handlebars.SafeString(escaped);
    }
    return '';
});

Ember.Handlebars.helper('wikiurl', function (item, options) {

});


Ember.TextField.reopen({
    attributeBindings: ['autofocus'],
    autofocus: 'autofocus'
});


