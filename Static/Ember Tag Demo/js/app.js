
App = Ember.Application.create();

App.Router.map(function() {
  this.route('search');
  this.route('document');
  this.route('workflow');
});


App.IndexRoute = Ember.Route.extend({
    redirect: function() {
        this.transitionTo("search");
    }
});


App.SearchRoute = Ember.Route.extend({
  model: function() {
    return ['red', 'yellow', 'blue'];
  }
});




App.SearchBoxController = Ember.Controller.extend({
    tags: [{name: 'Berlin', type: 'location'}, {name:'London', type: "location"}, {name:'2014', type: "date"}],
    dateModalBtn: [
      Ember.Object.create({title: 'Cancel', dismiss: 'modal'}),
      Ember.Object.create({title: 'Insert Date Filter', type:'success', clicked: "addDate"})
    ],
    locationModalBtn: [
      Ember.Object.create({title: 'Cancel', dismiss: 'modal'}),
      Ember.Object.create({title: 'Insert Location Filter', type:'success', clicked: "addLocation"})
    ],
    sched_date_from: "",
    sched_date_to: "",
    searchLocation: "",
    searchText: "", // this the text in the search box
    searchQuery: function(){ // this builds the search query
        var searchText = this.get('searchText');

        var everything = _.clone(this.get('tags'))
        if (searchText !==  ''){
            everything.addObject({
                name: searchText,
                type: 'keywords'
            })
        }
        return everything.map(function(a){
            return a.type + ':' + a.name
        }).join(',')

    }.property('tags.@each', 'searchText'),
    actions: {
        deleteTag: function(tag){
            this.get('tags').removeObject(tag)
        },
        showDateModal: function(){
            return Bootstrap.ModalManager.show('dateModal');
        },
        addDate: function() {
            var controller = this;
            var date_data = this.get('sched_date_from') + ' - ' + this.get('sched_date_to');
            this.get('tags').addObject({
                name: date_data,
                type: 'date',
                data: date_data
            })
            console.log(this.get('sched_date_from'))
            return Bootstrap.ModalManager.hide('dateModal');
        },
        showLocationModal: function(){
            var controller = this;
            var location = controller.get('searchLocation');
            console.log(location)
            this.get('tags').addObject({
                name: location,
                type: 'location',
                data: location
            })
            return Bootstrap.ModalManager.show('locationModal')
        },
        addLocation: function(){
            return Bootstrap.ModalManager.hide('locationModal');
        },
        search: function() {
            console.log('Perform the actual search')
        }

    },
    fitInput: function() {
        console.log('fitInput run')

        // Select input elemet
        var $input = $('.input input');

        // Get size of parent
        var parentLength = $input.parent().innerWidth();

        // Length of all Tags
        var tagsLength = $('.input span.tag').reduce(function(pV, cV, i, a){
                  return pV + $(cV).outerWidth(true);
             }, 0)

        // Set input.length = parent - tags
        $input.width(parentLength - tagsLength - 32);
        return '';
    }.property('tags.@each')
})





App.DatePickerField = Em.View.extend({
  templateName: 'datepicker',
  didInsertElement: function() {
    var onChangeDate, self;
    self = this;
    onChangeDate = function(ev) {
      return self.set("value", moment.utc(ev.date).format("YYYY-MM-DD"));
    };
    return this.$('.datepicker').datepicker({
      separator: "-"
    }).on("changeDate", onChangeDate);
  }
});



  Ember.Handlebars.registerHelper('debug', function(the_string){
    Ember.Logger.log(the_string);
    // or simply
    console.log(the_string);
  });