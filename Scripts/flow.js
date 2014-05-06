Ember.FEATURES["query-params"] = true;


App = Ember.Application.create({
    LOG_TRANSITIONS: true,
    rootElement: '#application'
});

App.Router.map(function () {
    this.route('graph', {path: 'graph'});
    this.route('graph', {path: 'graph/:id'});
    this.route('wikipedia', { path: "/wikipedia/:id" });
    this.route('search');
});


App.IndexRoute = Ember.Route.extend({
    beforeModel: function () {
        this.transitionTo("search");
    }
});




// Ember.run.debounce(this, this.calledRarely, 1000);



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
        if (this.get('graph')) i++
        if (this.get('file')) i++
        if (this.get('map')) i++
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
    dateModalBtn: [
      Ember.Object.create({ title: 'Cancel', dismiss: 'modal' }),
      Ember.Object.create({ title: 'Insert Date Filter', type: 'success', clicked: "addDate" })
    ],
    locationModalBtn: [
      Ember.Object.create({ title: 'Cancel', dismiss: 'modal' }),
      Ember.Object.create({ title: 'Insert Location Filter', type: 'success', clicked: "addLocation" })
    ],
    sched_date_from: "",
    sched_date_to: "",
    searchLocation: "",
    searchText: "",
    actions: {
        next: function (i) {
            this.incrementProperty(i)
            //console.log('next Page for ', i)
        },
        prev: function (i) {
            this.decrementProperty(i)
            //console.log('previous Page for ', i)
        },
        search: function () {
            this.loadAll();
            // On button click. Transition node.
            // this.transitionToRoute('search', 0, temp)
        },
        deleteTag: function (tag) {
            this.get('tags').removeObject(tag)
        },
        showDateModal: function () {
            return Bootstrap.ModalManager.show('dateModal');
        },
        addDate: function () {
            var controller = this;
            var date_data = this.get('sched_date_from') + ' - ' + this.get('sched_date_to');
            this.get('tags').addObject({
                n: date_data,
                d: date_data
            })
            console.log(this.get('sched_date_from'))
            return Bootstrap.ModalManager.hide('dateModal');
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
                controller.set('controllers.graphResults.results', res.get('content'))
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
                controller.set('controllers.mapResults.results', res.get('content'))
                controller.set('controllers.mapResults.loading', false);
            })
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
                controller.set('controllers.fileResults.results', res.get('content'))
                controller.set('controllers.fileResults.loading', false);
            });
        }
    }.observes('file', 'pageFile'),
    loadAll: function(){
        this.loadGraph();
        this.loadMap();
        this.loadFile();
    }.observes('pageSize').on('didInsertElement'),
    searchQuery: function () { // this builds the search query
        var controller = this;
        this.set('pageGraph', 0);
        this.set('pageFile', 0);
        this.set('pageMap', 0);
        Ember.run.debounce(this, controller.loadAll, 1000);
    }.observes('keywords')
});






App.ApplicationAdapter = DS.RESTAdapter.extend({
    namespace: 'flow',
    headers: {
        __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
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
            this.set('next', (nOfPages > page))
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
            this.set('next', (nOfPages > page))

        }


    }.observes('results'),
    results: []
});

App.MapResultsController = Ember.Controller.extend({
    _visi: function () {
        if (this.get('results').length === 0) {
            this.set('visi',false);
        }
        if (this.loading) {
            this.set('visi', false);
        }
        else {
            this.set('visi', true);
        }
    }.observes('loading', 'results'),
    visi: false,
    needs: 'search',
    next: false,
    prev: false,
    loading: true,
    page: Ember.computed.alias('controllers.search.pageMap'),
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
            this.set('next', (nOfPages > page))
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
})


var isMapResultsSetup = false;
App.MapResultComponent = Ember.Component.extend({
    map: null,
    visi: true,
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
            }
            if (component.map == null) {
                component.map = SetupMap(component._id);
                window.setTimeout(updateMapComponent, 100);
            }
            else
                window.setTimeout(updateMapComponent, 1);
        });
    }.observes('resultsGeo'),
    loading: function () {
        var tempmap = this.get('map');
        if (typeof google != 'undefined' && typeof tempmap != 'undefined' && tempmap != null) {
            if (this.get('visi'))
                $("#" + this.get('id')).show();
            else
                $("#" + this.get('id')).hide();
            window.setTimeout(function () { google.maps.event.trigger(tempmap, 'resize'); RefocusMap(tempmap); }, 100);            
        }        
    }.observes('visi').on('parentViewDidChange')
})

