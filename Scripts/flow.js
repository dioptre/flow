
App = Ember.Application.create();

App.Router.map(function () {
    this.route('graph');
});


App.IndexRoute = Ember.Route.extend({
    redirect: function () {
        this.transitionTo("graph");
    }
});

var c;
var graph, nodes, edges, data;
App.GraphRoute = Ember.Route.extend({
    model: function () {
        return Ember.RSVP.hash({
            nodes: this.store.find('Node'),
            edges: this.store.find('Edge')
        })
    },

    afterModel: function (model) {

        // Setup vis Dataset for Visualisation
        nodes = new vis.DataSet();
        edges = new vis.DataSet();



        model.nodes.forEach(function (item) {
            nodes.add(item.serialize({ includeId: true }))
        })


        model.edges.forEach(function (item) {
            edges.add(item.serialize({ includeId: true }))
        })

        // Combine nodes & edges in one nice data object
        data = {
            nodes: nodes,
            edges: edges
        };
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
                    console.log("nodes: ", i, a)
                    var node = App.Node.store.getById('node', a);
                    node.deleteRecord();
                    // node.save(); //not working, but should maybe need to connect to api first
                })


                // Delete all nodes
                $.each(data.edges, function (i, a) {
                    console.log("edges: ", i, a)
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
                    data.id = parseInt(Math.random() * 12334243143);
                    App.Node.store.createRecord('edge', data)
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

    if (App.Node.store.hasRecordForId('node', data.id)) {
        // already exist - just update record
        var record = App.Node.store.getById('node', data.id);

        console.log(record)
        record.set('label', data.label)
        record.save();

    } else {
        App.Node.store.createRecord('node', data)
    }

    callback(data);

}

// function saveEmber()




App.ApplicationAdapter = DS.FixtureAdapter;


App.Node = DS.Model.extend({
    label: DS.attr('string'),
    content: DS.attr('string'),
    children: DS.hasMany('edge')
});


App.Edge = DS.Model.extend({
    // from: DS.belongsTo('node'),
    // to: DS.belongsTo('node')
    from: DS.attr(),
    to: DS.attr()
});

App.Node.FIXTURES = [
    { id: '1', label: "Node_1", content: "Sample Content", children: [1, 2] },
    { id: '2', label: "Node_2", content: "Sample Content 2", children: [] },
    { id: '3', label: "Node_3", content: "Sample Content 3", children: [] }
];




App.Edge.FIXTURES = [
    { id: '1', from: 1, to: 2 },
    { id: '2', from: 1, to: 3 }
];






//<script src="~/Modules/EXPEDIT.Flow/Scripts/jquery-fn/cross-domain-ajax/jquery.xdomainajax.js"></script>
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
