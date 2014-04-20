
App = Ember.Application.create();

App.Router.map(function() {
  this.route('graph');
});


App.IndexRoute = Ember.Route.extend({
    redirect: function() {
        this.transitionTo("graph");
    }
});

var c;
var graph, nodes, edges, data;
App.GraphRoute = Ember.Route.extend({
  model: function() {
    return Ember.RSVP.hash({
          nodes: this.store.find('Node'),
          edges: this.store.find('Edge')
      })
  },

  afterModel: function(model){

    // Setup vis Dataset for Visualisation
    nodes = new vis.DataSet();
    edges = new vis.DataSet();



    model.nodes.forEach(function(item){
      nodes.add(item.serialize({includeId:true}))
    })


    model.edges.forEach(function(item){
      edges.add(item.serialize({includeId:true}))
    })

    // Combine nodes & edges in one nice data object
    data = {
        nodes: nodes,
        edges: edges
      };
  }
});













App.GraphView = Ember.View.extend({
    didInsertElement: function() {

      var options = {
          dataManipulation: true,
          keyboard: true,
          onAdd: function(data,callback) {
            var span = document.getElementById('operation');
            var idInput = document.getElementById('node-id');
            var labelInput = document.getElementById('node-label');
            var saveButton = document.getElementById('saveButton');
            var cancelButton = document.getElementById('cancelButton');
            var div = document.getElementById('graph-popUp');
            span.innerHTML = "Add Node";
            idInput.value = data.id;
            labelInput.value = data.label;
            saveButton.onclick = saveData.bind(this,data,callback);
            cancelButton.onclick = clearPopUp.bind();
            div.style.display = 'block';
          },
          onDelete: function(data,callback){

            // Delete all nodes
            $.each(data.nodes, function(i,a){
              console.log("nodes: ", i,a)
              var node = App.Node.store.getById('node', a);
              node.deleteRecord();
              // node.save(); //not working, but should maybe need to connect to api first
            })


            // Delete all nodes
            $.each(data.edges, function(i,a){
              console.log("edges: ", i,a)
              var edge =  App.Edge.store.getById('edge', a);
              edge.deleteRecord();
              // edge.save();
            })



            callback(data);
          },
          onEdit: function(data,callback) {
            var span = document.getElementById('operation');
            var idInput = document.getElementById('node-id');
            var labelInput = document.getElementById('node-label');
            var saveButton = document.getElementById('saveButton');
            var cancelButton = document.getElementById('cancelButton');
            var div = document.getElementById('graph-popUp');
            span.innerHTML = "Edit Node";
            idInput.value = data.id;
            labelInput.value = data.label;
            saveButton.onclick = saveData.bind(this,data,callback);
            cancelButton.onclick = clearPopUp.bind();
            div.style.display = 'block';
          },
          onConnect: function(data,callback) {
            function saveLink() {
              data.id = parseInt(Math.random() * 12334243143);
              App.Node.store.createRecord('edge', data)
              callback(data);
            }

            if (data.from == data.to) {
              var r=confirm("Do you want to connect the node to itself?");
              if (r==true) {
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

      function saveData(data,callback) {
        var idInput = document.getElementById('node-id');
        var labelInput = document.getElementById('node-label');
        var div = document.getElementById('graph-popUp');
        data.id = idInput.value;
        data.label = labelInput.value;
        clearPopUp();

        if (App.Node.store.hasRecordForId('node', data.id)){
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
  label: DS.attr( 'string' ),
  content: DS.attr( 'string' ),
  children: DS.hasMany('edge')
});


App.Edge = DS.Model.extend({
    // from: DS.belongsTo('node'),
    // to: DS.belongsTo('node')
    from: DS.attr(),
    to: DS.attr()
});

App.Node.FIXTURES = [
    {id: '1', label: "Node_1", content: "Sample Content", children: [1,2]},
    {id: '2', label: "Node_2", content: "Sample Content 2", children: []},
    {id: '3', label: "Node_3", content: "Sample Content 3", children: []}
];




App.Edge.FIXTURES = [
    {id: '1', from: 1, to: 2},
    {id: '2', from: 1, to: 3}
];

