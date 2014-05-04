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


var pfPageSize = 20;

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



App.SearchController = Ember.Controller.extend({
    needs: ['graphResults','mapResults','fileResults'],
    queryParams: ['keywords', 'tags', 'page', 'graph', 'file', 'map'],
    graph: true,
    file: true,
    map: false,
    activeResultsClass: function(){
        var i = 0;
        if (this.get('graph')) i++
        if (this.get('file')) i++
        if (this.get('map')) i++
            console.log(i)
        if (i===0) return '';
        return 'span' + (12/i);
    }.property('graph', 'file', 'map'),
    page: 0,
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
        next: function () {
            console.log('next')
        },
        previous: function () {
            console.log('previous')
        },
        search: function () {
            this.getData();
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
    getData: function(){
        var controller = this;
        if (this.get('graph')) {
            this.store.find('search', {
                page: this.get('page'),
                keywords: this.get('keywords'),
                tags: this.get('tags'),
                type: 'flow',
                pagesize: pfPageSize
            }).then(function (res) {
                controller.set('controllers.graphResults.results', res.get('content'))
            });
        }
        if (this.get('map')) {
            this.store.find('search', {
                page: this.get('page'),
                keywords: this.get('keywords'),
                tags: this.get('tags'),
                type: 'flowlocation',
                pagesize: pfPageSize
            }).then(function (res) {
                controller.set('controllers.mapResults.results', res.get('content'))
            });
        }
        if (this.get('file')) {
            this.store.find('search', {
                page: this.get('page'),
                keywords: this.get('keywords'),
                tags: this.get('tags'),
                type: 'file',
                pagesize: pfPageSize
            }).then(function (res) {
                controller.set('controllers.fileResults.results', res.get('content'))
            });
        }

    },
    searchQuery: function () { // this builds the search query
        var controller = this;
        Ember.run.debounce(this, controller.getData, 1000);

    }.observes('graph', 'map', 'file', 'keywords', 'page')
});






App.ApplicationAdapter = DS.RESTAdapter.extend({
    namespace: 'flow',
    headers: {
        __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
    }
});







App.GraphResultsController = Ember.Controller.extend({results: [] })
App.FileResultsController = Ember.Controller.extend({results: [] })
App.MapResultsController = Ember.Controller.extend({
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
    _id: null,
    id: function () {
        if (this._id === null)
            this._id = NewGUID();
        return this._id;
    }.property(),
    resultsGeo: [], //Expects geo (point data), name, id in array
    mapReady: false,
    intialize: function () {
        //this.set('mapReady', true);
        //debugger;
    }.on('didInsertElement'),
    update: function () {
        var component = this;
        Ember.RSVP.allSettled([deferredMap.promise]).then(function (array) {
            if (component.map == null)
                component.map = SetupMap(component._id);
            DeleteShapes(component.map);
            var geos = component.get('resultsGeo');
            $.each(geos, function (i, a) {
                var geoData = ParseGeographyData(a.geo);
                AddMarkerSingle(component.map, GetFirstLocation(geoData), false, a.name, a.id);
            });
            RefocusMap(component.map);
        });
    }.observes('resultsGeo')
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
        deferredMap.resolve("Map Loaded");
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
            selected: params.id
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

        var data = getDataBitch(sel, array, this, 1, depthMax, 'node');
        console.log(data);
        // var model = this.get('model')
        m.data = data;
    }
})


function getDataBitch(id , array, _this, depth, depthMax, store){
    var node = _this.store.getById(store, id);
    //debugger;

    console.log('this should happen twice')

    array.nodes.push({
        id: node.get('id'),
        label: node.get('label'),
        content: node.get('content')
    });

    var edges = node.get('edges');


    if (depth <= depthMax && edges.content.length !== undefined) {

        console.log('this should happen once')

        edges.forEach(function(edge){

            array.edges.push({
                id: edge.get('id'),
                from: edge.get('from'),
                to: edge.get('to')
            });

            // Check if id has already been processed
            array = getDataBitch(edge.get('to'), array, _this, depth + 1, depthMax, store);

        })
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
        console.log('test')

        var _this = this;

        var container = $('<div>').appendTo(this.$())[0];
        var data = this.get('vizDataSet');
        var options = {};

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
                console.log('Adding nodes')
                data.nodes.add(node);
            }
        })

        model_data.edges.forEach(function(edge){
            if (data.edges.get(edge.id) === null) {
                data.edges.add(edge);
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

        // Step 2: remove edges which aren't in the data set anymore
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
    groupid: DS.attr()
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
        this.store.findQuery('wikipedia', params.id);
        return Ember.RSVP.hash({
            data: this.store.find('wikipedia', params.id ),
            selected: params.id
        })
    },
    afterModel: function (m) {
        var sel = m.selected;
        var array = { nodes: [], edges: [] };
        var depthMax = 1; // currently depthMax is limited to 1 unless the data is already in ember store

        var data = getDataBitch(sel, array, this, 1, depthMax, 'wikipedia');
        console.log(data);
        // var model = this.get('model')
        m.data = data;
    }
});