var drawing = false;
var isMapSetup = false, isMapLoaded=false, isMapInitialized = false;
var smap;
var cmap;
var deferredMap = Ember.RSVP.defer();
//deferredMap.promise.then(function (value) {
//    isMapInitialized = true;
//});
function MapInitialize() {
    if (!isMapInitialized) {
        LoadScript(mapHelper);
        //Ember.run.later(function () { deferredMap.resolve("Map Loaded"); },1800);
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
            DeleteExceptedShape(map, event.eventSource)
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





(function () {
    App.DateModal = Bootstrap.BsModalComponent.extend({
        didInsertElement: function () {
            this._super();
        },
        becameVisible: function () {
            this._super();
            if (!isMapSetup) {
                LoadMap();
                isMapSetup = true;
                if (drawing)
                    smap = SetupDrawingMap('map-search');
                else
                    smap = SetupMap('map-search');
                RedrawMap(smap);
                $("#searchLocation").autocomplete({
                    delay: 100,
                    source: function (request, response) {
                        $.ajax({
                            url: "/share/getlocations/" + request.term,
                            type: "GET",
                            dataType: "json",
                            //data: { id: request.term },
                            success: function (data) {
                                response($.map(data, function (item) {
                                    return {
                                        label: item.Text, value: item.Text, id: item.Value
                                    }
                                }))
                            }
                        })
                    },
                    focus: function (event, ui) {
                        AddGeographyUnique(smap, JSON.parse(ui.item.id).spatial, false, ui.item.label, true, NewGUID());
                        RefocusMap(smap);
                        smap.setZoom(9);
                    },
                    response: function (event, ui) {
                        if (ui.content.length > 0) {
                            AddGeographyUnique(smap, JSON.parse(ui.content[0].id).spatial, false, ui.content[0].label, true, NewGUID());
                            RefocusMap(smap);
                            smap.setZoom(9);
                        }
                        else {
                            GetAddressLocation($("#searchLocation").val(), function (latlng) {
                                DeleteShapes(smap);
                                AddMarkerSingle(smap, latlng, false, $("#searchLocation").val(), NewGUID());
                                RefocusMap(smap);
                                smap.setZoom(15);
                            });
                        }

                    }
                });
            }
        }
    });

    Ember.Handlebars.helper('flow-modal', App.DateModal);
}).call(this);




// App.Node.store.getById('node', '1e61b5cf-d2f0-4f49-aa36-00d8ec63acca').get('label')
var c;
var graph;
var data = { nodes: new vis.DataSet(), edges: new vis.DataSet() };
var sessionGroupID = NewGUID();
// App.GraphRoute = Ember.Route.extend({
//     model: function () {
//         Ember.RSVP.hash(this.store.find('node')).then(function (hash) {

//         });

//         return Ember.RSVP.hash({
//             nodes: this.store.all('node'),
//             edges: this.store.all('edge')
//         })
//     },

//     afterModel: function (model) {


//     }
// });






App.GraphRoute = Ember.Route.extend({
    //
    // data for current node


    model: function(params){
        // var id = params.id
        // Ember.RSVP.hash({
        //    data: this.store.find('node', {id:id})
        // }).then(function(data){
        //     var d = data.data
        //     // do the recursion business right here!!!

        //     // console.log(this.store.getById('node', id));

        //     d.store.getById('node', d.query.id).get('edges')


        //     var test = { Nodes: [], Edges: []}
        //     // debugger;

        // });

        // console.log(data)

        // return [];



        // debugger\

        params.id = params.id.toLowerCase();

        return  Ember.RSVP.hash({
            data: this.store.find('node', {id: params.id}),
            selected: params.id,
            content: '',
            label: ''
        })


        // return this.store.find('node', params.id);
    //     var id = params.id;
    //     if (id) {
    //         // return Ember.RSVP.hash({
                    // current:  this.store.find('node', {id:params.id}),   // {}
                    // current: data.node.byId
    //         //     selected: id
    //         // })
    //     }
    //     return '';
    },
    afterModel: function(m){
        var sel = m.selected;
        var array = {nodes: [], edges: []};
        var depthMax = 1; // currently depthMax is limited to 1 unless the data is already in ember store
        var nodeMax = -1;
        var data = getDataBitch(sel, array, this, 1, depthMax, nodeMax, 'node');
        console.log(data);
        // var model = this.get('model')
        m.data = data;
        m.content = this.store.getById('node', sel).get('content');
        m.label = this.store.getById('node', sel).get('label');
    }
})


function getDataBitch(id, array, _this, depth, depthMax, nodeMax, store) {

    //TODO: nodemax based on sequence (priority) in edges
    if (!nodeMax || nodeMax < 0 || array.nodes.length < nodeMax) {
        var node = _this.store.getById(store, id);
        //debugger;

        //console.log('this should happen twice')

        array.nodes.push({
            id: node.get('id'),
            label: node.get('label'),
            content: node.get('content')
        });


        var edges = Enumerable.From(node.get('edges').content).OrderBy("$.get('sequence')").ToArray();


        if (depth <= depthMax && edges.length !== undefined) {

            //console.log('this should happen once')

            edges.forEach(function (edge) {

                array.edges.push({
                    id: edge.get('id'),
                    from: edge.get('from'),
                    to: edge.get('to')
                });


                // Check if id has already been processed
                array = getDataBitch(edge.get('to'), array, _this, depth + 1, depthMax, nodeMax, store);

            })
        }
    }

    return array;
}


App.GraphController = Ember.ObjectController.extend({
//     // selectedNode: function(){

//     //     var d = this.get('model.data');
//     //     var s = this.get('model.selected');

//     //     var a = {}

//     //     d.forEach(function(item){
//     //         if (item.id.toUpperCase() === s) {
//     //             console.log('we have a match')
//     //             a = item;
//     //         }
//     //     })

//     //     return a;
//     // }.property('model.selected', 'model.data'),
    changeSelected: function(){
        console.log('Selection changed, should redirect!')
        this.transitionToRoute('graph', this.get('model.selected'));
    }.observes('model.selected')
})


// App.GraphController = Ember.ObjectController.extend({})


App.VizEditorComponent = Ember.View.extend({
    data: null,
    vizDataSet: {nodes: new vis.DataSet(), edges: new vis.DataSet()},
    selected: '',
    graph: null,
    setup: function(){
        //console.log('test')

        var _this = this;

        var container = $('<div>').appendTo(this.$())[0];
        var data = this.get('vizDataSet');
        var options = {stabilize:false, stabilizationIterations:1};

        // // sample data
        // data.nodes.add({
        //     id: 1, label: 'test'
        // })

        // // Data was created in the route
        this.graph = new vis.Graph(container, data, options);

        // This sets the new selected item on click
        this.graph.on('click', function (data) {
            if (data.nodes.length > 0) {
                _this.set('selected', data.nodes[0])
            }
        });

        $(window).resize(function(){
            _this.graph.redraw();
        })
    },
    dataUpdates: function() {



        if (this.graph === null) {
            this.setup(); // graph hasn't been initialised yet
        }

        var model_data = this.get('data'); // has to be synched with data
        var data = this.get('vizDataSet');


        // Step 1: add all the new nodes & edges to the dataset

        model_data.nodes.forEach(function(node){
            // debugger;
            if (data.nodes.get(node.id) === null) {
                //console.log('Adding nodes')
                data.nodes.add(node);
            }
        })

               // Step 2: remove nodes which aren't in the data set anymore
        data.nodes.getIds().forEach(function(id){
            var match = false;

            model_data.nodes.forEach(function(item){
                if(item.id === id){
                    match = true;
                }
            });

            if (!match) {
                data.nodes.remove(id)
            }
        })


        // Same but this time for edges

        model_data.edges.forEach(function(edge){
            if (data.edges.get(edge.id) === null) {
                //debugger;
                if (data.nodes.get(edge.from) !== null && data.nodes.get(edge.to) !== null) {  // ensure that only edges are drawn where the to & from nodes exist
                    data.edges.add(edge);
                }

            }
        })


        data.edges.getIds().forEach(function(id){
            var match = false;

            model_data.edges.forEach(function(item){
                if(item.id === id){
                    match = true;
                }
            });

            if (!match) {
                data.edges.remove(id)
            }
        })



    }.observes('data').on('didInsertElement')


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
})


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

        record.set('label', data.label)
        record.save();

    } else {
        App.Node.store.createRecord('node', data).save();
    }

    callback(data);

}

