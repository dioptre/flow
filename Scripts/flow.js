
App = Ember.Application.create({
    rootElement: '#application'
});

App.Router.map(function () {
    this.route('graph');
});


App.IndexRoute = Ember.Route.extend({
    redirect: function () {
        this.transitionTo("graph");
    }
});

//App.Node.store.getById('node', '1e61b5cf-d2f0-4f49-aa36-00d8ec63acca').get('label')
var c;
var graph;
var data = { nodes: new vis.DataSet(), edges: new vis.DataSet() };
var sessionGroupID = NewGUID();
App.GraphRoute = Ember.Route.extend({
    model: function () {
        Ember.RSVP.hash(this.store.find('Node')).then(function (hash) {
          
        });

        return Ember.RSVP.hash({
            nodes: this.store.all('Node'),
            edges: this.store.all('Edge')
        })
    },

    afterModel: function (model) {

       
    }
});




App.GraphView = Ember.View.extend({
    didInsertElement: function () {

        var options = {
            dataManipulation: true,
            keyboard: true,
            onAdd: function (data, callback) {
                var span = document.getElementById('operation');
                var idInput = document.getElementById('node-id');
                var labelInput = document.getElementById('node-label');
                var saveButton = document.getElementById('saveButton');
                var cancelButton = document.getElementById('cancelButton');
                var div = document.getElementById('graph-popUp');
                span.innerHTML = "Add Node";
                idInput.value = data.id;
                labelInput.value = data.label;
                saveButton.onclick = saveData.bind(this, data, callback);
                cancelButton.onclick = clearPopUp.bind();
                div.style.display = 'block';
            },
            onDelete: function (data, callback) {

                // Delete all nodes
                $.each(data.nodes, function (i, a) {
                    //console.log("nodes: ", i, a)
                    var node = App.Node.store.getById('node', a);
                    node.deleteRecord();
                    // node.save(); //not working, but should maybe need to connect to api first
                })


                // Delete all nodes
                $.each(data.edges, function (i, a) {
                    //console.log("edges: ", i, a)
                    var edge = App.Edge.store.getById('edge', a);
                    edge.deleteRecord();
                    // edge.save();
                })



                callback(data);
            },
            onEdit: function (data, callback) {
                var span = document.getElementById('operation');
                var idInput = document.getElementById('node-id');
                var labelInput = document.getElementById('node-label');
                var saveButton = document.getElementById('saveButton');
                var cancelButton = document.getElementById('cancelButton');
                var div = document.getElementById('graph-popUp');
                span.innerHTML = "Edit Node";
                idInput.value = data.id;
                labelInput.value = data.label;
                saveButton.onclick = saveData.bind(this, data, callback);
                cancelButton.onclick = clearPopUp.bind();
                div.style.display = 'block';
            },
            onConnect: function (data, callback) {
                function saveLink() {
                    data.id = NewGUID();
                    data.groupid = sessionGroupID;
                    App.Node.store.createRecord('edge', data).save()
                    callback(data);
                }

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

        var container = this.$().find('#mygraph')[0];

        // Data was created in the route
        graph = new vis.Graph(container, data, options);

        graph.on('click', function (data) {
            //console.log(data, 'click event')
            if (data.nodes.length > 0) {
                App.Node.store.findQuery('node', { id: data.nodes[0] }).then(function (updated) {                    
                    var c = updated.get('content');
                    if (c && c[0]) {
                        var record = App.Node.store.getById('node', data.nodes[0]);
                        record.set('content', c[0].get('content'))
                        $('#flowItem').html(record.get('content'));
                    }
                });

                
            }
        })


    }
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

        return [];

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

        edges.forEach(function (edge) {
            //nodes[edge.from].children.push(edge.id)
            //App.Edge.store.push('edge', edge);
            nodes.forEach(function (node) {
                if (edge.from == node.id) {
                    node.edges.push(edge.id);
                    return false;
                }
            })
        });



        nodes = nodes.map(function (a) {
            return {
                label: a.label,
                content: a.content,
                id: a.id,
                edges: a.edges
            }
        })

        //Update Graph
        // Setup vis Dataset for Visualisation --> { nodes: new vis.DataSet(), edges: new vis.DataSet() };
        nodes.forEach(function (item) {
            if (!data.nodes.get(item.id)) //Only insert new data not twice if reloading from restadapter
                data.nodes.add(item)
        })
        edges.forEach(function (item) {
            if (!data.edges.get(item.id)) //Only insert new data not twice if reloading from restadapter
                data.edges.add(item)
        })


        payload = { "Nodes": nodes, "Edges": edges };
        if (edges.length === 0 && nodes.length === 1) {
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
    // to: DS.belongsTo('node')
    from: DS.attr(),
    to: DS.attr(),
    groupid: DS.attr()
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

$(document).ready(function () {

    var contented = document.createElement('div');
    contented.id = 'contented';
    contented.localName = 'contented';
    contented.style = 'width:100%;';
    document.body.appendChild(contented);
    
    function recurseTree(key, val, parent) {
        if (key == '_') {
            updateTree(val, parent);
            return false;
        }
        else if (val instanceof Object) {
            var missing = true;
            $.each(val, function (key, val) {
                missing = recurseTree(key, val, parent)
                return missing;
            });
            return missing;
        }
        return true;

    }

    function appendTree(val, parent) {
        leafMax = leafCache.length + leafConst;
        updateTree(val, parent);
    }

    var leafConst = 40;
    var leafMax = leafConst;
    var leafCache = [];
    function updateTree(val, parent) {
        var leaves = val.match(/\[\[.*?\]\]/igm);
        var delay = -1;
        $.each(leaves, function (key, val) {
            var id = '';
            if (val.indexOf('|') > -1)
                id = val.replace(/\[\[(.*)?\|.*/, "$1");
            else if (val.indexOf('[' > -1))
                id = val.replace(/\[\[(.*)?\]\]/, "$1");
            else id = val;
            if (leafCache.indexOf(id == -1)) {
                delay++;
                setTimeout(function () {
                    var url = 'http://en.wikipedia.org/w/api.php?format=json&action=query&titles=' + encodeURIComponent(id) + '&prop=revisions&rvprop=content';
                    if (leafCache.length < leafMax) {
                        leafCache.push(id);
                        $.getJSON("http://query.yahooapis.com/v1/public/yql?" +
                            "q=select%20content%20from%20data.headers%20where%20url%3D%22" +
                            encodeURIComponent(url) +
                            "%22&format=json&diagnostics=true&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys&callback=?"
                            ,
                            // "q=select%20content%20from%20data%2Eheaders%20where%20url%3D%22" +
                            // encodeURIComponent(url) +
                            // "%22&format=json'&callback=?",
                            function (data) {
                                var missing = true;
                                $.each(data, function (key, val) {
                                    missing = recurseTree(key, val, id);
                                    return missing;
                                });
                                if (missing) {
                                    //alert(id);
                                    //container.
                                    //  html('Error').
                                    //    focus().
                                    //      effect('highlight', { color: '#c00' }, 1000);
                                }
                                else {
                                    var container = $('#contented');
                                    //val = filterData(val);
                                    container.
                                    html(container.html() + leaves); //.
                                    //  focus().
                                    //    effect("highlight", {}, 1000);
                                }
                            }
                          );
                    }
                }, delay * 2000);

            }
        });

    }

    function filterData(data) {
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

    //updateTree('[[Main Page|Main Page]]');
});