App.WikipediaController = Ember.ObjectController.extend({
    changeSelected: function () {
        console.log('Selection changed, should redirect!')
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
                  var leaves = html.match(/\[\[.*?\]\]/igm);
                  var edges = [];
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

                  
                  var content = filterData(html.wiki2html());           
                  var edgeids = Enumerable.From(edges).Select("$.id").ToArray();
                  Enumerable.From(edges).ForEach(function (f) { App.Wikipedia.store.push('edge', f); });
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
    var escaped = filterData('' + options.contexts[0].get(options.data.properties[0]));
    return new Handlebars.SafeString(escaped);
});






(function () {

    var extendString = true;

    if (extendString) {
        String.prototype.wiki2html = wiki2html;
        String.prototype.iswiki = iswiki;
    } else {
        window.wiki2html = wiki2html;
        window.iswiki = iswiki;
    }

    // utility function to check whether it's worth running through the wiki2html
    function iswiki(s) {
        if (extendString) {
            s = this;
        }

        return !!(s.match(/^[\s{2} `#\*='{2}]/m));
    }

    // the regex beast...
    function wiki2html(s) {
        if (extendString) {
            s = this;
        }

        // lists need to be done using a function to allow for recusive calls
        function list(str) {
            return str.replace(/(?:(?:(?:^|\n)[\*#].*)+)/g, function (m) {  // (?=[\*#])
                var type = m.match(/(^|\n)#/) ? 'OL' : 'UL';
                // strip first layer of list
                m = m.replace(/(^|\n)[\*#][ ]{0,1}/g, "$1");
                m = list(m);
                return '<' + type + '><li>' + m.replace(/^\n/, '').split(/\n/).join('</li><li>') + '</li></' + type + '>';
            });
        }

        return list(s

            /* BLOCK ELEMENTS */
            .replace(/(?:^|\n+)([^# =\*<].+)(?:\n+|$)/gm, function (m, l) {
                if (l.match(/^\^+$/)) return l;
                return "\n<p>" + l + "</p>\n";
            })

            .replace(/(?:^|\n)[ ]{2}(.*)+/g, function (m, l) { // blockquotes
                if (l.match(/^\s+$/)) return m;
                return '<blockquote>' + l + '</pre>';
            })

            .replace(/((?:^|\n)[ ]+.*)+/g, function (m) { // code
                if (m.match(/^\s+$/)) return m;
                return '<pre>' + m.replace(/(^|\n)[ ]+/g, "$1") + '</pre>';
            })

            .replace(/(?:^|\n)([=]+)(.*)\1/g, function (m, l, t) { // headings
                return '<h' + l.length + '>' + t + '</h' + l.length + '>';
            })

            /* INLINE ELEMENTS */
            .replace(/'''(.*?)'''/g, function (m, l) { // bold
                return '<strong>' + l + '</strong>';
            })

            .replace(/''(.*?)''/g, function (m, l) { // italic
                return '<em>' + l + '</em>';
            })

            .replace(/[^\[](http[^\[\s]*)/g, function (m, l) { // normal link
                return '<a href="' + l + '">' + l + '</a>';
            })

            .replace(/[\[](http.*)[!\]]/g, function (m, l) { // external link
                var p = l.replace(/[\[\]]/g, '').split(/ /);
                var link = p.shift();
                return '<a href="' + link + '">' + (p.length ? p.join(' ') : link) + '</a>';
            })

            .replace(/\[\[(.*?)\]\]/g, function (m, l) { // internal link or image
                var p = l.split(/\|/);
                var link = p.shift();

                if (link.match(/^Image:(.*)/)) {
                    // no support for images - since it looks up the source from the wiki db :-(
                    return m;
                } else {
                    return '<a href="' + link + '">' + (p.length ? p.join('|') : link) + '</a>';
                }
            })
        );
    }

})();