// function saveEmber()




//App.ApplicationAdapter = DS.FixtureAdapter;

DS.RESTAdapter.reopen({
    namespace: 'flow'
});


App.EdgeSerializer = DS.RESTSerializer.extend({
    // First, restructure the top-level so it's organized by type
    // and the comments are listed under a post's `comments` key.
    extractArray: function (store, type, payload, id, requestType) {

        return []; //makes sure it does nothing

        return this._super(store, type, payload, id, requestType);
    }
})


App.NodeSerializer = DS.RESTSerializer.extend({
    // First, restructure the top-level so it's organized by type
    // and the comments are listed under a post's `comments` key.
    extractArray: function (store, type, payload, id, requestType) {

        var nodes = payload.Nodes;
        var edges = payload.Edges;

        nodes.forEach(function (node) {
            if (!node.edges)
                node.edges = [];
        });

        if (edges) {
            edges.forEach(function (edge) {
                //nodes[edge.from].children.push(edge.id)
                //App.Edge.store.push('edge', edge);
                nodes.forEach(function (node) {
                    if (edge.from == node.id) {
                        // console.log('EDGES GETTING PUSHED')
                        //node.edges.push(edge.id);
                        return false;
                    }
                })
            });
        }



        nodes = nodes.map(function (a) {
            // console.log(a,'test')
            return {
                label: a.label,
                content: a.content,
                id: a.id,
                edges: a.edges
            }
        })

        //Update Graph
        // Setup vis Dataset for Visualisation --> { nodes: new vis.DataSet(), edges: new vis.DataSet() };
        // nodes.forEach(function (item) {
        //     if (!data.nodes.get(item.id)) //Only insert new data not twice if reloading from restadapter
        //         data.nodes.add(item)
        // })

        // if (edges) {
        //     edges.forEach(function (item) {
        //         if (!data.edges.get(item.id)) //Only insert new data not twice if reloading from restadapter
        //             data.edges.add(item)
        //     })
        // }




        payload = { "Nodes": nodes, "Edges": edges };
        if (!edges || (edges.length === 0 && nodes.length === 1)) {
            delete payload.Edges;
            //payload.node = nodes[0];
            //delete payload.Edges;
            //delete payload.Nodes;
            //$('#flowItem').html(nodes[0].content); //Really not cool
        } else if (nodes.length === 0 && edges.length === 1) {
            //payload.edge = edges[0];
        }

        //console.log(payload);

        //var supp = this._super(store, type, payload, id, requestType);
        //return supp;
      return this._super(store, type, payload, id, requestType);
    }
    //,
    //extract: function (store, type, payload, id, requestType) {
    //    if (payload.Nodes && payload.Nodes.length === 1) {
    //        return this._super(store, type, { "node": payload.Nodes[0] }, payload.Nodes[0].id, requestType);
    //    }
    //    return this._super(store, type, payload, id, requestType);
    //}

    //,

//    normalizeHash: {
//        edges: function (hash) {
//            return hash;
//        }
    // Next, normalize individual comments, which (after `extract`)
    // are now located under `comments`
    //comments: function(hash) {
    //    hash.id = hash._id;
    //    hash.title = hash.comment_title;
    //    delete hash._id;
    //    delete hash.comment_title;
    //    return hash;
    //}
//}
})


