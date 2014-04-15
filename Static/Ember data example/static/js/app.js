App = Ember.Application.create();

App.Router.map(function() {
  this.route('about', {path: '/about/:id'});
});

App.IndexRoute = Ember.Route.extend({
  model: function() {
    return this.store.find('coffee');
  }
});


App.AboutRoute = Ember.Route.extend({
    model: function(params){
      return this.store.find('coffee', params.id);
    }
})

// App.Store = DS.Store.extend();


App.ApplicationAdapter = DS.RESTAdapter.extend({
  namespace: 'api/v1'
});


App.Coffee = DS.Model.extend({
    name: DS.attr(),
    short_description: DS.attr(),
    price: DS.attr()
})