App.Node = DS.Model.extend({
    label: DS.attr('string'),
    content: DS.attr('string'),
    edges: DS.hasMany('edge')
});


App.Edge = DS.Model.extend({
    //from: DS.belongsTo('node'),
    //from: DS.belongsTo('App.Node'),
    //to: DS.belongsTo('node')
    from: DS.attr(),
    to: DS.attr(),
    groupid: DS.attr(),
    sequence: DS.attr()
});



App.DatePickerField = Em.View.extend({
    templateName: 'datepicker',
    didInsertElement: function () {
        var onChangeDate, self;
        self = this;
        onChangeDate = function (ev) {
            return self.set("value", moment.utc(ev.date).format("YYYY-MM-DD"));
        };
        return this.$('.datepicker').datepicker({
            separator: "-"
        }).on("changeDate", onChangeDate);
    }
});



//App.Node.FIXTURES = [
//    { id: '1', label: "Node_1", content: "Sample Content", children: [1, 2] },
//    { id: '2', label: "Node_2", content: "Sample Content 2", children: [] },
//    { id: '3', label: "Node_3", content: "Sample Content 3", children: [] }
//];




//App.Edge.FIXTURES = [
//    { id: '1', from: 1, to: 2 },
//    { id: '2', from: 1, to: 3 }
//];




//<script src="~/Modules/EXPEDIT.Share/Scripts/jquery-fn/cross-domain-ajax/jquery.xdomainajax.js"></script>
//$('#contented').load('http://google.com'); // SERIOUSLY!
//$.ajax({
//    url: 'http://en.wikipedia.org/w/api.php?format=json&action=query&titles=Main%20Page&prop=revisions&rvprop=content',
//    type: 'GET',
//    success: function (res) {
//        var headline = $(res.responseText).find('a.tsh').text();
//        alert(res.responseText);
//    }
//});


App.Wikipedia = DS.Model.extend({
    label: DS.attr('string'),
    content: DS.attr('string'),
    edges: DS.hasMany('edge')
});


App.WikipediaRoute = Ember.Route.extend({
    model: function (params) {
        console.log(params.id);
        return Ember.RSVP.hash({
            data: this.store.findQuery('wikipedia', params.id ),
            selected: params.id,
            content: ''
        })
    },
    afterModel: function (m) {
        var sel = m.selected;
        var array = { nodes: [], edges: [] };
        var depthMax = 1; // currently depthMax is limited to 1 unless the data is already in ember store
        var nodeMax = 25;
        var data = getDataBitch(sel, array, this, 1, depthMax, nodeMax, 'wikipedia');
        //console.log(data);
        // var model = this.get('model')
        m.data = data;
        m.content = this.store.getById('wikipedia', sel).get('content');
    }
});


App.WikipediaController = Ember.ObjectController.extend({
    changeSelected: function () {
        //console.log('Selection changed, should redirect!')
        this.transitionToRoute('wikipedia', this.get('model.selected'));
    }.observes('model.selected')
})

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
                        return recurse(key, val, parent)
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
                      content = filterData(InstaView.convert(html));
                      var leaves = html.match(/\[\[.*?\]\]/igm);
                      $.each(leaves, function (key, val) {
                          var leaf = '';
                          if (val.indexOf('|') > -1)
                              leaf = val.replace(/\[\[(.*)?\|.*/, "$1");
                          else if (val.indexOf('[' > -1))
                              leaf = val.replace(/\[\[(.*)?\]\]/, "$1");
                          else leaf = val;
                          if (leaf) {
                              edges.push({ id: id + '-' + leaf, from: id, to: leaf });
                          }
                      });
                  }
                  //edges = Enumerable.From(edges).GroupBy("$.id", "", "key,e=>{id: key, from: e.source[0].get('from'), to: e.source[0].get('to')}").ToArray()
                  edges = Enumerable.From(edges).GroupBy("$.id", "", "key,e=>{id: key, from: e.source[0].from, to: e.source[0].to}").ToArray()
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
                  Enumerable.From(edges).Where("$.to!='" + id + "'").ForEach(function (f) { App.Wikipedia.store.push('wikipedia', { id: f.to, label: f.to }); });
                  App.Wikipedia.store.push('wikipedia', { id: id, label: id, edges: edgeids, content: content });
                  if (typeof array === 'undefined')
                      Ember.run(null, resolve, { id: id, label: id, content: content, edges: edgeids });
                  else {
                      var toReturn = { Nodes: [{ id: id, label: id, content: content, edges: edgeids }], Edges: edges };
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
    data = data.replace(/<script.*\/>/, '');
    // [... add as needed ...]
    return data;
}


Ember.Handlebars.helper('safehtml', function (item, options) {
    if (this.results && this.results.length) {
        var obj = Enumerable.From(options.contexts[0].results).Where("$.get('id')=='" + options.data.keywords.result.id + "'").FirstOrDefault();
        if (obj) {
            var escaped = filterData('' + obj.get(options.data.properties[0].split('.')[1]));
            return new Handlebars.SafeString(escaped);
        }
    }
    else {
        var escaped = filterData('' + options.contexts[0].get(options.data.properties[0]));
        return new Handlebars.SafeString(escaped);
    }
    return '';
});

Ember.TextField.reopen({
    attributeBindings: ['autofocus'],
    autofocus: 'autofocus'
});


