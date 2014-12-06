if ((_ref = Ember.libraries) != null) {
  _ref.register('FlowPro', '2.0.0 Alpha');
}


$(function () {
    FastClick.attach(document.body);
});

Ember.FEATURES["query-params"] = true;
var defaultLocale = 'en-US';


function RedirectToLogin() {
    window.location.hash = '#/login'
    location.reload();
}

$.ajaxSetup({
    beforeSend: function (xhr, settings) {
        if (settings.url.match(/\/\//igm) === null)
            settings.url = expHost + settings.url;
    }
});


App = Ember.Application.create({
    // LOG_TRANSITIONS: true,
    rootElement: '#emberapphere'
});

// might fix affected route
App.ResetScroll = Ember.Mixin.create({
  activate: function() {
    this._super();
    window.scrollTo(0,0);
  }
});

LiquidFire.defineTransition('rotateBelow', function (oldView, insertNewView, opts) {
  var direction = 1;
  if (opts && opts.direction === 'cw') {
    direction = -1;
  }
  LiquidFire.stop(oldView);
  return insertNewView().then(function(newView) {
    oldView.$().css('transform-origin', '50% 150%');
    newView.$().css('transform-origin', '50% 150%');
    return LiquidFire.Promise.all([
      LiquidFire.animate(oldView, { rotateZ: -90*direction + 'deg' }, opts),
      LiquidFire.animate(newView, { rotateZ: ['0deg', 90*direction+'deg'] }, opts),
    ]);
  });
});

LiquidFire.map(function(){
    this.transition(
        this.fromRoute('todo'),
        this.toRoute('step'),
        this.use('toLeft'),
        this.reverse('toRight')
    );

    this.transition(
        this.fromRoute('search'),
        this.toRoute('graph'),
        this.use('toLeft'),
        this.reverse('toRight')
    );

    this.transition(
      this.withinRoute('permission'),
      this.use('toLeft')
    );
})

App.Router.map(function () {
    // App & Features
    this.route('graph', { path: 'process/:id' });
    this.route('workflow', { path: 'workflow/:id' });
    this.route('wikipedia', { path: "/wikipedia/:id" });

    this.route('debugz');


    this.route('search', { path: "search" }, function(){

    });
    this.route('myworkflows');
    this.route('mylicenses');
    this.route('file');
    this.route('permission');




    // FlowPro v2
    this.route('todo');
    this.route('styleguide', {path:"styleguide"}); // Internal only
    // this.route('editor', { path: 'editor/:id' }); // - editing executable workflows
    this.route('step', { path: 'step/:id' }); // - executing
    this.route('report')

    // Localisation
    this.route('translate', { path: 'translate/:workflowID' });
    this.route('translateme', { path: 'translateme' }); // Internal only



    // User stuff
    this.route('login');
    this.route('signup');
    this.route('resetpassword');
    this.route('myprofiles');
    this.route('help');

    // Currently unused.
    // this.route('userlist');
    // this.route('userprofile');
    // this.route('usernew');

    // 404 page
    this.route('errorpage', {path: '/*wildcard'})
});




// This is used to setup Title - http://www.jrhe.co.uk/setting-the-document-title-in-ember-js-apps/ - http://emberjs.jsbin.com/furabo/1/edit
Ember.Route = Ember.Route.extend({
  actions: {
    _setupTitle: function() {
        // Try Title Attr on Controller first
        var controllerTitle = this.controller.get('title'); // Title Attribute must be set on controller (pretty stupid name thinking about it - should be something unusual like _titleAttr)
        if (controllerTitle && typeof controllerTitle !== 'undefined') {
            App.setTitle(controllerTitle);
        } else {


            // // Try Title Attr on Route
            // var routerTitle = this.get('title');
            // if(routerTitle && typeof routerTitle !== 'undefined'){
            //     App.setTitle(title);
            //     return false;
            // } else {
                return true; // Bubble up!
            // }

        }
    },
    didTransition: function() {
      this.send('_setupTitle');
    }
  }
});
App.setTitle = function(title) { // little utilitiy function, pretty useless atm
    // to do any extra stuff with title, do it here
    title = title + " | FlowPro";
   document.title = title;
};

App.StyleguideRoute = Ember.Route.extend({
    model: function(){
        return {
            countries: [
              { label: 'Canada', value: 1 },
              { label: 'United Sates', value: 2 },
              { label: 'Mexico', value: 3 }
            ]
        }
    },
    actions: {
        toggleFirstModal: function() {
          this.toggleProperty('controller.showFirstModal');
        },
        firstModalCancel: function() {
          console.log('You pressed ESC to close the first modal');
        }
    }
});

App.ReportController = Ember.Controller.extend({ 
    // Used for horizontal bar chart, vertical bar chart, and pie chart
    content: [
    {
        "label": "Equity",
        "value": 12935781.176999997
    },
    {
        "label": "Real Assets",
        "value": 10475849.276172025
    },
    {
        "label": "Fixed Income",
        "value": 8231078.16438347
    },
    {
        "label": "Cash & Cash Equivalent",
        "value": 5403418.115000006
    },
    {
        "label": "Hedge Fund",
        "value": 1621341.246006786
    },
    {
        "label": "Private Equity",
        "value": 1574677.59
    }
    ],

    // Used only for scatter chart
    scatterContent: [
    {
        "group": "Energy",
        "xValue": 0.017440569068138557,
        "yValue": 0.029481600786463634
    },
    {
        "group": "Energy",
        "xValue": -0.28908275497440244,
        "yValue": -0.08083803288141521
    },
    {
        "group": "Industrial Metals",
        "xValue": 0.14072400896070691,
        "yValue": 0.04008348814566197
    },
    {
        "group": "Municipal Bonds",
        "xValue": -0.2712097037294005,
        "yValue": -0.11227088454416446
    },
    {
        "group": "Precious Metals",
        "xValue": -0.1728403500715051,
        "yValue": -0.04917117591842082
    },
    {
        "group": "Real Estate",
        "xValue": -0.06466537726032852,
        "yValue": -0.03309230484591455
    }
    ],

    // Used only for time series chart
    timeSeriesBarContent: [
    {
      time: d3.time.format('%Y-%m-%d').parse("2013-05-15"),
      label: "Financial analytics software",
      value: 49668,
      type: "money"
    }, {
      time: d3.time.format('%Y-%m-%d').parse("2013-06-15"),
      label: "Financial analytics software",
      value: 68344,
      type: "money"
    }, {
      time: d3.time.format('%Y-%m-%d').parse("2013-07-16"),
      label: "Financial analytics software",
      value: 60654,
      type: "money"
    }, {
      time: d3.time.format('%Y-%m-%d').parse("2013-08-16"),
      label: "Financial analytics software",
      value: 48240,
      type: "money"
    }, {
      time: d3.time.format('%Y-%m-%d').parse("2013-09-16"),
      label: "Financial analytics software",
      value: 62074,
      type: "money"
    }
    ],

    // Used only for time series chart
    timeSeriesLineContent: [
    {
      time: d3.time.format('%Y-%m-%d').parse("2013-05-15"),
      label: "Software & Programming",
      value: 17326,
      type: "money"
    }, {
      time: d3.time.format('%Y-%m-%d').parse("2013-05-15"),
      label: "Telecommunication",
      value: 4515,
      type: "money"
    }, {
      time: d3.time.format('%Y-%m-%d').parse("2013-06-15"),
      label: "Software & Programming",
      value: 15326,
      type: "money"
    }, {
      time: d3.time.format('%Y-%m-%d').parse("2013-06-15"),
      label: "Telecommunication",
      value: 1515,
      type: "money"
    }, {
      time: d3.time.format('%Y-%m-%d').parse("2013-07-16"),
      label: "Software & Programming",
      value: 14326,
      type: "money"
    }, {
      time: d3.time.format('%Y-%m-%d').parse("2013-07-16"),
      label: "Telecommunication",
      value: 8518,
      type: "money"
    }, {
      time: d3.time.format('%Y-%m-%d').parse("2013-08-16"),
      label: "Software & Programming",
      value: 42301,
      type: "money"
    }, {
      time: d3.time.format('%Y-%m-%d').parse("2013-08-16"),
      label: "Telecommunication",
      value: 90191,
      type: "money"
    }, {
      time: d3.time.format('%Y-%m-%d').parse("2013-09-16"),
      label: "Software & Programming",
      value: 57326,
      type: "money"
    }, {
      time: d3.time.format('%Y-%m-%d').parse("2013-09-16"),
      label: "Telecommunication",
      value: 39544,
      type: "money"
    }
    ]
});

App.StyleguideController = Ember.Controller.extend({
  queryParams: ['foo'],
  foo: null
})

App.ErrorpageRoute = Ember.Route.extend({
    model: function(){
        return {currentpage: location.href}
    }
})
App.ErrorpageController = Ember.Controller.extend({
    title: '404 Error'
})

App.HelpRoute = Ember.Route.extend({
    beforeModel: function (m) {
        // Just redirect to a cretain workflow (the help workflow)
        return this.replaceWith('graph', '76be503d-4689-47f8-99fe-2f512f81d4d5', { queryParams: { workflowID: 'c65d9b93-d986-4f70-bc0a-3eb2c6c0ecdd' } });
    }
});


App.TranslatemeRoute = Ember.Route.extend({
    model: function(){
        // return this.store.get('locale');
    }
})


App.TranslatemeController = Ember.ObjectController.extend({
    queryParams: ['item'],
    needs: ['application'],
    item: '',
    label: "fp_menu_link_workflow", // naming convention sample
    translation: 'translation',
    actions: {
        createItem: function(){

            var label = this.get('label');
            var translation = this.get('translation')

            var guid = NewGUID();
            var a = this.store.createRecord('locale', {
            id:guid,
            TranslationCulture: 'en-US',
                Label: this.get('label'),
                OriginalText: translation,
                OriginalCulture: 'en-US',
                Translation: translation,
            }) ;
            a.save().then(function(){
                 Messenger().post({ type: 'success', message: 'Saved' });
            }, function(){
                Messenger().post({ type: 'error', message: 'Error' });
            });

        }
    }
})



App.TranslateRoute = Ember.Route.extend({
    queryParams: {
        selected: { refreshModel: true }  // this ensure that new data is loaded if another element is selected
    },
    model: function(params){
        var localeSelected = App.get('localeSelected')
        return Ember.RSVP.hash({
            workflow: this.store.find('workflow', params.workflowID),
            processes:  this.store.find('node', { groupid: params.workflowID }), // Load edges and nodes
            translatedSteps: this.store.find('translation', {docid: params.workflowID, TranslationCulture:localeSelected, DocType: 'flows'}),
            translatedWorkflow: this.store.find('translation', {docid: params.workflowID, TranslationCulture:localeSelected, DocType: 'workflow'}),
            workflowID: params.workflowID,
            select: params.selected,
            tinyReset: "yes"
        });
    },
    afterModel: function(m){
        // Get all nodes
        var nodes = App.Node.store.all('node').content;
        var translation = App.Translation.store.all('translation').content;

        // get's this from query params on application route
        var localeSelected = App.get('localeSelected')


        // Get a list of all items with the code below:
        //var edges = App.Edge.store.all("edge").content;
        // var wfedges = Enumerable.From(edges).Where("f=>f.get('GroupID')==='" + m.workflowID + "'");
        // var nodeids = wfedges.Select("f=>f.get('from')").Union(wfedges.Select("f=>f.get('to')")).Where("f=>f!==null").ToArray();
        // var wfnodes = Enumerable.From(nodeids).Join(nodes, "", "f=>f.id", "f,g=>g").ToArray();


        m.translateTodos = Enumerable.From(m.translatedWorkflow.content)
            .Where("f=>f.get('DocID')==='" + m.workflowID + "'")
            .Select(function (f) {
                return { t: f, w: m.workflow, id: m.workflow.id, DocType: 'workflow' };
            });
        m.translateTodos = m.translateTodos.Union(Enumerable.From(m.translatedSteps.content).Join(nodes, "f=>f.get('DocID')", "g=>g.id", "f,g=>{t:f,n:g,id:g.id,DocType:'node'}")).ToArray();


        if (m.select) {


            var selectedObject = Enumerable.From(m.translateTodos).Where("f=>f.id==='" + m.select + "'").First();
            var doctype = selectedObject.DocType;


            // Get ccontent by pulling itme by ID
            App.Node.store.find('translation', {docid: m.select, TranslationCulture:localeSelected, DocType: doctype})
            App.Node.store.find(doctype, {id: m.select});

            // Depending on query params selected choose relevant item
            m.selectedOriginal = Enumerable.From(m.translateTodos).Where("f=>f.id==='" + m.select + "'").FirstOrDefault();
            //m.selectedTranslation = Enumerable.From(translation).Where("f=>f.get('DocID')==='" + m.select + "' && f.get('TranslationCulture')==='" + localeSelected + "'").FirstOrDefault();
        }
    },
    willTransition: function(transition) {
      if (this.controller.get('model.selectedTranslation.isDirty') &&
          !confirm("Are you sure you want to abandon progress?")) {
        transition.abort();
      } else {
        // Bubble the `willTransition` action so that
        // parent routes can decide whether or not to abort.
        return true;
      }
    }
});

App.TranslateController = Ember.ObjectController.extend({
    queryParams: ['selected'],
    needs: ['application'],
    selected: null,
    newContent: function(){
        return !this.get("model.selectedOriginal.t.isDirty");
    }.property('model.selectedOriginal.t.isDirty'),
    actions: {
        redoTranslate: function () {
            var _this = this;
            var translationID = this.get('model.selectedOriginal.t.id');
            this.store.find('translation', { id: translationID, Refresh: true }).then(function () {
                // location.reload();
                // debugger;
                // _this.
                Messenger().post({ type: 'success', message: 'Succesfully reloaded translation!' });
            }, function () {
                Messenger().post({ type: 'error', message: 'Unknown transition error.' });

            });

        },
        resetTranslation: function(){
            var translation = this.get('model.selectedOriginal.t');

            translation.rollback();
        },
        saveTranslation: function(){
            var translation = this.get('model.selectedOriginal.t');
            var language = this.get('controllers.application.localeSelectedDetails.humanName')
            translation.save().then(function(){
                Messenger().post({ type: 'success', message: language + ' translation was saved.' });

            }, function(){
                Messenger().post({ type: 'error', message: 'Unknown transition error.' });

            });
        }
    }
})

App.WorkflowRoute = Ember.Route.extend({
    actions: {
        error: function () {
            Messenger().post({ type: 'error', message: 'Could not find workflow. Ensure you have permission and are logged in.' });
            //Ember.run.later(null, RedirectToLogin, 3000);
        }
    },
    model: function (params) {
        if (params.id === 'undefined') {
            return null;
        }
        return this.store.find('workflow', params.id);
    },
    afterModel: function (m) {
        if (m === null) {
            //this.controllerFor('application').set('workflowID', NewGUID());
            return this.replaceWith('graph', NewGUID(), { queryParams: {workflowID: NewGUID()}});
        }
        var fn = m.get('firstNode');
        var id = m.get('id');
        if (typeof fn !== 'undefined' && fn) {
            //this.controllerFor('application').set('workflowID', id);
            return this.replaceWith('graph', fn, { queryParams: { workflowID: id } });
        }
        else {
            fn = Enumerable.From(App.Workflow.store.all('edge').content).Where("f=>f.get('GroupID')==='" + id + "'").Select("f=>f.get('from')").FirstOrDefault();
            if (typeof fn !== 'undefined' && fn && fn !== null) {
                //this.controllerFor('application').set('workflowID', id);
                return this.replaceWith('graph', fn, { queryParams: { workflowID: id } });
            }
            else {
                if (!id)
                    id = NewGUID();
                //this.controllerFor('application').set('workflowID', id);
                return this.replaceWith('graph', NewGUID(), { queryParams: { workflowID: id } });
            }
        }
    }
});

App.PermissionRoute = Ember.Route.extend({
    queryParams: {
        type: { refreshModel: true }  // this ensure that new data is loaded if the dropdown is changed
    },
    model: function (params) {
        return this.store.findQuery('mySecurityList', {type: params.type});
    }
})


App.PermissionController = Ember.ObjectController.extend({
    needs: ['application'],
    title: function() {
        var _this = this;
        // Go through all the types, and pick the matching type (then pick the text val)
        var text = Enumerable.From(_this.get('types')).Where('f=>f.id=="' + _this.get('type') +'"').Select("f=>f.text").ToString()
        return 'Permissions for ' + text;
    }.property('type'),
    queryParams: ['type'],
    types: [{ id: 'node', text: 'Steps' }, { id: 'workflow', text: 'Workflows' }, { id: 'file', text: 'Files' }],
    type: 'node',
    actions: {
        deletePermission: function (item) {
            var _this = this;
            item.deleteRecord();
            item.save().then(function () {
                Messenger().post({ type: 'success', message: "Successfully deleted user permission", id: 'user-security' })
                _this.set('model', _this.store.findQuery('mySecurityList', { type: _this.get('type') }));
            }, function () {
                Messenger().post({ type: 'error', message: "Could not delete user permission", id: 'user-security' })

            });

        }
    }

})


App.MyworkflowsRoute = Ember.Route.extend({
    model: function(){
        return Ember.RSVP.hash({
            workflows: this.store.find('myWorkflow'),
            processes: this.store.find('myNode')
        });
    }
})

App.MyworkflowsController = Ember.ObjectController.extend({
    needs: ['application'],
    title: 'My Workflows',
    permissionModal: false,
    activeItem: null,
    actions: {
        editPermission: function(item){

            // So in the submit we know what file we should be diting
            this.set('activeItem', item);

            this.set('permissionModal', true); // Show the modal before anything else

            // Make selectbox work after it's been inserted to the view - jquery hackss
            Ember.run.scheduleOnce('afterRender', this, function(){
                $('#add-comp-perm').select2({
                    placeholder: "Enter Companies...",
                    minimumInputLength: 2,
                    tags: true,
                    //createSearchChoice : function (term) { return {id: term, text: term}; },  // thus is good if you want to use the type in item as an option too
                    ajax: { // instead of writing the function to execute the request we use Select2's convenient helper
                        url: "/share/getcompanies",
                        dataType: 'json',
                        multiple: true,
                        data: function (term, page) {
                            return {id: term };
                        },
                        results: function (data, page) { // parse the results into the format expected by Select2.
                            if (data.length === 0) {
                                return { results: [] };
                            }
                            var results = Enumerable.From(data).Select("f=>{id:f.Value,tag:f.Text}").ToArray();
                            return { results: results, text: 'tag' };
                        }
                    },
                    formatResult: function(state) {return state.tag; },
                    formatSelection: function (state) {return state.tag; },
                    escapeMarkup: function (m) { return m; }
                });

                $('#add-users-perm').select2({
                    placeholder: "Enter Username...",
                    minimumInputLength: 2,
                    tags: true,
                    //createSearchChoice : function (term) { return {id: term, text: term}; },  // thus is good if you want to use the type in item as an option too
                    ajax: { // instead of writing the function to execute the request we use Select2's convenient helper
                        url: "/share/getusernames",
                        dataType: 'json',
                        multiple: true,
                        data: function (term, page) {
                            return {id: term };
                        },
                        results: function (data, page) { // parse the results into the format expected by Select2.
                            if (data.length === 0) {
                                return { results: [] };
                            }
                            var results = Enumerable.From(data).Select("f=>{id:f.Value,tag:f.Text}").ToArray();
                            return { results: results, text: 'tag' };
                        }
                    },
                    formatResult: function(state) {return state.tag; },
                    formatSelection: function (state) {return state.tag; },
                    escapeMarkup: function (m) { return m; }
                })
            });
        },
        submitPermission: function(){
            var _this = this;
            var newusers = $('#add-users-perm').val()
            var newcomp = $('#add-comp-perm').val()
            var fileid = this.get('activeItem').id;
            var TableType = this.get('activeItem.TableType');
            // console.log(this.get('activeItem'));
            if (newusers !== '') {
                Enumerable.From(newusers.split(',')).ForEach(function (f) {
                    var a = _this.store.createRecord('mySecurityList', {
                        ReferenceID: fileid,
                        SecurityTypeID: 2,
                        OwnerTableType: TableType,
                        AccessorUserID: f,
                        CanCreate: true,
                        CanRead: true,
                        CanUpdate: true,
                        CanDelete: true
                    })
                    a.save().then(function () {
                        Messenger().post({ type: 'success', message: "Successfully added user permissions", id: 'user-security' })
                    }, function () {
                        Messenger().post({ type: 'error', message: "Could not add user permissions", id: 'user-security' })

                    });
                });
            }

            if (newcomp !== '') {
                Enumerable.From(newcomp.split(',')).ForEach(function (f) {
                    var a = _this.store.createRecord('mySecurityList', {
                        ReferenceID: fileid,
                        SecurityTypeID: 2,
                        OwnerTableType: TableType,
                        AccessorCompanyID: f,
                        CanCreate: true,
                        CanRead: true,
                        CanUpdate: true,
                        CanDelete: true
                    })
                    a.save().then(function () {
                        Messenger().post({ type: 'success', message: "Successfully added company permissions", id: 'company-security' })
                    }, function () {
                        Messenger().post({ type: 'error', message: "Could not add company permissions", id: 'company-permission' })

                    });
                });
            }

            this.set('permissionModal', false);
        },
        cancelPermission: function(){
            this.set('permissionModal', false);
        }
    }
})


App.FileRoute = Ember.Route.extend({
    model: function(){
        return this.store.find('myFile');
    }
})

App.FileController = Ember.ObjectController.extend({
    needs: ['application'],
    title: 'My Files',
    permissionModal: false,
    activeItem: null,
    actions: {
        editPermission: function(item){

            // So in the submit we know what file we should be diting
            this.set('activeItem', item);

            this.set('permissionModal', true); // Show the modal before anything else

            // Make selectbox work after it's been inserted to the view - jquery hackss
            Ember.run.scheduleOnce('afterRender', this, function(){
                $('#add-comp-perm').select2({
                    placeholder: "Enter Companies...",
                    minimumInputLength: 2,
                    tags: true,
                    //createSearchChoice : function (term) { return {id: term, text: term}; },  // thus is good if you want to use the type in item as an option too
                    ajax: { // instead of writing the function to execute the request we use Select2's convenient helper
                        url: "/share/getcompanies",
                        dataType: 'json',
                        multiple: true,
                        data: function (term, page) {
                            return {id: term };
                        },
                        results: function (data, page) { // parse the results into the format expected by Select2.
                            if (data.length === 0) {
                                return { results: [] };
                            }
                            var results = Enumerable.From(data).Select("f=>{id:f.Value,tag:f.Text}").ToArray();
                            return { results: results, text: 'tag' };
                        }
                    },
                    formatResult: function(state) {return state.tag; },
                    formatSelection: function (state) {return state.tag; },
                    escapeMarkup: function (m) { return m; }
                });

                $('#add-users-perm').select2({
                    placeholder: "Enter Username...",
                    minimumInputLength: 2,
                    tags: true,
                    //createSearchChoice : function (term) { return {id: term, text: term}; },  // thus is good if you want to use the type in item as an option too
                    ajax: { // instead of writing the function to execute the request we use Select2's convenient helper
                        url: "/share/getusernames",
                        dataType: 'json',
                        multiple: true,
                        data: function (term, page) {
                            return {id: term };
                        },
                        results: function (data, page) { // parse the results into the format expected by Select2.
                            if (data.length === 0) {
                                return { results: [] };
                            }
                            var results = Enumerable.From(data).Select("f=>{id:f.Value,tag:f.Text}").ToArray();
                            return { results: results, text: 'tag' };
                        }
                    },
                    formatResult: function(state) {return state.tag; },
                    formatSelection: function (state) {return state.tag; },
                    escapeMarkup: function (m) { return m; }
                })
            });
        },
        submitPermission: function(){
            var _this = this;
            var newusers = $('#add-users-perm').val()
            var newcomp = $('#add-comp-perm').val()
            var fileid = this.get('activeItem').id

            if (newusers !== '') {
                Enumerable.From(newusers.split(',')).ForEach(function(f) {
                    var a = _this.store.createRecord('mySecurityList', {
                        ReferenceID: fileid,
                        SecurityTypeID: 2,
                        OwnerTableType: 'file',
                        AccessorUserID: f,
                        CanCreate: true,
                        CanRead: true,
                        CanUpdate: true,
                        CanDelete: true
                    })
                    a.save().then(function(){
                        Messenger().post({type: 'success', message: "Successfully added user permissions", id: 'user-security'})
                    }, function() {
                        Messenger().post({type: 'error', message: "Could not add user permissions", id: 'user-security'})

                    });
                });
            }

            if (newcomp !== '') {
                Enumerable.From(newcomp.split(',')).ForEach(function(f) {
                    var a = _this.store.createRecord('mySecurityList', {
                        ReferenceID: fileid,
                        SecurityTypeID: 2,
                        OwnerTableType: 'file',
                        AccessorCompanyID: f,
                        CanCreate: true,
                        CanRead: true,
                        CanUpdate: true,
                        CanDelete: true
                    })
                    a.save().then(function(){
                        Messenger().post({type: 'success', message: "Successfully added company permissions", id: 'company-security'})
                    }, function() {
                        Messenger().post({type: 'error', message: "Could not add company permissions", id: 'company-permission'})

                    });
                });
            }


            this.set('permissionModal', false);
        },
        cancelPermission: function(){
            this.set('permissionModal', false);
        }
    }
})


App.LoginRoute = Ember.Route.extend(App.ResetScroll, {});

App.LoginController = Ember.Controller.extend({
    queryParams: ['fromRoute'],
    fromRoute: '',
    needs: ['application'],
    email: "",
    rememberme: false,
    password: "",
    actions: {
        loginUser: function(){
            var UserName = this.get('email');
            var Password = this.get('password');
            var RememberMe = this.get('rememberme');
            var _this = this;

            this.set('password', '');

            _this.set('controllers.application.isLoading', true)
            $.post('/share/login', {
                UserName: UserName,
                Password: Password,
                RememberMe: RememberMe
            }).then(function(data){
                _this.set('controllers.application.isLoading', false)
                if (data === true) {

                    Ember.$.ajax({
                      url: "/flow/myuserinfo"
                    }).then(function(data){
                        Messenger().post({ type: 'success', message: 'Successfully logged in.', id: 'authenticate' });
                        _this.set('controllers.application.isLoggedIn', true)
                        data.UserName = ToTitleCase(data.UserName);
                        data.Thumb = "/share/photo/" + data.UserID;
                        mixpanel.identify(data.id);
                        mixpanel.people.set({
                            $first_name: data.UserName
                        });
                        _this.set('controllers.application.userProfile', data);
                    })
                    if (_this.get('fromRoute') != '') {
                         _this.transitionToRoute(_this.get('fromRoute'));
                    } else {
                        _this.transitionToRoute('search');
                    }

                } else {
                    Messenger().post({type:'error', message:'Incorrect username and/or password. Please try again.', id:'authenticate'});
                }

            }, function (jqXHR) {
                  jqXHR.then = null; // tame jQuery's ill mannered promises
            });
        }
    }
})

App.ResetpasswordController = Ember.Controller.extend({
    needs: ['application'],
    email: "",
    emailsent: false,
    actions: {
        resetPassword: function(){
            var email = this.get('email');
            var _this = this;

            if (!validateEmail(email)){
                Messenger().post({ type: 'error', message: 'Invalid email. Please try again.', id: 'authenticate' });
            } else {
                _this.set('controllers.application.isLoading', true)
                $.post('/share/requestpassword', {
                    id: email
                }).then(function(data){
                    _this.set('controllers.application.isLoading', false);
                    if (data === true) {
                        Messenger().post({ type: 'success', message: 'Successfully reset password. Please check your email.', id: 'authenticate' });
                        _this.set('emailsent', true);
                    } else {
                        Messenger().post({type:'error', message:'Unsuccesfull. Please make sure you have submitted the correct email.', id:'authenticate'});
                    }

                }, function (jqXHR) {
                      jqXHR.then = null; // tame jQuery's ill mannered promises
                });
            }
        }
    }
})


App.SignupController = Ember.Controller.extend({
    needs: ['application'],
    captchaKey: NewGUID(),
    captchaImg: function(){
        var _this = this;
        // Set the temporary loading image
        var loadingIMGbase64 = 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAADIAAAAUCAMAAAD4FJ6oAAAANlBMVEX///////93dXWDgYGNjIyYl5eioaGsq6u1tLS+vb3HxsbPz8/Y19fg4ODo6Ojw8PD39/f///9n9tiyAAAAAnRSTlPm8i0ECmIAAACbSURBVCjP1ZPNCsMwDINT2W4SO396/5fdcaOUjuw2XcWHBLZSOjaVEreVjl3i+Esk+hdkYV1Ma9Ieka5XU0Z9TvFMTgN8GnR2hUpXiiIvg+i4QYqTubBJAOgS9NPzwGhaCse79QdijdSgm/SBgcmzFm/Glk9n2E2xBUACsJEhxgJoWKuVtXZA8tZdVlnLYguZGTj/4sd+mNj+kF/3mjUy6Wc2lAAAAABJRU5ErkJggg==';
        var loadingIMGbase64 = 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAADIAAAAUBAMAAAA95HOpAAAALVBMVEX///////93dXWDgYGNjIyYl5eioaG1tLTHxsbPz8/Y19fg4ODo6Ojw8PD///8WzaMaAAAAAnRSTlPm8i0ECmIAAABCSURBVCjPY2AUxA4EGN7hAgxyOCQeDhGZ6UBYiU3mhVGGUZpyHxaZt65rXVeF3MMi8+bMmTOn75wbTD7FHXM4YxsAHLyPsxAxleAAAAAASUVORK5CYII=';
        _this.set('captchaURL', loadingIMGbase64);

        // Pull in a new image from the server
        _this.set('controllers.application.isLoading', true)
        Ember.$.getJSON('/share/captcha/' + this.get('captchaKey')).then(function(data){
            _this.set('controllers.application.isLoading', false)
            _this.set('captchaURL', 'data:image/png;base64,' + data.Image64);
        });
    }.observes('captchaKey').on('init'),
    captchaURL: '',
    captchaSolution: '',
    email: "",
    username: "",
    rememberme: false,
    password: "",
    usercreated: false,
    actions: {
        changeCaptcha: function(){
            this.set('captchaKey', NewGUID());
        },
        signupUser: function(){
            var _this = this;

            _this.set('controllers.application.isLoading', true)


            if (!validateEmail(_this.get('email'))) {
                Messenger().post({type:'error', message:'Invalid email.', id:'authenticate'});
                return;
            }

            if (_this.get('captchaSolution').length !== 4) {
                Messenger().post({type:'error', message:'Invalid human code.', id:'authenticate'});
                return;
            }

            // First check username email availabiltiy
            $.post('/share/DuplicateUser', {
                UserName: _this.get('username'),
                Email: _this.get('email')
            }).then(function(data){
                _this.set('controllers.application.isLoading', false);
                if (data) {
                 $.post('/share/signup', {
                    UserName: _this.get('username'),
                    Email: _this.get('email'),
                    Password: _this.get('password'),
                    CaptchaKey: _this.get('captchaSolution'),
                    CaptchaCookie: _this.get('captchaKey')
                 }).then(function(data){
                    if (data.Response === 1) {
                        Messenger().post({ type: 'success', message: 'Successful signup!', id: 'authenticate' });
                        _this.set('captchaKey', NewGUID());
                        _this.set('email', '');
                        _this.set('username', '');
                        _this.set('password', '');
                        _this.transitionToRoute('search');
                    }
                    else if (data.Response === 4) {
                         Messenger().post({type:'error', message:'Invalid human code. Please try again.', id:'authenticate'});
                        //_this.set('captchaKey', NewGUID());
                    } else {
                         Messenger().post({type:'error', message:'Unknown error. Plese try again later.', id:'authenticate'});

                    }
                 })
                } else {
                    Messenger().post({type:'error', message:'Email and/or username already taken.', id:'authenticate'});
                }
            });

        }
    }
})


var updateLocale = function (context, locale) {
    var c = context.controllerFor('application')
    context.store.find('locale', { TranslationCulture: locale, OriginalCulture: 'en-US' }).then(function (m) {
        Enumerable.From(m.content).ForEach(function (f) {
            m[f.get('Label')] = f.get('Translation');
        });
        App.set('locale.t', Ember.Object.create(m));
        c.set('model', App.get('locale'));
    });
}
App.ApplicationRoute = Ember.Route.extend({
    queryParams: {
        localeSelected: { refreshModel: true }
    },
    model: function (params) {

        if (params.localeSelected == 'null')
            params.localeSelected = App.get('locale.l');

        // Get the language from the browser settings;
        var userLang = params.localeSelected || navigator.language || navigator.userLanguage;

        var isNew = false;
        if (!App.get('locale')) {
            isNew = true;
            App.set('locale', Ember.Object.create({ l: null }));
        }
        if (isNew || App.get('locale.l') !== userLang)
        {
            App.set('locale.l', userLang);
            updateLocale(this, userLang);
        }

        App.set('localeSelected', userLang);

    },
    actions: {
        transitionSearch: function (a) {
            this.transitionTo('search', {queryParams: {keywords: a}})
            $('.searchTransitionMenu').val('').blur();
        },
        loading: function (m) {
            var controller = this.controller;

            if(typeof controller !== 'undefined') {
                //console.log('loading')
                controller.get('startLoading')()
                this.router.one('didTransition', function () {
                    controller.get('stopLoading')();
                });
            }
            return true;
        },
        error: function () {
            var controller = this.controller;


            // On error show the error page - sort of like a 404 error
            this.transitionTo('errorpage');

            if(typeof controller !== 'undefined') {
                controller.get('stopLoading')()
            }

            return true;
        }
    }
 });


App.ApplicationController = Ember.Controller.extend({
    queryParams: ['localeSelected'],
    currentPathDidChange: function () {
        window.scrollTo(0, 0); // THIS IS IMPORTANT - makes the window scroll to the top if changing route
        var currentPath = this.get('currentPath');
        App.set('currentPath', currentPath);  // Set path to the top
        $('body').trigger('pathChanged');
    }.observes('currentPath'), // This set the current path App.get('currentPath');
    isLoading: false, // this allows triggering the loading from anywhere in the code, just go: _this.set('controllers.application.isLoading', true)
    isLoadingObserver: function(){
       if(this.get('isLoading')){
        this.get('startLoading')();
       } else {
        this.get('stopLoading')();
       }
    }.observes('isLoading'),
    stopLoading: function(){
        setTimeout((function () {
                $('body').removeClass('cursor-progress loading-stuff');
                return Pace.stop();
        }), 0);
    },
    startLoading: function(){
        Pace.restart();
        $('body').addClass('cursor-progress loading-stuff');
    },
    isLoggedIn: false,
    logoutModal: false,
    userProfile: '',
    localeDic: {"af": "Afrikaans", "af-ZA": "Afrikaans (Suid-Afrika)", "am": "አማርኛ", "am-ET": "አማርኛ (ኢትዮጵያ)", "ar": "العربية", "ar-AE": "العربية (الإمارات العربية المتحدة)", "ar-BH": "العربية (البحرين)", "ar-DZ": "العربية (الجزائر)", "ar-EG": "العربية (مصر)", "ar-IQ": "العربية (العراق)", "ar-JO": "العربية (الأردن)", "ar-KW": "العربية (الكويت)", "ar-LB": "العربية (لبنان)", "ar-LY": "العربية (ليبيا)", "ar-MA": "العربية (المملكة المغربية)", "ar-OM": "العربية (عمان)", "ar-QA": "العربية (قطر)", "ar-SA": "العربية (المملكة العربية السعودية)", "ar-SY": "العربية (سوريا)", "ar-TN": "العربية (تونس)", "ar-YE": "العربية (اليمن)", "arn": "Mapudungun", "arn-CL": "Mapudungun ( )", "as": "অসমীয়া", "as-IN": "অসমীয়া (ভাৰত)", "az": "Azərbaycan­ılı", "az-Cyrl": "Азәрбајҹан дили", "az-Cyrl-AZ": "Азәрбајҹан (Азәрбајҹан)", "az-Latn": "Azərbaycan dili (Azərbaycan)", "az-Latn-AZ": "Azərbaycan dili (Azərbaycan)", "ba": "Башҡорт", "ba-RU": "Башҡорт (Рәсәй)", "be": "Беларуская", "be-BY": "Беларуская (Беларусь)", "bg": "български", "bg-BG": "български (България)", "bn": "বাংলা", "bn-BD": "বাংলা (বাংলাদেশ)", "bn-IN": "বাংলা (ভারত)", "bo": "བོད་ཡིག", "bo-CN": "བོད་ཡིག (ཀྲུང་ཧྭ་མི་དམངས་སྤྱི་མཐུན་རྒྱལ་ཁབ།)", "br": "brezhoneg", "br-FR": "brezhoneg (Frañs)", "bs": "bosanski", "bs-Cyrl": "босански", "bs-Cyrl-BA": "босански (Босна и Херцеговина)", "bs-Latn": "bosanski", "bs-Latn-BA": "bosanski (Bosna i Hercegovina)", "ca": "Català", "ca-ES": "Català (Català)", "ca-ES-valencia": "Valencià (Espanya)", "chr": "ᏣᎳᎩ", "chr-Cher": "ᏣᎳᎩ", "chr-Cher-US": "ᏣᎳᎩ (ᏣᎳᎩ)", "co": "Corsu", "co-FR": "Corsu (Francia)", "cs": "čeština", "cs-CZ": "čeština (Česká republika)", "cy": "Cymraeg", "cy-GB": "Cymraeg (Y Deyrnas Unedig)", "da": "dansk", "da-DK": "dansk (Danmark)", "de": "Deutsch", "de-AT": "Deutsch (Österreich)", "de-CH": "Deutsch (Schweiz)", "de-DE": "Deutsch (Deutschland)", "de-LI": "Deutsch (Liechtenstein)", "de-LU": "Deutsch (Luxemburg)", "dsb": "dolnoserbšćina", "dsb-DE": "dolnoserbšćina ( )", "dv": "ދިވެހިބަސް", "dv-MV": "ދިވެހިބަސް (ދިވެހި ރާއްޖެ)", "el": "Ελληνικά", "el-GR": "Ελληνικά (Ελλάδα)", "en-US": "English", "en-029": "English ( )", "en-AU": "English (Australia)", "en-BZ": "English (Belize)", "en-CA": "English (Canada)", "en-GB": "English (United Kingdom)", "en-HK": "English (Hong Kong)", "en-IE": "English (Ireland)", "en-IN": "English (India)", "en-JM": "English (Jamaica)", "en-MY": "English (Malaysia)", "en-NZ": "English (New Zealand)", "en-PH": "English (Philippines)", "en-SG": "English (Singapore)", "en-TT": "English (Trinidad and Tobago)", "en-US": "English", "en-ZA": "English (South Africa)", "en-ZW": "English (Zimbabwe)", "es": "español", "es-419": "español ( )", "es-AR": "español (Argentina)", "es-BO": "español (Bolivia)", "es-CL": "español (Chile)", "es-CO": "español (Colombia)", "es-CR": "español (Costa Rica)", "es-DO": "español (República Dominicana)", "es-EC": "español (Ecuador)", "es-ES": "español (España, alfabetización internacional)", "es-GT": "español (Guatemala)", "es-HN": "español (Honduras)", "es-MX": "español (México)", "es-NI": "español (Nicaragua)", "es-PA": "español (Panamá)", "es-PE": "español (Perú)", "es-PR": "español (Puerto Rico)", "es-PY": "español (Paraguay)", "es-SV": "español (El Salvador)", "es-US": "español (Estados Unidos)", "es-UY": "español (Uruguay)", "es-VE": "español (Republica Bolivariana de Venezuela)", "et": "eesti", "et-EE": "eesti (Eesti)", "eu": "euskara", "eu-ES": "euskara (euskara)", "fa": "فارسى", "fa-IR": "فارسى (ایران)", "ff": "Fulah", "ff-Latn": "Fulah", "ff-Latn-SN": "Fulah (Sénégal)", "fi": "suomi", "fi-FI": "suomi (Suomi)", "fil": "Filipino", "fil-PH": "Filipino ( )", "fo": "føroyskt", "fo-FO": "føroyskt (Føroyar)", "fr": "français", "fr-BE": "français (Belgique)", "fr-CA": "français (Canada)", "fr-CD": "français (Congo [RDC])", "fr-CH": "français (Suisse)", "fr-CI": "français (Côte d’Ivoire)", "fr-CM": "français (Cameroun)", "fr-FR": "français (France)", "fr-HT": "français (Haïti)", "fr-LU": "français (Luxembourg)", "fr-MA": "français (Maroc)", "fr-MC": "français (Principauté de Monaco)", "fr-ML": "français (Mali)", "fr-RE": "français (Réunion)", "fr-SN": "français (Sénégal)", "fy": "Frysk", "fy-NL": "Frysk (Nederlân)", "ga": "Gaeilge", "ga-IE": "Gaeilge (Éire)", "gd": "Gàidhlig", "gd-GB": "Gàidhlig (An Rìoghachd Aonaichte)", "gl": "galego", "gl-ES": "galego (galego)", "gn": "Guarani", "gn-PY": "Guarani (Paraguái)", "gsw": "Elsässisch", "gsw-FR": "Elsässisch ( )", "gu": "ગુજરાતી", "gu-IN": "ગુજરાતી (ભારત)", "ha": "Hausa", "ha-Latn": "Hausa", "ha-Latn-NG": "Hausa (Nijeriya)", "haw": "Hawaiʻi", "haw-US": "Hawaiʻi ( )", "he": "עברית", "he-IL": "עברית (ישראל)", "hi": "हिंदी", "hi-IN": "हिंदी (भारत)", "hr": "hrvatski", "hr-BA": "hrvatski (Bosna i Hercegovina)", "hr-HR": "hrvatski (Hrvatska)", "hsb": "hornjoserbšćina", "hsb-DE": "hornjoserbšćina ( )", "hu": "magyar", "hu-HU": "magyar (Magyarország)", "hy": "Հայերեն", "hy-AM": "Հայերեն (Հայաստան)", "id": "Bahasa Indonesia", "id-ID": "Bahasa Indonesia (Indonesia)", "ig": "Igbo", "ig-NG": "Igbo (Nigeria)", "ii": "ꆈꌠꁱꂷ", "ii-CN": "ꆈꌠꁱꂷ (ꍏꉸꏓꂱꇭꉼꇩ)", "is": "íslenska", "is-IS": "íslenska (Ísland)", "it": "italiano", "it-CH": "italiano (Svizzera)", "it-IT": "italiano (Italia)", "iu": "Inuktitut", "iu-Cans": "ᐃᓄᒃᑎᑐᑦ", "iu-Cans-CA": "ᐃᓄᒃᑎᑐᑦ (ᑲᓇᑕᒥ)", "iu-Latn": "Inuktitut", "iu-Latn-CA": "Inuktitut (Kanatami)", "ja": "日本語", "ja-JP": "日本語 (日本)", "jv": "Basa Jawa", "jv-Latn": "Basa Jawa", "jv-Latn-ID": "Basa Jawa (Indonesia)", "ka": "ქართული", "ka-GE": "ქართული (საქართველო)", "kk": "Қазақ", "kk-KZ": "Қазақ (Қазақстан)", "kl": "kalaallisut", "kl-GL": "kalaallisut (Kalaallit Nunaat)", "km": "ភាសាខ្មែរ", "km-KH": "ភាសាខ្មែរ (កម្ពុជា)", "kn": "ಕನ್ನಡ", "kn-IN": "ಕನ್ನಡ (ಭಾರತ)", "ko": "한국어", "ko-KR": "한국어(대한민국)", "kok": "कोंकणी", "kok-IN": "कोंकणी ( )", "ku": "کوردیی ناوەڕاست", "ku-Arab": "کوردیی ناوەڕاست", "ku-Arab-IQ": "کوردیی ناوەڕاست (کوردستان)", "ky": "Кыргыз", "ky-KG": "Кыргыз (Кыргызстан)", "lb": "Lëtzebuergesch", "lb-LU": "Lëtzebuergesch (Lëtzebuerg)", "lo": "ພາສາລາວ", "lo-LA": "ພາສາລາວ (ສປປ ລາວ)", "lt": "lietuvių", "lt-LT": "lietuvių (Lietuva)", "lv": "latviešu", "lv-LV": "latviešu (Latvija)", "mg": "Malagasy", "mg-MG": "Malagasy (Madagasikara)", "mi": "Reo Māori", "mi-NZ": "Reo Māori (Aotearoa)", "mk": "македонски јазик", "mk-MK": "македонски јазик (Македонија)", "ml": "മലയാളം", "ml-IN": "മലയാളം (ഭാരതം)", "mn": "Монгол хэл", "mn-Cyrl": "Монгол хэл", "mn-MN": "Монгол хэл (Монгол улс)", "mn-Mong": "ᠮᠤᠨᠭᠭᠤᠯ ᠬᠡᠯᠡ", "mn-Mong-CN": "ᠮᠤᠨᠭᠭᠤᠯ ᠬᠡᠯᠡ (ᠪᠦᠭᠦᠳᠡ ᠨᠠᠢᠷᠠᠮᠳᠠᠬᠤ ᠳᠤᠮᠳᠠᠳᠤ ᠠᠷᠠᠳ ᠣᠯᠣᠰ)", "mn-Mong-MN": "ᠮᠤᠨᠭᠭᠤᠯ ᠬᠡᠯᠡ (ᠮᠤᠨᠭᠭᠤᠯ ᠣᠯᠣᠰ)", "moh": "Kanien'kéha", "moh-CA": "Kanien'kéha ", "mr": "मराठी", "mr-IN": "मराठी (भारत)", "ms": "Bahasa Melayu", "ms-BN": "Bahasa Melayu (Brunei Darussalam)", "ms-MY": "Bahasa Melayu (Malaysia)", "mt": "Malti", "mt-MT": "Malti (Malta)", "my": "ဗမာ", "my-MM": "ဗမာ (မြန်မာ)", "nb": "norsk (bokmål)", "nb-NO": "norsk, bokmål (Norge)", "ne": "नेपाली", "ne-IN": "नेपाली (भारत)", "ne-NP": "नेपाली (नेपाल)", "nl": "Nederlands", "nl-BE": "Nederlands (België)", "nl-NL": "Nederlands (Nederland)", "nn": "norsk (nynorsk)", "nn-NO": "norsk, nynorsk (Noreg)", "no": "norsk", "nqo": "ߒߞߏ", "nqo-GN": "ߞߏ (ߖߌ߬ߣߍ߬ ߞߊ߲ߓߍ߲)", "nso": "Sesotho sa Leboa", "nso-ZA": "Sesotho sa Leboa (Afrika Borwa)", "oc": "Occitan", "oc-FR": "Occitan (França)", "om": "Oromoo", "om-ET": "Oromoo (Itoophiyaa)", "or": "ଓଡ଼ିଆ", "or-IN": "ଓଡ଼ିଆ (ଭାରତ)", "pa": "ਪੰਜਾਬੀ", "pa-Arab": "پنجابی", "pa-Arab-PK": "پنجابی (پاکستان)", "pa-IN": "ਪੰਜਾਬੀ (ਭਾਰਤ)", "pl": "polski", "pl-PL": "polski (Polska)", "prs": "درى", "prs-AF": "رى()", "ps": "پښتو", "ps-AF": "پښتو(افغانستان)", "pt": "português", "pt-AO": "português(Angola)", "pt-BR": "português(Brasil)", "pt-PT": "português(Portugal)", "qut": "Kiche", "qut-GT": "K(Guatemala)", "quz": "runasimi", "quz-BO": "runasimi()", "quz-EC": "runashimiEcuadorSuyu)", "quz-PE": "runasimi()", "rm": "Rumantsch", "rm-CH": "Rumantsch(Svizra)", "ro": "română", "ro-MD": "română(RepublicaMoldova)", "ro-RO": "română(România)", "ru": "русский", "ru-RU": "русский(Россия)", "rw": "Kinyarwanda", "rw-RW": "Kinyarwanda(Rwanda)", "sa": "संस्कृत", "sa-IN": "संस्कृत(भारतम्)", "sah": "Саха", "sah-RU": "Cаха", "sd": "سنڌي", "sd-Arab": "سنڌي", "sd-Arab-PK": "سنڌي(پاکستان)", "se": "davvisámegiella", "se-FI": "davvisámegiella(Suopma)", "se-NO": "davvisámegiella(Norga)", "se-SE": "davvisámegiella(Ruoŧŧa)", "si": "සිංහල", "si-LK": "සිංහල(ශ්‍රීලංකා)", "sk": "slovenčina", "sk-SK": "slovenčina(Slovenskárepublika)", "sl": "slovenščina", "sl-SI": "slovenščina(Slovenija)", "sma": "åarjelsaemiengïele", "sma-NO": "åarjelsaemiengïele()", "sma-SE": "åarjelsaemiengïele()", "smj": "julevusámegiella", "smj-NO": "julevusámegiella()", "smj-SE": "julevusámegiella()", "smn": "sämikielâ", "smn-FI": "sämikielâ()", "sms": "sää´mǩiõll", "sms-FI": "sää´mǩiõll()", "sn": "chiShona", "sn-Latn": "chiShona(Latin)", "sn-Latn-ZW": "chiShona(Latin Zimbabwe)", "so": "Soomaali", "so-SO": "Soomaali(Soomaaliya)", "sq": "Shqip", "sq-AL": "Shqip(Shqipëria)", "sr": "srpski", "sr-Cyrl": "српски", "sr-Cyrl-BA": "српски(БоснаиХерцеговина)", "sr-Cyrl-CS": "српски(СрбијаиЦрнаГора(Бивша))", "sr-Cyrl-ME": "српски(ЦрнаГора)", "sr-Cyrl-RS": "српски(Србија)", "sr-Latn": "srpski", "sr-Latn-BA": "srpski(BosnaiHercegovina)", "sr-Latn-CS": "srpski(SrbijaiCrnaGora(Bivša))", "sr-Latn-ME": "srpski(CrnaGora)", "sr-Latn-RS": "srpski(Srbija)", "st": "Sesotho", "st-ZA": "Sesotho(SouthAfrica)", "sv": "svenska", "sv-FI": "svenska(Finland)", "sv-SE": "svenska(Sverige)", "sw": "Kiswahili", "sw-KE": "Kiswahili(Kenya)", "syr": "ܣܘܪܝܝܐ", "syr-SY": "ܣܘܪܝܝܐ()", "ta": "தமிழ்", "ta-IN": "தமிழ்(இந்தியா)", "ta-LK": "தமிழ்(இலங்கை)", "te": "తెలుగు", "te-IN": "తెలుగు(భారతదేశం)", "tg": "Тоҷикӣ", "tg-Cyrl": "Тоҷикӣ", "tg-Cyrl-TJ": "Тоҷикӣ(Тоҷикистон)", "th": "ไทย", "th-TH": "ไทย(ไทย)", "ti": "ትግርኛ", "ti-ER": "ትግርኛ(ኤርትራ)", "ti-ET": "ትግርኛ(ኢትዮጵያ)", "tk": "Türkmendili", "tk-TM": "Türkmendili(Türkmenistan)", "tn": "Setswana", "tn-BW": "Setswana(Botswana)", "tn-ZA": "Setswana(AforikaBorwa)", "tr": "Türkçe", "tr-TR": "Türkçe(Türkiye)", "ts": "Xitsonga", "ts-ZA": "Xitsonga(SouthAfrica)", "tt": "Татар", "tt-RU": "Татар(Россия)", "tzm": "Tamazight", "tzm-Latn": "Tamazight", "tzm-Latn-DZ": "Tamazight(Djazaïr)", "tzm-Tfng": "ⵜⴰⵎⴰⵣⵉⵖⵜ", "tzm-Tfng-MA": "ⵜⴰⵎⴰⵣⵉⵖⵜ(ⵍⵎⵖⵔⵉⴱ)", "ug": "ئۇيغۇرچە", "ug-CN": "ئۇيغۇرچە(جۇڭخۇاخەلقجۇمھۇرىيىتى)", "uk": "українська", "uk-UA": "українська(Україна)", "ur": "اُردو", "ur-IN": "اردو(بھارت)", "ur-PK": "اُردو(پاکستان)", "uz": "O'zbekcha", "uz-Cyrl": "Ўзбекча", "uz-Cyrl-UZ": "Ўзбекча(ЎзбекистонРеспубликаси)", "uz-Latn": "O'zbekcha", "uz-Latn-UZ": "O'zbekcha(O'zbekistonRespublikasi)", "vi": "TiếngViệt", "vi-VN": "TiếngViệt(ViệtNam)", "wo": "Wolof", "wo-SN": "Wolof(Senegaal)", "xh": "isiXhosa", "xh-ZA": "isiXhosa(uMzantsiAfrika)", "yo": "Yoruba", "yo-NG": "Yoruba(Nigeria)", "zgh": "StandardMorrocanTamazight", "zgh-Tfng": "ⵜⴰⵎⴰⵣⵉⵖⵜ", "zgh-Tfng-MA": "ⵜⴰⵎⴰⵣⵉⵖⵜ(ⵍⵎⵖⵔⵉⴱ)", "zh": "中文", "zh-CN": "中文(中华人民共和国)", "zh-Hans": "中文(简体)", "zh-Hant": "中文(繁體)", "zh-HK": "中文(香港特別行政區)", "zh-MO": "中文(澳門特別行政區)", "zh-SG": "中文(新加坡)", "zh-TW": "中文(台灣)", "zu": "isiZulu", "zu-ZA": "isiZulu(iNingizimuAfrika)", "zh-CHS": "中文()旧版", "zh-CHT": "中文()舊版"},
    localeActivated: ["af","ar","az","be","bg","bn","bs","ca","ceb","cs","cy","da","de","el","en-US","eo","es","et","eu","fa","fi","fr","ga","gl","gu","ha","hi","hmn","hr","ht","hu","hy","id","ig","is","it","iw","ja","jw","ka","km","kn","ko","la","lo","lt","lv","mi","mk","mn","mr","ms","mt","ne","nl","no","pa","pl","pt","ro","ru","sk","sl","so","sq","sr","sv","sw","ta","te","th","tl","tr","uk","ur","vi","yi","yo","zh","zh-TW","zu"],
    localeAvailable:  function () {
        var localeDic = this.get('localeDic');
        var localeAtivated = this.get('localeActivated');

        var ln = [];

        localeAtivated.forEach(function (d, i) {
            if (localeDic[d] && d) {
                ln.push({ value: d, label: localeDic[d] });
            }
        });


       return ln;

   }.property('localeDic', 'localeActivated'),
    localeSelectedDetails: function(){
        return {humanName: this.get('localeDic')[this.get('localeSelected')]}
    }.property('localeSelected'),
    localeSelectedOberver: function () {
        var selectedLocal = this.get('localeSelected');
        if (typeof selectedLocal !== 'undefined' && selectedLocal !== null && selectedLocal !== 'null' && -1 === $.inArray(selectedLocal, this.get('localeActivated'))) {
            this.set('localeSelected', defaultLocale);
            this.transitionTo({ queryParams: {localeSelected: defaultLocale}})
        }
        App.set('localeSelected', selectedLocal); // this way you can pull the what the language is at any point (like in the model of routes)
    }.observes('localeSelected'),
    localeAppObserver: function () {
        var _this = this;
        App.get('locale').addObserver('l', this, function () {
            _this.set('localeSelected',App.get('locale.l'));
        });
        _this.set('localeSelected', App.get('locale.l') || defaultLocale);
    }.on('init'),
    localeSelected: null, //fixed not translating back to english
    actions: {
        logoutUser: function(){
            var _this = this;
            $.post('/share/logout').then(function(data){
                mixpanel.track('Manual logout');
                $.cookie('showLoggedOutModal', true);
                RedirectToLogin();
            }, function (jqXHR) {
                  jqXHR.then = null; // tame jQuery's ill mannered promises
            });
        },
        togglelogoutModal: function(){
            this.toggleProperty('logoutModal');
        }
    }
});


App.ApplicationView = Ember.View.extend({
    didInsertElement: function () {
        var _this = this;



        if ($.cookie('showLoggedOutModal') === true) {
            $.cookie('showLoggedOutModal', false);
            Messenger().post({ type: 'success', message: 'Successfully logged out.', id: 'authenticate' });
            //_this.transitionToRoute('login');
        }



        // Start - Code to handle if user is logged in or not
        var timeoutDelay = 15000; // 15 seconds

        var keepActive = function(){
            if(_this.active){
                $.ajax({
                  url: "/share/loggedin"
                }).then(function(result){

                    _this.active = false; // Set active to false until mouse is moved/keypress

                    if (result === true || result === false){
                        _this.set('controller.isLoggedIn', result)
                    }

                    // Pull in user information if it hasn't been set yet
                    if (result === true) {
                       //_this.get('controller.getUserProfile')();
                        $.ajax({url: "/flow/myuserinfo"}).then(function(data){
                            data.UserName = ToTitleCase(data.UserName);
                            data.Licensed = data.Licenses && (data.Licenses.length > 0)
                            data.Thumb = "/share/photo/" + data.UserID;
                            _this.set('controller.userProfile', data);
                        }, function (jqXHR) {
                          jqXHR.then = null; // tame jQuery's ill mannered promises
                        });
                    }

                    Ember.run.later(_this, keepActive, timeoutDelay);

                }, function (jqXHR) {
                  jqXHR.then = null; // tame jQuery's ill mannered promises
                });
            } else {
                Ember.run.later(_this, keepActive, timeoutDelay); // make sure this infinite loop never stops
            }
        }


        // Making sure to set active
        $(window).mousemove(function(e){
          _this.active = true;
        });
        $(window).keypress(function(e){
          _this.active = true;
        });

        var hidden, state, visibilityChange;
        if (typeof document.hidden !== "undefined") {
            hidden = "hidden";
            visibilityChange = "visibilitychange";
            state = "visibilityState";
        } else if (typeof document.mozHidden !== "undefined") {
            hidden = "mozHidden";
            visibilityChange = "mozvisibilitychange";
            state = "mozVisibilityState";
        } else if (typeof document.msHidden !== "undefined") {
            hidden = "msHidden";
            visibilityChange = "msvisibilitychange";
            state = "msVisibilityState";
        } else if (typeof document.webkitHidden !== "undefined") {
            hidden = "webkitHidden";
            visibilityChange = "webkitvisibilitychange";
            state = "webkitVisibilityState";
        }
        document.addEventListener(visibilityChange, function() {
            //console.log('visibilitychange')
            _this.active = true;
            keepActive()
        }, false);

        _this.active = true;
        keepActive();


        // END - User logged in (yes/no)






        // EVERYTHING FROM HERE IS COPIED FROM THE MAIN TEMPLATE JS FILE
        // Setup all jQuery plugins here!!!

        Ember.run.scheduleOnce('afterRender', this, function () {
            // navbar notification popups
            // $(".notification-dropdown").each(function (index, el) {
            //     var $el = $(el);
            //     var $dialog = $el.find(".pop-dialog");
            //     var $trigger = $el.find(".trigger");

            //     $dialog.click(function (e) {
            //         e.stopPropagation()
            //     });
            //     $dialog.find(".close-icon").click(function (e) {
            //         e.preventDefault();
            //         $dialog.removeClass("is-visible");
            //         $trigger.removeClass("active");
            //     });
            //     $("body").click(function () {
            //         $dialog.removeClass("is-visible");
            //         $trigger.removeClass("active");
            //     });

            //     $trigger.click(function (e) {
            //         e.preventDefault();
            //         e.stopPropagation();

            //         // hide all other pop-dialogs
            //         $(".notification-dropdown .pop-dialog").removeClass("is-visible");
            //         $(".notification-dropdown .trigger").removeClass("active")

            //         $dialog.toggleClass("is-visible");
            //         if ($dialog.hasClass("is-visible")) {
            //             $(this).addClass("active");
            //         } else {
            //             $(this).removeClass("active");
            //         }
            //     });
            // });


            // Setup localisation dropdown
            // $('.select2-localisation > select').select2({}) // NOT NEEDED ANYMORE USING EUI DROPDOWN EMBERUI.com


            // Remove the preloading screen
            $('.preloader').remove();


            // Setup the notifications dynamically
            $('.navbar-nav').on("click", "li.notification-dropdown > a.trigger", function(e){

                var $el = $(this).parent();
                var $dialog = $el.find(".pop-dialog");
                var $trigger = $el.find(".trigger");

                $dialog.click(function (e) {
                    e.stopPropagation()
                });
                $dialog.find(".close-icon").click(function (e) {
                    e.preventDefault();
                    $dialog.removeClass("is-visible");
                    $trigger.removeClass("active");
                });

                $("body").click(function () {
                    $dialog.removeClass("is-visible");
                    $trigger.removeClass("active");
                });

                e.preventDefault();
                e.stopPropagation();

                // hide all other pop-dialogs
                $(".notification-dropdown .pop-dialog").removeClass("is-visible");
                $(".notification-dropdown .trigger").removeClass("active")

                $dialog.toggleClass("is-visible");
                if ($dialog.hasClass("is-visible")) {
                    $(this).addClass("active");
                } else {
                    $(this).removeClass("active");
                }

            })




            // sidebar menu dropdown toggle
            $("#dashboard-menu .dropdown-toggle").click(function (e) {
                e.preventDefault();
                var $item = $(this).parent();
                $item.toggleClass("active");
                if ($item.hasClass("active")) {
                    $item.find(".submenu").slideDown("fast");
                } else {
                    $item.find(".submenu").slideUp("fast");
                }
            });


            // mobile side-menu slide toggler
            var $menu = $("#sidebar-nav");
            $("body").click(function () {
                if ($(this).hasClass("menu")) {
                    $(this).removeClass("menu");
                    $("html").removeClass("menuHelper");
                }
            });
            $menu.click(function(e) {
                e.stopPropagation();
            });

            // On click hide menu
            $menu.find('a').click(function(){
                if ($(this).attr('class') != "dropdown-toggle"){
                    $("body").removeClass("menu");
                    $("html").removeClass("menuHelper");
                }
            })

            $("#menu-toggler").click(function (e) {
                e.stopPropagation();
                $("body").toggleClass("menu");
                $("html").toggleClass("menuHelper");
            });

            $(window).resize(function() {
                if ($(this).width() > 769) {
                    $("body.menu").removeClass("menu");
                    $("html.menuHelper").removeClass("menuHelper");
                }
            })


            // build all tooltips from data-attributes
            $("[data-toggle='tooltip']").each(function (index, el) {
                $(el).tooltip({
                    placement: $(this).data("placement") || 'top'
                });
            });


            // custom uiDropdown element, example can be seen in user-list.html on the 'Filter users' button
            var uiDropdown = new function() {
                var self;
                self = this;
                this.hideDialog = function($el) {
                    return $el.find(".dialog").hide().removeClass("is-visible");
                };
                this.showDialog = function($el) {
                    return $el.find(".dialog").show().addClass("is-visible");
                };
                return this.initialize = function() {
                    $("html").click(function() {
                        $(".ui-dropdown .head").removeClass("active");
                        return self.hideDialog($(".ui-dropdown"));
                    });
                    $(".ui-dropdown .body").click(function(e) {
                        return e.stopPropagation();
                    });
                    return $(".ui-dropdown").each(function(index, el) {
                        return $(el).click(function(e) {
                            e.stopPropagation();
                            $(el).find(".head").toggleClass("active");
                            if ($(el).find(".head").hasClass("active")) {
                                return self.showDialog($(el));
                            } else {
                                return self.hideDialog($(el));
                            }
                        });
                    });
                };
            };

            // instantiate new uiDropdown from above to build the plugins
            new uiDropdown();


            // toggle all checkboxes from a table when header checkbox is clicked
            $(".table th input:checkbox").click(function () {
                $checks = $(this).closest(".table").find("tbody input:checkbox");
                if ($(this).is(":checked")) {
                    $checks.prop("checked", true);
                } else {
                    $checks.prop("checked", false);
                }
            });

            // quirk to fix dark skin sidebar menu because of B3 border-box
            if ($("#sidebar-nav").height() > $(".content").height()) {
                $("html").addClass("small");
            }



        });
    }
})


var oldURL = '';
App.Router.reopen({
    notifyAnalytics: function () {
        var url = this.get('url');

        // Duplicate search results shouldn't be tracked
        if (/^\/search/.test(url)) {
            url = '/search'
        }

        // Only send event if actually on new page
        if (oldURL != url) {
            console.log('Tracking URL:', url);
            mixpanel.track('Page Viewed', {
                pageName: document.title,
                domain: location.host,
                url: url
            });

             ga('send', 'pageview', {
                'page': this.get('url'),
                'title': this.get('url')
              });
        }

        oldURL = url; // create copy
    }.on('didTransition'),
});





App.ApplicationAdapter = DS.RESTAdapter.extend({
    host: expHost,
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
    "InternalUrl": DS.attr(''),
    "ExternalUrl": DS.attr(''),
    "ResourcePath": function(){
        return '/share/file/' + this.get('ReferenceID');
    }.property('ReferenceID'),
     "ResourcePreviewPath": function(){
        return '/share/preview/' + this.get('ReferenceID') + '?width=30&height=30&crop=true';
    }.property('ReferenceID'),
    "Author": DS.attr(''),
    "authorTags": function(){
        var author = this.get('Author')
        if (author) {
            return '<span class="label label-info">' + author + '</span>'
        }
        return ''
    }.property('Author'),
    "Updated": DS.attr(''),
    updatedNice: function(){
        var updated = this.get('Updated');
        if (updated){
            return moment(updated).format('DD/MM/YYYY');
        }
        return '';
    }.property('Updated'),
    updatedNiceMinutes: function(){
        var updated = this.get('Updated');
        if (updated){
         return moment(updated).format('DD/MM/YYYY @ HH:SS');
        }
        return '';
    }.property('Updated'),
    humanName: function () {
        var temp = this.get('Title');
        if (temp)
            return ToTitleCase(temp.replace(/_/g, ' '));
        else
            return null;
    }.property('Title')
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


App.SearchRoute = Ember.Route.extend({});
// App.SearchController = Ember.ObjectController.extend({
App.SearchController = Ember.Controller.extend({
    needs: ['application', 'graphResults','mapResults','fileResults','workflowResults'],
    queryParams: ['keywords', 'tags', 'wf', 'graph', 'file', 'map', 'pageWorkflow', 'pageGraph', 'pageFile', 'pageMap', 'pageSize'],
    wf: true,
    graph: true,
    file: false,
    map: false,
    title: "Search",
    activeResultsClass: function(){
        var i = 0;
        if (this.get('wf')) i++;
        if (this.get('graph')) i++;
        if (this.get('file')) i++;
        if (this.get('map')) i++;
        $(window).trigger('redrawMap');
        if (i===0) return '';
        return 'col-md-' + (12 / i);
    }.property('wf', 'graph', 'file', 'map'),
    pageWorkflow: 0,
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
    mapModal: false,
    settingsModal: false,
    actions: {
        toggleDateModal: function(){
            this.toggleProperty('dateModal');
        },
        toggleMapModal: function(){
            this.toggleProperty('mapModal');
        },
        toggleSettingsModal: function(){
            this.toggleProperty('settingsModal');
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
            mixpanel.track('Manual search', {
                keywords: this.get('keywords'),
                tags: this.get('tags')
            });
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
            var f = this.get('sched_date_from');
            var t = this.get('sched_date_to');

            if (f !== null && t !== null) {
                var a = [f, t].map(function (i) {
                    return moment.unix(i).format("DD/MM/YYYY")
                }).join(' - ');
                this.get('tags').addObject({
                    n: a,
                    d: f + '-' + t
                });

                // Reset date selectors
                this.set('sched_date_from', null);
                this.set('sched_date_to', null);

                this.toggleProperty('dateModal');
            } else {
               alert('Incomplete data selection. Please try again.')
            }

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
            controller.set('searchLocation', ''); // Clean up the form
           return  this.toggleProperty('mapModal');
        }
    },
    loadWorkflowQuery: '',
    loadWorkflow: function () {
        var controller = this;
        var query = {
            page: controller.get('pageWorkflow'),
            keywords: controller.get('keywords'),
            tags: controller.get('tags'),
            type: 'workflow',
            pagesize: controller.get('pageSize')
        }

        // Search function
        var loadFn = function () {
            if (controller.get('wf') && controller.get('componentURI').length > 0) {
                controller.set('controllers.workflowResults.loading', true);
                controller.store.find('search', query).then(function (res) {
                    controller.set('loadWorkflowQuery', JSON.stringify(query) + Math.round(new Date().getTime() / 30000));
                    controller.set('controllers.workflowResults.results', res.get('content'));
                    controller.set('controllers.workflowResults.loading', false);
                });
            }
        }

        //// Don't reload the search if the old query was identical to current one (stops the flickering!!!)
        if (controller.get('loadWorkflowQuery') !== JSON.stringify(query) + Math.round(new Date().getTime() / 30000)) {
            loadFn();
        }
    }.observes('wf', 'pageWorkflow'),
    loadGraphQuery: '',
    loadGraph: function(){
        var controller = this;
        var query = {
            page: controller.get('pageGraph'),
            keywords: controller.get('keywords'),
            tags: controller.get('tags'),
            type: 'process',
            pagesize: controller.get('pageSize')
        }

        // Search function
        var loadFn = function() {
            if (controller.get('graph') && controller.get('componentURI').length > 0) {
                controller.set('controllers.graphResults.loading', true);
                controller.store.find('search', query).then(function (res) {
                    controller.set('loadGraphQuery', JSON.stringify(query) + Math.round(new Date().getTime() / 30000));
                    controller.set('controllers.graphResults.results', res.get('content'));
                    controller.set('controllers.graphResults.loading', false);
                });
            }
        }

        // Don't reload the search if the old query was identical to current one (stops the flickering!!!)
        if (controller.get('loadGraphQuery') !== JSON.stringify(query) + Math.round(new Date().getTime() / 30000)) {
            loadFn();
        }
    }.observes('graph', 'pageGraph'),
    loadFileQuery: '',
    loadFile: function(){
        var controller = this;

        var query = {
                page: controller.get('pageFile'),
                keywords: controller.get('keywords'),
                tags: controller.get('tags'),
                type: 'file',
                pagesize: controller.get('pageSize')
            }

        var loadFn = function() {
            if (controller.get('file') && controller.get('componentURI').length > 0) {
                controller.set('controllers.fileResults.loading', true);
                controller.store.find('search', query).then(function (res) {
                    controller.set('loadFileQuery', JSON.stringify(query) + Math.round(new Date().getTime() / 30000));
                    controller.set('controllers.fileResults.results', res.get('content'));
                    controller.set('controllers.fileResults.loading', false);
                });
            }
        }

        if (controller.get('loadFileQuery') !== JSON.stringify(query) + Math.round(new Date().getTime() / 30000)) {
            loadFn();
        }

    }.observes('file', 'pageFile'),
    loadMap: function(){
        var controller = this;
        if (this.get('map') && this.get('componentURI').length > 0) {

            var slowLoading = Ember.run.later(controller, function(){
                controller.set('controllers.mapResults.loading', true);
            }, 250)
            this.store.find('search', {
                page: this.get('pageMap'),
                keywords: this.get('keywords'),
                tags: this.get('tags'),
                type: 'flowlocation',
                pagesize: this.get('pageSize')
            }).then(function (res) {
                controller.set('controllers.mapResults.results', res.get('content'));
                Ember.run.cancel(slowLoading);
                controller.set('controllers.mapResults.loading', false);
            });
        }
    }.observes('map', 'pageMap'),
    loadAll: function(){
        this.loadGraph();
        this.loadMap();
        this.loadFile();
        this.loadWorkflow();
    }.observes('pageSize', 'tags'), // the did insert element here doesn't  work that's why the view is setup below to kickoff the initial search
    searchQuery: function () { // this builds the search query
        var controller = this;
        this.set('pageWorkflow', 0);
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



App.WorkflowResultsController = Ember.ObjectController.extend({
    needs: ['application','search'],
    next: false,
    prev: false,
    loading: true,
    page: Ember.computed.alias('controllers.search.pageWorkflow'),
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
    results: []
});



App.GraphResultsController = Ember.ObjectController.extend({
    needs: ['application','search'],
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
    needs: ['application', 'search'],
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
    needs: ['application','search'],
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
                geos[geo.id] = { name: '<a href="/flow/#/process/' + a.get('ReferenceID') + '">' + a.get('humanName') + '</a>', id: geo.id, geo: geo.data };
            }
            else {
                geos[geo.id].name += '<br/><a href="/flow/#/process/' + a.get('ReferenceID') + '">' + a.get('humanName') + '</a>';
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
        LoadScript('https://maps.googleapis.com/maps/api/js?libraries=drawing&sensor=true&callback=MapInitialize');

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





(function () {
    App.MapModal = window.eui.EuiModalComponent.extend({
        magicid: null,
        id: function () {
            return 'mapmodalid';
            //Todo:fix
            if (this.get('magicid') === null)
                this.set('magicid', NewGUID());

            return this.get('magicid');
        }.property(),
       setup: function () {
           this._super();
           var _this = this;
           Ember.run.scheduleOnce('afterRender', this, function(){
                setTimeout(function(){
                    _this.becameVisible();
                }, 20);
           })
       },
       map: null,
       becameVisible: function () {
           if (!isMapSetup || true) {
               LoadMap();
               isMapSetup = true;
               if (drawing)
                   this.map = SetupDrawingMap(this.get('id'));
               else
                   this.map = SetupMap(this.get('id'));
               RedrawMap(this.map);
               var smap = this.map;
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
                                   };
                               }));
                           }
                       });
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
   Ember.Handlebars.helper('map-modal', App.MapModal);
}).call(this);

App.GraphRoute = Ember.Route.extend({
    lastLoadedWorkflowLocale: '',
    queryParams: {
        workflowID: {
            refreshModel: true
        },
        preview: {
            refreshModel: true
        }
    },
    actions: {
        error: function(){
            Messenger().post({ type: 'error', message: 'Could not find step. Ensure you have permission and are logged in.' });
            // Ember.run.later(null, RedirectToLogin, 3000); maybe not do a refresh
        }
    },
    beforeModel: function (params) {
        if (params.queryParams.workflowID === 'undefined') {
            this.replaceWith('graph', params.params.graph.id, { queryParams: { workflowID: NewGUID() } });
        }
    },
    model: function (params) {
        var _this = this;
        var id = params.id;
        id = id.toLowerCase(); // just in case
        //var workflow = new Promise(function (resolve, reject) {
        //    (function (resolve) {
        //        resolve();
        //    }, function () {
        //        resolve();
        //    })
        //});
        return Ember.RSVP.hash({
            data: this.store.find('node', { id: id, groupid: params.workflowID }),
            workflow: this.store.find('workflow', params.workflowID).catch(function (reason) {
                var groupID = Enumerable.From(_this.store.all('edge').content).Where("f=>f.get('from') ==='" + id + "' && f.get('to') === null").Select("f=>f.get('GroupID')").FirstOrDefault();
                if (typeof groupID !== 'undefined' && document.URL.indexOf(groupID) < 1) {
                    return _this.store.find('workflow', groupID).then(function (wfid) {
                        _this.replaceWith('graph', id, { queryParams: { workflowID: groupID } });
                    });
                }
                else if (typeof groupID === 'undefined' || !groupID)
                    return App.Workflow.store.createRecord('workflow', { id: params.workflowID, name: 'Untitled Workflow - ' + moment().format('YYYY-MM-DD @ HH:mm:ss'), StartGraphDataID: _this.get('model.selectedID') })
            }),
            duplicateNode: $.get('/flow/NodeDuplicateID/' + id),
            selectedID: id,
            workflowID: params.workflowID,
            updateGraph: Ember.Object.create(),
            content: '',
            label: '',
            editing: true,  // This gets passed to visjs to enable/disable editing dependig on context
            params: params,
            links: { prev: [], next: [], up: [], down: [] },
            preview: params.preview
        });
    },
    afterModel: function (m) {
        var _this = this;
        m.selected = this.store.getById('node', m.selectedID);
        if (m.selected) {
            //Get the selected item from m.data

            var matchedWorkflow = Enumerable.From(m.selected._data.workflows).Where("f=>f.id == '" + m.workflowID + "'").Any();
            if (!matchedWorkflow) { // The current node does not exist with the query param (workflowID)
                var firstWorkflow = Enumerable.From(m.selected._data.workflows).FirstOrDefault();
                if (firstWorkflow)
                    this.replaceWith('graph', m.selectedID, { queryParams: { workflowID: firstWorkflow.id } }); // choose the first workflowID which the node is in
                else {
                    //this.replaceWith('graph', m.selectedID, { queryParams: { workflowID: NewGUID() } });  //leave out otherwise recursive
                    // create the workflow here
                    m.workflow = App.Workflow.store.getById('workflow', m.workflowID);
                    if (!m.workflow)
                        m.workflow = App.Workflow.store.createRecord('workflow', { id: m.workflowID, name: 'Untitled Workflow - ' + moment().format('YYYY-MM-DD @ HH:mm:ss'), StartGraphDataID: m.selectedID });
                }
            }
            else {
                m.workflow = this.store.getById('workflow', m.workflowID);
                m.updateGraph.set('updated', NewGUID());
            }

            //Now get links
            var edges =  Enumerable.From(_this.store.all('edge').content);
            var prev = edges.Where("f=>f.get('GroupID')==='" + m.workflowID + "' && f.get('to')==='" + m.selectedID + "'").Distinct();
            var next = edges.Where("f=>f.get('GroupID')==='" + m.workflowID + "' && f.get('from')==='" + m.selectedID + "'").Distinct();
            var into = edges.Where("f=>f.get('GroupID')!=='" + m.workflowID + "' && f.get('to')==='" + m.selectedID + "'").Distinct();
            var out = edges.Where("f=>f.get('GroupID')!=='" + m.workflowID + "' && f.get('from')==='" + m.selectedID + "'").Distinct();

            var nodes = Enumerable.From(_this.store.all('node').content);
            m.links.prev = prev.Join(nodes, "$.get('from')", "$.id", "outer,inner=>{node:inner, wfid:outer.get('GroupID'), wfname:outer.get('groupName'), color: outer.get('color.color')}").ToArray();
            m.links.next = next.Join(nodes, "$.get('to')", "$.id", "outer,inner=>{node:inner, wfid:outer.get('GroupID'), wfname:outer.get('groupName'), color: outer.get('color.color')}").ToArray();
            m.links.into = into.Join(nodes, "$.get('from')", "$.id", "outer,inner=>{node:inner, wfid:outer.get('GroupID'), wfname:outer.get('groupName'), color: outer.get('color.color')}").ToArray();
            m.links.out = out.Join(nodes, "$.get('to')", "$.id", "outer,inner=>{node:inner, wfid:outer.get('GroupID'), wfname:outer.get('groupName'), color: outer.get('color.color')}").ToArray();


        } else { //NEW NODE

            //TODO by default transition to en-US
            var currentLocale = App.get('localeSelected');
            var thisLoadedWorkflowLocale = m.params.workflowID + currentLocale;
            if (IsGUID(m.params.workflowID) && currentLocale != defaultLocale && this.get('lastLoadedWorkflowLocale') !== thisLoadedWorkflowLocale) {
                this.set('lastLoadedWorkflowLocale', thisLoadedWorkflowLocale);
                if (!Enumerable.From(this.store.all('translation').content).Any("f=>f.get('DocID') ==='" + m.params.workflowID + "' && f.get('TranslationCulture') ==='" + currentLocale + "'"))
                    this.store.find('translation', { docid: m.params.workflowID, TranslationCulture: currentLocale, DocType: 'flows' })
                        .then(function () { }, function () {
                            m.params.localeSelected = defaultLocale;
                            _this.replaceWith('graph', m.params.id, { queryParams: { localeSelected: defaultLocale, workflowID: m.params.workflowID } });
                            return;
                        });
            }

            // Before creating a new node, check if it already exists, because if it does, then must be a permission problem
            if (m.duplicateNode) {
                Messenger().post({ type: 'error', message: 'Permission denied. Ensure you have permission and are logged in.' });
                _this.transitionTo('login');
            }

            // Reset (creating a new node)
            m.content = '';
            m.label = '';
            m.editing = true;
            m.humanName = '';
            m.workflow = this.store.getById('workflow', m.params.workflowID);
            if (!m.workflow) {
                m.workflow = App.Workflow.store.createRecord('workflow', {
                    id: m.params.workflowID,
                    name: 'Untitled Workflow - ' + moment().format('YYYY-MM-DD @ HH:mm:ss'),
                    StartGraphDataID: m.params.id
                });
            }
            m.workflows = Em.A([m.workflow]);

            var newNode = App.Node.store.createRecord('node', {
                id: m.selectedID,
                label: 'Untitled Step - ' + moment().format('YYYY-MM-DD @ HH:mm:ss'),
                content: '',
                VersionUpdated: Ember.Date.parse(moment().format('YYYY-MM-DD @ HH:mm:ss'))
            });

            newNode.get('workflows').then(function (w) {
                w.content.pushObject(m.workflow);
                m.selected = newNode;
                m.workflowID = m.params.workflowID;
                m.selectedID = m.selectedID;
            });

        }



    }

});

App.GraphController = Ember.ObjectController.extend({
    needs: ['application'],
    queryParams: ['workflowID', 'preview', 'localeSelected'],
    title: function(){
        var name = this.get('selected.humanName') + ' Step';
        App.setTitle(name);
        return name;
    }.property('selected.humanName'),
    nextSteps: function(){
        return ((this.get('model.links.prev') && this.get('model.links.prev').length > 0) || (this.get('model.links.next') && this.get('model.links.next').length > 0));
    }.property('model.links.prev','model.links.next'),
    nextWorkflowSteps: function(){
        return ((this.get('model.links.into') && this.get('model.links.into').length > 0) || (this.get('model.links.out') && this.get('model.links.out').length > 0));
    }.property('model.links.into','model.links.out'),
    newName: null,
    previewNext: function () {
        return (!this.get('nextSteps') && this.get('preview'));
    }.property('preview', 'nextSteps'),
    newContent: null,
    workflowEditNameModal: false,
    workflowShareModal: false,
    workflowNewModal: false, // up to here is for new ones
    moneyModal: false,
    loadingMoney: true,
    triggerModal: false,
    editing: true,
    updateGraph: '',
    workflowName: function () {
        var a = this.store.getById('workflow', this.get('workflowID'));
        if (a)
            a = a.get('name');
        else
            a = 'Untitled Workflow - ' + moment().format('YYYY-MM-DD @ HH:mm:ss');
        return a;
    }.property('workflowID', 'model.workflow.name'),
    isPreview: function () {
        var preview = this.get('preview');
        if (typeof preview === 'undefined' || preview === 'undefined' || preview === false || preview === 'false' || preview === null || preview === 'null')
            return false;
        else
            return true;
    }.property('preview'),
    columnLayout: function () {
        if (this.get('isPreview'))
            return 'process-header col-md-12';
        else
            return 'process-header col-md-6';
    }.property('preview'),
    workflowID: null, // available ids will be in model
    graphID: Ember.computed.alias('model.selectedID'),
    preview: null,
    localeSelected: null,
    currentURL: function () {
        return window.document.URL;
    }.property('workflowID', 'model.selected', 'preview', 'generatePreviewLink'),
    previewURL: function () {
        if (queryParamsLookup('preview') !== null && queryParamsLookup('preview') !== 'false' && queryParamsLookup('preview') !== false)
            return window.document.URL;
        else
            return window.document.URL + '&preview=true';
    }.property('workflowID', 'model.selected', 'preview', 'generatePreviewLink'),
    generatePreviewLink: false,
    workflowEditModal : false,
    validateWorkflowName: false,
    validateNewName: false,
    validateNewNewName: false,
    loadingNewNewName: false,
    validateExistingName: false,
    loadingWorkflowName: false,
    loadingNewName: false,
    loadingExistingName: false,
    workflows: function(){
        return this.get('model.selected')._data.workflows;
    }.property('model.selected'),
    workflowsOptionsArray: function () {
        if (typeof this.get('workflows') === 'undefined')
            return [];
        var x =  this.get('workflows').map(function (a) {
            return { name: a.get('name'), id: a.id }
        });
        return x;
    }.property('workflows'),
    workflowGte2: Ember.computed.gte('workflows.length', 2),
    fitVis: function(){
        Ember.run.scheduleOnce('afterRender', this, function(){
            $('body').fitVids();
        })
    }.observes('model.content'),
    changeSelected: function () {
        //var found = false;
        //var edges = Enumerable.From(this.get('graphData').edges);
        //if (edges.Any("f=>f.from=='" + this.get('model.selectedID') + "'") || edges.Any("f=>f.to=='" + this.get('model.selectedID') + "'"))
        this.transitionToRoute('graph', this.get('model.selectedID'), { queryParams: { localeSelected: App.get('localeSelected') } });
    }.observes('model.selectedID'),
    checkWorkflowName: function () {
        var _this = this;
        if (!_this.get('model.workflow.name') || typeof _this.get('model.workflow.name') !== 'string' || _this.get('model.workflow.name').trim().length < 1) {
            _this.set('validateWorkflowName', 'Name required.');
            return;
        }
        _this.set('loadingWorkflowName', true);
        return new Ember.RSVP.Promise(function (resolve, reject) {
            jQuery.getJSON('/Flow/WorkflowDuplicate?id=' + encodeURIComponent(_this.get('model.workflow.name').trim()) + '&guid=' + _this.get('workflowID')
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

    }.observes('model.workflow.name'),
    checkNewNodeName: function () {
        var _this = this;
        if (!_this.get('model.selected.label') || typeof _this.get('model.selected.label') !== 'string' || _this.get('model.selected.label').trim().length < 1) {
            _this.set('validateNewName', 'Name required.');
            return;
        }
        _this.set('loadingNewName', true);
        return new Ember.RSVP.Promise(function (resolve, reject) {
            jQuery.getJSON('/Flow/NodeDuplicate?id=' + encodeURIComponent(_this.get('model.selected.label').trim()) + '&guid=' + _this.get('selectedID')
              ).then(function (data) {
                  _this.set('loadingNewName', false);
                  Ember.run(null, resolve, data);
              }, function (jqXHR) {
                  jqXHR.then = null; // tame jQuery's ill mannered promises
                  Ember.run(null, reject, jqXHR);
              });
        }).then(function (value) {
            _this.set('validateNewName', value ? 'Name already in use.' : false);
        });

    }.observes('model.selected.label'),
    checkNewNewNodeName: function () {
        var _this = this;
        if (!_this.get('newName') || typeof _this.get('newName') !== 'string' || _this.get('newName').trim().length < 1) {
            _this.set('validateNewNewName', 'Name required.');
            return;
        }
        _this.set('loadingNewNewName', true);
        return new Ember.RSVP.Promise(function (resolve, reject) {
            jQuery.getJSON('/Flow/NodeDuplicate?id=' + encodeURIComponent(_this.get('newName').trim()) + '&guid=' + _this.get('selectedID')
              ).then(function (data) {
                  _this.set('loadingNewNewName', false);
                  Ember.run(null, resolve, data);
              }, function (jqXHR) {
                  jqXHR.then = null; // tame jQuery's ill mannered promises
                  Ember.run(null, reject, jqXHR);
              });
        }).then(function (value) {
            _this.set('validateNewNewName', value ? 'Name already in use.' : false);
        });

    }.observes('newName'),
    checkExistingNodeName: function () {
        var _this = this;
        if (!_this.get('model.label') || typeof _this.get('model.label') !== 'string' || _this.get('model.label').trim().length < 1) {
            _this.set('validateExistingName', 'Name required.');
            return;
        }
        _this.set('loadingExistingName', true);
        return new Ember.RSVP.Promise(function (resolve, reject) {
            jQuery.getJSON('/Flow/NodeDuplicate?id=' + encodeURIComponent(_this.get('model.label').trim()) + '&guid=' + _this.get('selectedID')
              ).then(function (data) {
                  _this.set('loadingExistingName', false);
                  Ember.run(null, resolve, data);
              }, function (jqXHR) {
                  jqXHR.then = null; // tame jQuery's ill mannered promises
                  Ember.run(null, reject, jqXHR);
              });
        }).then(function (value) {
            _this.set('validateExistingName', value ? 'Name already in use.' : false);
        });
    }.observes('model.label'),
    validateNames: function () {
        if (this.get('model.selected.name') != null)
            return (typeof this.get('validateNewName') === 'string') || (typeof this.get('validateWorkflowName') === 'string');
        else
            return (typeof this.get('validateExistingName') === 'string') || (typeof this.get('validateWorkflowName') === 'string');
    }.property('validateWorkflowName', 'validateNewName', 'validateExistingName'),

    selectedData: {},
    selectedDataisSingleEdgeError: '',
    selectedSingleEdgeID: 'none',
    selectedDataisSingleEdge: function(){
        var a = this.get('selectedData.edges');
        if (a && a.length == 1) {
            this.set('selectedSingleEdgeID', a[0])
            return true;
        } else {
            this.set('selectedSingleEdgeID', 'none')  
        }

        if (a && a.length == 0) {
            this.set('selectedDataisSingleEdgeError', 'No edge selected');
        } else {
            this.set('selectedDataisSingleEdgeError', 'More than one edge selected');
        }

        return false;
    }.property('selectedData','selectedData.edges', 'selectedData.nodes'),
    moneyModalStoreObject: {}, // this is for the money modal - all input fileds bind to this...
    actions: {
        createWorkflowInstance: function() {
            this.transitionToRoute('step', NewGUID(), { queryParams: { workflowID: this.get('workflowID') } });
        },
        translateWorkflow: function(workflowID, selectedID){
            this.transitionToRoute('translate', workflowID, {queryParams: {selected: selectedID}});
        },
        cancelWorkflowName: function (data, callback) {
            var wf = this.store.getById('workflow', this.get('workflowID'));
            if (typeof wf !== 'undefined' && wf && wf.currentState.parentState.dirtyType != 'created') {
                wf.rollback();
                if (typeof this.get('workflow') !== 'undefined')
                    this.get('workflow').set('name', wf.get('name'));
            }
            this.send('toggleworkflowEditNameModal', data, callback);
        },
        toggleworkflowEditNameModal: function () {
            this.toggleProperty('workflowEditNameModal');
        },
        toggleWorkflowNewModal: function (data, callback) {
            this.toggleProperty('workflowNewModal');
        },
        toggleWorkflowEditModal: function (data, callback) {
            this.toggleProperty('workflowEditModal');
        },
        toggleMoenyModal: function (data, callback) {
            // more like opening (not toggle) money modal
            var _this = this;

            this.set('moneyModal', true); // show the modal

            this.set('loadingMoney', true); // this will be used once data gets loaded in

            this.store.findQuery('task', {GraphDataGroupID: this.get('workflowID'), GraphDataID: this.get('graphID')}).then(function(a) {
                return a.get('firstObject');
            }, function(){
               // return new Promise(function(resolve, reject) {
                            var aPromise = _this.store.createRecord('task',{
                                id: NewGUID(), 
                                GraphDataGroupID: _this.get('workflowID'), 
                                GraphDataID: _this.get('graphID')
                            }) //.save()

                            // if save then not really a promise
                            return aPromise
                 //       });
            }).then(function(a){
                _this.set('moneyModalStoreObject', a);
                _this.set('loadingMoney', false); // this will be used once data gets loaded in
            });
        },
        submitMoneyModal: function (data, callback) {
            var a = this.get('moneyModalStoreObject');
            

            var _this = this;
            // Save Money Variables
            a.save().then(function(){
                Messenger().post({ type: 'success', message: 'Successfully updated details.' });
                _this.set('moneyModal', false);
            }, function(){
                Messenger().post({ type: 'error', message: 'Error updating details. Please try again.' });
            })
        },
        cancelMoneyModal: function (data, callback) {
            // Clear variable here
            this.set('moneyModal', false);
        },

        toggleTriggersModal: function (data, callback) {
            this.toggleProperty('triggerModal');
        },
        cancelTriggerModal: function (data, callback) {
            this.set('triggerModal', false);
        },
        submitTriggerModal: function (data, callback) {
            var a = this.get('moneyModalStoreObject');
            
            this.set('triggerModal', false);


            var _this = this;
            // Save Money Variables
            // a.save(function(){
            //     debugger;
            //     Messenger().post({ type: 'success', message: 'Successfully updated details.' });
            //     _this.set('moneyModal', false);
            // }, function(){
            //     debugger;
            //     Messenger().post({ type: 'error', message: 'Error updating details. Please try again.' });
            // })
        },
        toggleShareModal: function (data, callback) {
            this.toggleProperty('workflowShareModal');
        },
        togglePreview: function (data, callback) {
            if (this.get('preview') === 'true')
                this.set('preview', false);
            else if (this.get('preview') === 'false')
                this.set('preview', true);
            else this.toggleProperty('preview');
        },
        cancelWorkflowShare: function (data, callback) {
            this.set('workflowShareModal', false);
        },
        cancelProcess: function (data, callback) {
            var wf = this.store.getById('node', this.get('selectedID'));
            if (typeof wf !== 'undefined' && wf && wf.currentState.parentState.dirtyType != 'created') { //&& wf.get('label').search(/^Untitled Process - [0-9\@\- :]*$/ig) < 0)
                wf.rollback();
            }
            this.send('toggleWorkflowEditModal', data, callback);
        },
        updateDirtyGraph: function () {
            var _this = this;

            tinyMCE.triggerSave();

            newWorkflow = App.Node.store.getById('workflow', this.get('workflowID'));
            if (newWorkflow === null || typeof newWorkflow.get('name') === 'undefined') {
                newWorkflow = App.Node.store.createRecord('workflow', { id: this.get('workflowID'), name: this.get('model.workflow.name'), StartGraphDataID: _this.get('model.selectedID')  });
                this.set('model.workflow', newWorkflow);
            }
            else
                newWorkflow.set('name', this.get('workflowName'));

            var updateDirtyProcesses = function () {
                Enumerable.From(App.Node.store.all('node').content).Where("f=>f.get('isDirty') && f.id !=='" + _this.get('model.selectedID') + "'").ForEach(
                    function (newNode) {
                        if (Enumerable.From(newNode.get('workflows').content.content).Any("f=>f.id=='" + _this.get('workflowID') + "'")) {
                            newNode.save().then(function (f) {
                                Messenger().post({ type: 'success', message: 'Successfully Updated Changed Step' });
                            }, function () {
                                Messenger().post({ type: 'error', message: 'Error Updating Changed Step' });
                            });
                        }
                });
            };

            var updateProcesses = function () {
                var newNode = App.Node.store.getById('node', _this.get('selectedID'));
                if (typeof newNode === 'undefined' || !newNode) {
                    newNode = App.Node.store.createRecord('node', { id: _this.get('model.selectedID'), label: _this.get('model.selected.label'), content: _this.get('model.selected.content'), VersionUpdated: Ember.Date.parse(new Date()) });
                    this.set('model.selected', newNode);
                }
                if (newNode.get('isDirty')) {
                    newNode.save().then(function (f) {
                        Messenger().post({ type: 'success', message: 'Successfully Updated Step' });
                        updateDirtyProcesses();
                        //var a = { id: f.get('id'), label: f.get('label'), shape: f.get('shape'), group: f.get('group') }
                        _this.set('workflowEditModal', false);
                    }, function () {
                        if (_this.get('model'))
                            Messenger().post({ type: 'error', message: 'Error Updating Step' });
                        else
                            Messenger().post({ type: 'error', message: 'Error Adding Step' });;
                    });
                }
                else {
                    updateDirtyProcesses();
                }

            };

            if (newWorkflow.get('isDirty')) {
                this.set('model.workflow', newWorkflow);
                newWorkflow.save().then(function (data) {
                    Messenger().post({ type: 'success', message: 'Successfully Updated Workflow' });

                    //var wfid = _this.get('workflowID');
                    //var all = Enumerable.From(App.Node.store.all('node').content);
                    //var promises = [];
                    //var nodes = [];
                    //var edges = [];
                    //all.ForEach(function (f) {
                    //    promises.push(f.get('workflows').then(function (g) {
                    //        $.each(g.content, function (key, value) {
                    //            if (value.id == wfid) {
                    //                nodes.push(f);
                    //                //f.save();
                    //            }
                    //        });
                    //    }));
                    //});
                    //Ember.RSVP.allSettled(promises).then(function (array) {
                        if (_this.get('workflowGte2')) { //Reload page if update dropdown emberui text (bug in emberui)
                            //Enumerable.From(_this.get('model.workflows')).Where("f=>f.id==='" + _this.get('workflowID') + "'").Single().name = _this.get('model.workflow.name');
                            // _this.refresh();
                            location.reload();
                            // this.get('model.workflows').findProperty('id', this.get('workflowID')) - don't need linq.zzz
                        }
                        _this.set('workflowEditNameModal', false);
                        updateProcesses();
                    //});
                }, function () {
                    if (_this.get('workflowID'))
                        Messenger().post({ type: 'error', message: 'Error Updating Workflow.' });
                    else
                        Messenger().post({ type: 'error', message: 'Error Adding Workflow' });
                });
            } else {
                updateProcesses();
            }
        },
        addNewNode: function () {
            var _this = this;
            this.send('updateDirtyGraph');
            var c = this.get('newContent')
            var n = this.get('newName')
            var id = NewGUID();
            var newNode = App.Node.store.createRecord('node',{ id: id, label: n, content: c, VersionUpdated: Ember.Date.parse(new Date()) });
            newNode.save().then(function (f) {
                f.get('workflows').content.pushObject(_this.store.getById('workflow', _this.get('workflowID')));
                Messenger().post({ type: 'success', message: 'Successfully Added New Step' });
                _this.set('newName', null);
                _this.set('newContent', null);
                _this.toggleProperty('workflowNewModal');
                _this.set('updateGraph', NewGUID());
            }, function (o) {
                Messenger().post({ type: 'error', message: 'Error Adding New Step. No Permission.' });
                _this.store.unloadRecord(newNode);
            });
        },
        addNewEdge: function (data) {
            var _this = this;
            this.send('updateDirtyGraph'); // create a new workflow if it doens't exist
            data.id = NewGUID();
            data.GroupID = this.get('workflowID');
            var newEdge = App.Node.store.createRecord('edge', data);
            newEdge.save().then(function (o) {
                Messenger().post({ type: 'success', message: 'Successfully Added New Connection' });
                _this.set('updateGraph', NewGUID());
            }, function (o) {
                Messenger().post({type:'error', message:'Error Adding New Connection. No Permission. Can\'t connect to or from a node that is not in your permission list.'});
                _this.store.unloadRecord(newEdge);
            });
        },
        deleteGraphItems: function (data, callback) {
            var _this = this;
            var promises = [];
            Enumerable.From(data.edges).ForEach(function (f) {
                var m = App.Node.store.getById('edge', f);
                if (m)
                    promises.push(m.destroyRecord());
            });
            Enumerable.From(data.nodes).ForEach(function (f) {
                var m = App.Node.store.getById('node', f);
                if (m && !m.get('isNew')) {
                    var promise = new Ember.RSVP.Promise(function (resolve, reject) {
                        var wfid = _this.get('workflowID');
                        var gid = m.id;
                        $.ajax({
                            url: '/flow/nodes',
                            type: 'DELETE',
                            data: { id: gid, workflows: wfid },
                            success: function (result) {
                                // Do something with the result
                                m.unloadRecord();
                                resolve(result);
                            },
                            error: function(jqXHR, textStatus, errorThrown) {
                                reject(jqXHR);
                            }
                        });
                    });
                    promises.push(promise);
                }
            });
            Ember.RSVP.allSettled(promises).then(function (array) {
                if (Enumerable.From(array).Any("f=>f.state=='rejected'"))
                    Messenger().post({ type: 'error', message: 'Error Updating Workflow Deletions' });
                else {
                    _this = _this;

                    var currentSelected = _this.get('selected');
                    var currentWorkflow = _this.get('workflowID');
                    var isDeleted = false;
                    // var deletedNode = _this.store.filter('node', { id: currentSelected });

                    // After the delete check all the remaing nodes and edges
                    var all = Enumerable.From(App.Node.store.all('node').content);
                    var promises1 = [];
                    var nodes = [];
                    all.ForEach(function (f) {
                        promises1.push(f.get('workflows').then(function (g) {
                            $.each(g.content, function (key, value) {
                                if (value.id === currentWorkflow)
                                    nodes.push(f);
                            });
                        }));
                    });


                    Ember.RSVP.allSettled(promises1).then(function () {
                        //debugger;
                        var selectedNodeAlive = Enumerable.From(nodes).Where('f=>f.id=="' + currentSelected.get('id') + '"').Any()
                        if (!selectedNodeAlive) { // current node selected, have to redirect
                            if (nodes.length >= 1) {  // if selected node was deleted transition to this node
                                _this.transitionToRoute('graph', nodes[0].id);
                            } else {
                                Messenger().post({ type: 'info', message: 'All steps in current workflow deleted. Redirecting to search.' });
                                _this.transitionToRoute('search');
                            }
                        } else {
                            _this.set('updateGraph', NewGUID()); // make graph rerender
                        }
                    });

                    Messenger().post({ type: 'success', message: 'Successfully Updated Workflow Deletions' });
                }


            }, function (error) {

            });

        }
    }
});

App.GraphView = Ember.View.extend({
    didInsertElement : function(){
        this._super();
        this.selectedContentChanges();
    },
    selectedContentChanges: function () {
        Ember.run.debounce(this, renderDynamic, 150, false);
    }.observes('controller.model.selected.content'),
});

var renderDynamic = function () {
    window.cleanAlternative(this.$());
    window.renderFunctions(this.$());
};

function recurseGraphData(id, array, _this, depth, depthMax, nodeMax, store) {
    //AGTODO
    //return array;
    if (typeof id == 'undefined')
        return array;

    //TODO: nodemax based on sequence (priority) in edges
    if (!nodeMax || nodeMax < 0 || array.nodes.length < nodeMax) {
        var node = _this.store.getById(store, id);

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
    needs: ['graph'],
    editing: true,
    toggleEditing: function () {
        if (this.graph !== null) {
            this.graph.setOptions({
                dataManipulation: {
                    enabled: this.get('editing'),
                    initiallyVisible: true
                }
            });
        }
    }.observes('editing'),
    data: null,
    updateGraph: '',
    vizDataSet: { nodes: new vis.DataSet(), edges: new vis.DataSet() },
    classNames: ['vis-component'],
    selected: '',
    selectedName: '',
    workflowID: '',
    selectedData: {},
    isSelected: false,
    isSelectedEdge: function() {
        var a = this.get('selectedData.edges');
        console.log('1')
        if (a && a.length == 1) {
            // this.set('')
            return true;
        }
        return false;
    }.property('selectedData','selectedData.edges', 'selectedData.nodes'),
    isWorkflow: function () {
        return IsGUID(this.get('selected'));
    }.property(),
    graph: null,
    existingNodeModal: false,
    setup: function () {

        var _this = this;
        var groups = {};
        var centralGravity = -0.1155; //TODO HACK, AG, Less gravity for known graphs
        if (!this.get('isWorkflow')) {
            centralGravity = 0.5;
        }
        var container = $('<div>').appendTo(this.$())[0];
        var data = this.get('vizDataSet');
        var options = {
            navigation: true,
            //freezeForStabilization: true,
            //minVelocity: 5,
            //clustering: {
            //    enabled: true
            //},
            //configurePhysics: true,
            // labels:{
            //       add:"Add Step",
            //       edit:"Edit",
            //       link:"Add Connection",
            //       del:"Delete selected",
            //       editNode:"Edit Step",
            //       back:"Back",
            //       addDescription:"Click the empty space to create a Step.",
            //       linkDescription:"Connect Steps by dragging.",
            //       addError:"The function for add does not support two arguments (data,callback).",
            //       linkError:"The function for connect does not support two arguments (data,callback).",
            //       editError:"The function for edit does not support two arguments (data, callback).",
            //       editBoundError:"No edit function has been bound to this button.",
            //       deleteError:"The function for delete does not support two arguments (data, callback).",
            //       deleteClusterError:"Clusters cannot be deleted."
            // },
            //physics: {barnesHut: {enabled: false}, repulsion: {nodeDistance: 150, centralGravity: 0.15, springLength: 20, springConstant: 0, damping: 0.3}},
            smoothCurves: true,
            //hierarchicalLayout: {enabled:true},
            //physics: {barnesHut: {enabled: false, gravitationalConstant: -13950, centralGravity: 1.25, springLength: 150, springConstant: 0.335, damping: 0.3}},
            //physics: {barnesHut: {enabled: false}},
            //physics: { barnesHut: { gravitationalConstant: -8425, centralGravity: 0.1, springLength: 150, springConstant: 0.058, damping: 0.3 } },
            //physics: { barnesHut: { enabled: true, gravitationalConstant: -12000, centralGravity: centralGravity, springConstant: 0.01, damping: 0.1, springLength: 170 }, repulsion: { nodeDistance: 170} },
            //physics: { barnesHut: { springLength: 200, centralGravity: -0.1, damping: 0.1 } },
            //physics: { barnesHut: { enabled: true, gravitationalConstant: 2000, springLength: 200, centralGravity: 0.3, springCsontant: 0.04, damping: 0.09 } },
            physics: {
                barnesHut: {enabled: true, //},
                //repulsion: {
                    centralGravity: 0.3,
                    springLength: 200,
                    springConstant: 0.2,
                    //nodeDistance: 200,
                    damping: 0.096,
                    gravitationalConstant: -8000
                }
            },
            stabilize: true,
            //clustering: true,
            stabilizationIterations: 10000,
            dataManipulation: {
                enabled: this.get('editing'),
                initiallyVisible: true
            },
            onAdd: function (data, callback) {
                var cb = function (finalData) {
                    _this.graph._toggleEditMode();
                    callback(finalData);
                };
                _this.sendAction('toggleWorkflowNewModal', data, cb);
                if (_this.graph.editMode)
                    _this.graph._toggleEditMode();
                _this.set('isSelected', false);
            },
            onDelete: function (data, callback) {
                if (data.nodes.length > 0) {
                    var r = confirm("Sure you want to delete this step?");
                    if (!r) {
                        return false;
                    }
                }
                _this.sendAction('deleteGraphItems', data, callback);
                _this.set('isSelected', false);
            },
            onEdit: function (data, callback) {
                _this.sendAction('toggleWorkflowEditModal', data, callback);
            },
            onConnect: function (data, callback) {
                if (typeof _this.graph.connection !== 'undefined') {
                    data.RelationTypeID = _this.graph.connection.RelationTypeID;
                    _this.graph.connection.RelationTypeID = null;
                }
                function saveLink() {
                    _this.sendAction('addNewEdge', data, callback);
                }
                if (data.from == data.to) {
                    //TODO NOT SUPPORTED
                    //var r = confirm("Do you want to connect the node to itself?");
                    //if (r == true) {
                    //    saveLink()
                    //}
                }
                else {
                    saveLink()
                }
                _this.set('isSelected', false);
            }
        };

        // Initialise vis.js
        //console.log(container, data, options);
        this.graph = new vis.Network(container, data, options);
        // This sets the new selected item on click
        this.graph.on('click', function (data) {
            
            // Set the value on the component - used by a few computed properties including edit edge conditions...
            _this.set('selectedData', data);
            
            if (data.nodes.length > 0) {
                var wfid = _this.get('workflowID'); // has to be synched with data
                // either wikipedia OR node is part of workflow confirmed by store OR first node
                if (!IsGUID(data.nodes[0]) // wikipedia???
                    || Enumerable.From(App.Node.store.all('edge').content).Any("f=>f.get('GroupID')==='" + wfid + "' &&  (f.get('from') === '" + data.nodes[0] + "' || f.get('to') === '" + data.nodes[0] + "')") // existsin the edge store - not sure why relevant
                    || !Enumerable.From(App.Node.store.all('edge').content).Any("f=>f.get('GroupID')==='" + wfid + "' && f.get('to') !== null") // or the beginning node
                    //|| !Enumerable.From(App.Node.store.all('edge').content).Any("f=>f.get('GroupID')==='" + wfid + "' &&  ((f.get('from') === '" + _this.get('selected') + "' && f.get('to') !== null) || f.get('to') === '" + _this.get('selected') + "' )")
                    ) {
                    _this.set('selected', data.nodes[0]);
                    _this.set('isSelected', true);


                    //if (IsGUID(data.nodes[0])) {
                    //    var d = _this.get('vizDataSet');
                    //    var edges = d.edges.get();
                    //    var nodes = d.nodes.get();
                    //    var n = d.nodes.get(data.nodes[0]);
                    //    Enumerable.From(nodes).ForEach(
                    //        function (value) {
                    //            //delete value.color;
                    //            //delete value.fontColor;
                    //            if (!Enumerable.From(edges).Where("f=>f.to=='" + value.id + "' && f.group == '" + wfid + "'").Any() && value.group.indexOf(wfid) > -1) {
                    //                value.color = "#FFFFFF"; //BEGIN
                    //                value.fontColor = "#000000";
                    //            }
                    //            else if (!Enumerable.From(edges).Where("f=>f.from=='" + value.id + "' && f.group == '" + wfid + "'").Any()
                    //                    && (value.group.indexOf(wfid) > -1
                    //           || (value.group.indexOf(wfid) < 0 && Enumerable.From(edges).Where("f=>f.to=='" + value.id + "' && f.group == '" + wfid + "'").Any()))) {
                    //                value.color = "#333333"; //END
                    //                value.fontColor = "#FFFFFF";
                    //            }
                    //            else if (value.group.indexOf(wfid) > -1) {
                    //                value.color = "#6fa5d7"; //Current
                    //                value.fontColor = "#000000";
                    //            }
                    //            return value;
                    //        });
                    //    d.nodes.update(nodes);
                    //}
                }
                else {
                    _this.set('isSelected', false);
                }
            }
            else if (data.edges.length > 0)
                _this.set('isSelected', true);
             else
                _this.set('isSelected', false);
        });

        //this.graph.on('stabilized', function (iterations) {
        //    _this.graph.zoomExtent(); //Not working?!
        //});
        this.graph.scale = 0.82; //Zoom out a little

        $(window).resize(function () {
            setTimeout(function () {
                _this.graph.zoomExtent(); //Not working?!
                _this.graph.redraw(); // This makes the graph responsive!!!
            }, 500);
        });

        $(window).bind('orientationchange', function () {
            setTimeout(function () {
                _this.graph.zoomExtent(); //Not working?!
                _this.graph.redraw(); // This makes the graph responsive!!!
            }, 500);
        });

        setTimeout(function () {
            //_this.graph.zoomExtent(); //Not working?!
            _this.graph.redraw(); // This makes the graph responsive!!!
        }, 500);
    },
    dataUpdated: function (_this) {
        //console.log('updated graph ' + new Date())
        //var _this = this;
        if (_this.graph === null) {
            _this.setup(); // graph hasn't been initialised yet
        }
        var wfid = _this.get('workflowID');

        var updateGraph = function (nodes, edges) {
            nodes = $.map(nodes, function (item) { return { id: item.get('id'), label: item.get('localName'), mass: 1, group: item.get('group') }; });
            edges = $.map(edges, function (item) { return { id: item.get('id'), from: item.get('from'), to: item.get('to'), style: item.get('style'), color: item.get('color') }; });
            Enumerable.From(nodes).ForEach(
                function (value) {
                    //delete value.color;
                    //delete value.fontColor;
                    //console.log(value.id.replace(/\'/ig, '\''));
                    if (!Enumerable.From(edges).Where("f=>f.to=='" + value.id.replace(/'/ig, '\\\'') + "'").Any()) {
                        value.color = "#FFFFFF"; //BEGIN
                        value.fontColor = "#000000";
                    }
                    else if (!Enumerable.From(edges).Where("f=>f.from=='" + value.id.replace(/'/ig, '\\\'') + "'").Any()) {
                        value.color = "#333333"; //END
                        value.fontColor = "#FFFFFF";
                        if (!IsGUID(value.id) && Enumerable.From(edges).Where("f=>f.to=='" + value.id.replace(/'/ig, '\\\'') + "'").Count() > 1)
                            value.color = "salmon";
                    }
                    else {
                        value.color = "#6fa5d7"; //Current
                        value.fontColor = "#000000";
                    }
                    return value;
                });

            var md = {
                nodes: nodes,
                edges: edges,
            };
            var d = _this.get('vizDataSet');
            var select = _this.get('selected');

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

            //var firstNode = true;
            ////Step 1a: Clean Nodes for Presentation
            //md.nodes = Enumerable.From(md.nodes).Select(
            //    function (value, index) {
            //        if (typeof value !== 'undefined' && typeof value.label === 'string')
            //            value.label = value.label.replace(/_/g, ' ');
            //        value.mass = 0.8;
            //        if (IsGUID(value.id)) {
            //            if (firstNode) {
            //                //value.x = 150; //TODO Shake the sedentary nodes out
            //                firstNode = false;
            //            }
            //            delete value.color;
            //            delete value.fontColor;
            //            if (!Enumerable.From(md.edges).Where("f=>f.to=='" + value.id + "' && f.group == '" + md.workflowID + "'").Any() && value.group.indexOf(md.workflowID) > -1) {
            //                value.color = "#FFFFFF"; //BEGIN
            //                value.fontColor = "#000000";
            //            }
            //            else if (!Enumerable.From(md.edges).Where("f=>f.from=='" + value.id + "' && f.group == '" + md.workflowID + "'").Any()
            //                && (value.group.indexOf(md.workflowID) > -1
            //                    || (value.group.indexOf(md.workflowID) < 0 && Enumerable.From(md.edges).Where("f=>f.to=='" + value.id + "' && f.group == '" + md.workflowID + "'").Any()))) {
            //                value.color = "#333333"; //END
            //                value.fontColor = "#FFFFFF";
            //            }
            //            else if (value.group.indexOf(md.workflowID) > -1) {
            //                value.color = "#6fa5d7"; //Current
            //                value.fontColor = "#000000";
            //            }
            //        }
            //        else if (typeof value.group === 'undefined') {
            //            value.color = "#6fa5d7"; //Current
            //            value.fontColor = "#000000";
            //        }
            //        return value;
            //    }).ToArray();

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


            var newEdges = md.edges.filter(function (edge) {
                return (d.nodes.get(edge.from) !== null && d.nodes.get(edge.to) !== null);
            });


            //Enumerable.From(md.nodes).ForEach(function (f) {
            //    //Add similar group if in workflow
            //});
            d.edges.update(newEdges);

            if (d.nodes.get(select) !== null && _this.graph.getSelectedNodes().length == 0)
                _this.graph.selectNodes([select]);
        }



        if (!IsGUID(_this.get('selected'))) {
            var all = Enumerable.From(App.Wikipedia.store.all('wikipedia').content);
            var edges = Enumerable.From(App.Wikipedia.store.all('edge').content).Where("f=>typeof f.get('GroupID')=='undefined'");
            var cleaned = Enumerable.Empty();
            var nodes = Enumerable.Empty();
            Enumerable.From(edges).GroupBy("$.get('origin')", "$").ForEach(function (data) {
                var clean = Enumerable.From(data.source).Take(20);
                cleaned = cleaned.Concat(clean);
                nodes = nodes.Concat(all.Intersect(clean.Select(function (edge) { return { id: edge.get('to') }; }), "$.id"));
                nodes = nodes.Concat(all.Where("$.id=='" + data.Key() + "'")); //from
            });
            nodes.Distinct().ForEach(function (data) {
                cleaned = cleaned.Concat(edges.Where("f=>f.get('destination') === '" + data.get('dataName') + "'"));
            });

            updateGraph(nodes.ToArray(), cleaned.Distinct().ToArray());
        }
        else {
            var all = Enumerable.From(App.Node.store.all('node').content);
            var nodes = [];
            var edges = [];
            var promises = [];
            all.ForEach(function (f) {
                promises.push(f.get('workflows').then(function (g) {
                    $.each(g.content, function (key, value) {
                        if (value.id == wfid)
                            nodes.push(f);
                    });
                }));
            });

            if (!Enumerable.From(nodes).Any("f=>f.id=='" + _this.get('selected') + "'"))
                nodes.push(App.Node.store.getById('node', _this.get('selected')));

            var waitForLocalePromises = function (nodes, edges) {
                Enumerable.From(nodes).Select("$.get('localName')").ToArray();
                if (Enumerable.From(nodes).Where("f=>f.get('isNew') == false").Select("typeof $.get('_localePromise') === 'undefined'").Any("f=>f"))
                    setTimeout(function () { waitForLocalePromises(nodes, edges) }, 150);
                else
                    Ember.RSVP.allSettled(Enumerable.From(nodes).Select("$.get('_localePromise')").ToArray()).then(function (array) {
                        updateGraph(nodes, edges);
                    });
            };

            Ember.RSVP.allSettled(promises).then(function (array) {
                edges = Enumerable.From(App.Node.store.all('edge').content).Where("f=>f.get('GroupID')=='" + wfid + "'").ToArray();
                //Ember.run.schedule('afterRender', this, updateGraph, nodes, edges);
                //updateGraph(nodes,edges);
                waitForLocalePromises(nodes, edges);

            });
        }

    },
    dataUpdates: function () {
        //var _this = this;
        //var a = function () {
        //    Ember.run.scheduleOnce('afterRender', _this, _this.dataUpdated, _this);
        //} //TODO optimise further
        Ember.run.debounce(this, this.dataUpdated, this, 100, false);
    }.observes('workflowID', 'data', 'data.nodes', 'data.edges', 'selectedName', 'updateGraph', 'data.updateGraph.updated').on('didInsertElement'),
    actions: {
        edgeCreateY: function () {
            if (typeof this.graph.connection === 'undefined')
                this.graph.connection = {};
            this.graph.connection.RelationTypeID = 'bc4f2c1f-e25e-4849-a7f9-878c41aa6847';
            if (!this.graph.editMode)
                this.graph._toggleEditMode();
            else {
                this.graph._toggleEditMode();
                this.graph._toggleEditMode();
            }
            this.graph._createAddEdgeToolbar();
        },
        edgeCreateN: function () {
            if (typeof this.graph.connection === 'undefined')
                this.graph.connection = {};
            this.graph.connection.RelationTypeID = '4d6a4003-2dd7-404e-a3cc-8d96d8237aa7';
            if (!this.graph.editMode)
                this.graph._toggleEditMode();
            else {
                this.graph._toggleEditMode();
                this.graph._toggleEditMode();
            }
            this.graph._createAddEdgeToolbar();
        },
        edgeCreate: function () {
            if (typeof this.graph.connection === 'undefined')
                this.graph.connection = {};
            this.graph.connection.RelationTypeID = null;
            if (!this.graph.editMode)
                this.graph._toggleEditMode();
            else {
                this.graph._toggleEditMode();
                this.graph._toggleEditMode();
            }
            this.graph._createAddEdgeToolbar();
        },
        processCreate: function () {
            if (!this.graph._selectionIsEmpty())
                this.graph._unselectAll();
            if (!this.graph.editMode)
                this.graph._toggleEditMode();
            else {
                this.graph._toggleEditMode();
                this.graph._toggleEditMode();
            }
            this.graph._createAddNodeToolbar();
            this.graph._addNode();
        },
        selectionDelete: function () {
            if (!this.graph.editMode)
                this.graph._toggleEditMode();
            else {
                this.graph._toggleEditMode();
                this.graph._toggleEditMode();
            }
            this.graph._deleteSelected();
        },
        showExistingNodeModal: function (item) {


            this.set('existingNodeModal', true); // Show the modal before anything else

            // Make selectbox work after it's been inserted to the view - jquery hackss
            Ember.run.scheduleOnce('afterRender', this, function () {
                $('#existingNodesel').select2({
                    placeholder: "Enter Step...",
                    minimumInputLength: 2,
                    tags: true,
                    //createSearchChoice : function (term) { return {id: term, text: term}; },  // thus is good if you want to use the type in item as an option too
                    ajax: { // instead of writing the function to execute the request we use Select2's convenient helper
                        url: "/flow/searches",
                        dataType: 'json',
                        multiple: true,
                        data: function (term, page) {
                            return { keywords: term, type: 'process', pageSize: 8, page: page - 1 };
                        },
                        results: function (data, page) { // parse the results into the format expected by Select2.
                            if (data.search.length === 0) {
                                return { results: [] };
                            }
                            var total = data.search[0].TotalRows;
                            var more = (page * 8) < total;
                            var results = Enumerable.From(data.search).Select("f=>{id:f.id + f.Title,tag:f.Title}").ToArray();
                            return { results: results, text: 'tag', more: more };
                        }
                    },
                    formatResult: function (state) { return state.tag; },
                    formatSelection: function (state) { return state.tag; },
                    escapeMarkup: function (m) { return m; }
                });
            });
        },
        submitExistingNodeModal: function () {
            var _this = this;

            var existingNodes = $('#existingNodesel').val()



            if (existingNodes !== '') {
                Enumerable.From(existingNodes.split(',')).ForEach(function (f) {
                    // check if node already in store???
                    var id = f.substring(0, 36)
                    var name = f.substring(36)



                    var result = App.Node.store.getById('node', id);
                    if (result === null) {
                        // need to push record into store
                        App.Node.store.push('node', {
                            id: id,
                            label: name
                        });
                    }

                    // check if already in graph
                    var currentNodesonScreen = _this.get('vizDataSet');

                    var found = false
                    if (currentNodesonScreen) {
                        found = (Enumerable.From(currentNodesonScreen.nodes.getIds()).Any("f=>f==='" + id + "'"));
                    }
                    if (!found) {
                        var newNode = App.Node.store.getById('node', id);
                        var o = newNode.get('workflows').then(function (w) {
                            w.content.pushObject(App.Node.store.getById('workflow', _this.get('workflowID')));
                        });
                        var a = { id: newNode.get('id'), label: newNode.get('label'), shape: newNode.get('shape'), group: newNode.get('group') };
                        currentNodesonScreen.nodes.add(a);
                        //_this.get('data').nodes.push(a); //, currentNodesonScreen.nodes.concat([]));

                    } else {
                        alert('Step already in workflow.')
                    }
                });
            }

            this.set('existingNodeModal', false);
        },
        cancelExistingNodeModal: function () {
            this.set('existingNodeModal', false);
        }
    }
});


/* Editor
-------------------------------------------------- */
App.EditorRoute = Ember.Route.extend({
    lastLoadedWorkflowLocale: '',
    queryParams: {
        workflowID: {
            refreshModel: true
        },
        preview: {
            refreshModel: true
        }
    },
    actions: {
        error: function(){
            Messenger().post({ type: 'error', message: 'Could not find step. Ensure you have permission and are logged in.' });
            // Ember.run.later(null, RedirectToLogin, 3000); maybe not do a refresh
        }
    },
    beforeModel: function (params) {
        if (params.queryParams.workflowID === 'undefined') {
            this.replaceWith('editor', params.params.graph.id, { queryParams: { workflowID: NewGUID() } });
        }
    },
    model: function (params) {
        var _this = this;
        var id = params.id;
        id = id.toLowerCase(); // just in case
        //var workflow = new Promise(function (resolve, reject) {
        //    (function (resolve) {
        //        resolve();
        //    }, function () {
        //        resolve();
        //    })
        //});
        return Ember.RSVP.hash({
            data: this.store.find('node', { id: id, groupid: params.workflowID }),
            workflow: this.store.find('workflow', params.workflowID).catch(function (reason) {
                var groupID = Enumerable.From(_this.store.all('edge').content).Where("f=>f.get('from') ==='" + id + "' && f.get('to') === null").Select("f=>f.get('GroupID')").FirstOrDefault();
                if (typeof groupID !== 'undefined' && document.URL.indexOf(groupID) < 1) {
                    return _this.store.find('workflow', groupID).then(function (wfid) {
                        _this.replaceWith('editor', id, { queryParams: { workflowID: groupID } });
                    });
                }
                else if (typeof groupID === 'undefined' || !groupID)
                    return App.Workflow.store.createRecord('workflow', { id: params.workflowID, name: 'Untitled Workflow - ' + moment().format('YYYY-MM-DD @ HH:mm:ss'), StartGraphDataID: id })
            }),
            duplicateNode: $.get('/flow/NodeDuplicateID/' + id),
            selectedID: id,
            workflowID: params.workflowID,
            updateGraph: Ember.Object.create(),
            content: '',
            label: '',
            editing: true,  // This gets passed to visjs to enable/disable editing dependig on context
            params: params,
            links: { prev: [], next: [], up: [], down: [] },
            preview: params.preview
        });
    },
    afterModel: function (m) {
        var _this = this;
        m.selected = this.store.getById('node', m.selectedID);
        if (m.selected) {
            //Get the selected item from m.data

            var matchedWorkflow = Enumerable.From(m.selected._data.workflows).Where("f=>f.id == '" + m.workflowID + "'").Any();
            if (!matchedWorkflow) { // The current node does not exist with the query param (workflowID)
                var firstWorkflow = Enumerable.From(m.selected._data.workflows).FirstOrDefault();
                if (firstWorkflow)
                    this.replaceWith('editor', m.selectedID, { queryParams: { workflowID: firstWorkflow.id } }); // choose the first workflowID which the node is in
                else {
                    //this.replaceWith('graph', m.selectedID, { queryParams: { workflowID: NewGUID() } });  //leave out otherwise recursive
                    // create the workflow here
                    m.workflow = App.Workflow.store.getById('workflow', m.workflowID);
                    if (!m.workflow)
                        m.workflow = App.Workflow.store.createRecord('workflow', { id: m.workflowID, name: 'Untitled Workflow - ' + moment().format('YYYY-MM-DD @ HH:mm:ss'), StartGraphDataID : m.selectedID });
                }
            }
            else {
                m.workflow = this.store.getById('workflow', m.workflowID);
                m.updateGraph.set('updated', NewGUID());
            }

            //Now get links
            var edges =  Enumerable.From(_this.store.all('edge').content);
            var prev = edges.Where("f=>f.get('GroupID')==='" + m.workflowID + "' && f.get('to')==='" + m.selectedID + "'").Distinct();
            var next = edges.Where("f=>f.get('GroupID')==='" + m.workflowID + "' && f.get('from')==='" + m.selectedID + "'").Distinct();
            var into = edges.Where("f=>f.get('GroupID')!=='" + m.workflowID + "' && f.get('to')==='" + m.selectedID + "'").Distinct();
            var out = edges.Where("f=>f.get('GroupID')!=='" + m.workflowID + "' && f.get('from')==='" + m.selectedID + "'").Distinct();

            var nodes = Enumerable.From(_this.store.all('node').content);
            m.links.prev = prev.Join(nodes, "$.get('from')", "$.id", "outer,inner=>{node:inner, wfid:outer.get('GroupID'), wfname:outer.get('groupName'), color: outer.get('color.color')}").ToArray();
            m.links.next = next.Join(nodes, "$.get('to')", "$.id", "outer,inner=>{node:inner, wfid:outer.get('GroupID'), wfname:outer.get('groupName'), color: outer.get('color.color')}").ToArray();
            m.links.into = into.Join(nodes, "$.get('from')", "$.id", "outer,inner=>{node:inner, wfid:outer.get('GroupID'), wfname:outer.get('groupName'), color: outer.get('color.color')}").ToArray();
            m.links.out = out.Join(nodes, "$.get('to')", "$.id", "outer,inner=>{node:inner, wfid:outer.get('GroupID'), wfname:outer.get('groupName'), color: outer.get('color.color')}").ToArray();


        } else { //NEW NODE

            //TODO by default transition to en-US
            var currentLocale = App.get('localeSelected');
            var thisLoadedWorkflowLocale = m.params.workflowID + currentLocale;
            if (IsGUID(m.params.workflowID) && currentLocale != defaultLocale && this.get('lastLoadedWorkflowLocale') !== thisLoadedWorkflowLocale) {
                this.set('lastLoadedWorkflowLocale', thisLoadedWorkflowLocale);
                if (!Enumerable.From(this.store.all('translation').content).Any("f=>f.get('DocID') ==='" + m.params.workflowID + "' && f.get('TranslationCulture') ==='" + currentLocale + "'"))
                    this.store.find('translation', { docid: m.params.workflowID, TranslationCulture: currentLocale, DocType: 'flows' })
                        .then(function () { }, function () {
                            m.params.localeSelected = defaultLocale;
                            _this.replaceWith('editor', m.params.id, { queryParams: { localeSelected: defaultLocale, workflowID: m.params.workflowID } });
                            return;
                        });
            }

            // Before creating a new node, check if it already exists, because if it does, then must be a permission problem
            if (m.duplicateNode) {
                Messenger().post({ type: 'error', message: 'Permission denied. Ensure you have permission and are logged in.' });
                _this.transitionTo('login');
            }

            // Reset (creating a new node)
            m.content = '';
            m.label = '';
            m.editing = true;
            m.humanName = '';
            m.workflow = this.store.getById('workflow', m.params.workflowID);
            if (!m.workflow) {
                m.workflow = App.Workflow.store.createRecord('workflow', {
                    id: m.params.workflowID,
                    name: 'Untitled Workflow - ' + moment().format('YYYY-MM-DD @ HH:mm:ss'),
                    StartGraphDataID: m.selectedID
                });
            }
            m.workflows = Em.A([m.workflow]);

            var newNode = App.Node.store.createRecord('node', {
                id: m.selectedID,
                label: 'Untitled Step - ' + moment().format('YYYY-MM-DD @ HH:mm:ss'),
                content: '',
                VersionUpdated: Ember.Date.parse(moment().format('YYYY-MM-DD @ HH:mm:ss'))
            });

            newNode.get('workflows').then(function (w) {
                w.content.pushObject(m.workflow);
                m.selected = newNode;
                m.workflowID = m.params.workflowID;
                m.selectedID = m.selectedID;
            });

        }



    }

});



App.EditorController = Ember.ObjectController.extend({
    needs: ['application'],
    queryParams: ['workflowID', 'preview', 'localeSelected'],
    title: function(){
        var name = this.get('selected.humanName') + ' Step';
        App.setTitle(name);
        return name;
    }.property('selected.humanName'),
    nextSteps: function(){
        return ((this.get('model.links.prev') && this.get('model.links.prev').length > 0) || (this.get('model.links.next') && this.get('model.links.next').length > 0));
    }.property('model.links.prev','model.links.next'),
    nextWorkflowSteps: function(){
        return ((this.get('model.links.into') && this.get('model.links.into').length > 0) || (this.get('model.links.out') && this.get('model.links.out').length > 0));
    }.property('model.links.into','model.links.out'),
    newName: null,
    previewNext: function () {
        return (!this.get('nextSteps') && this.get('preview'));
    }.property('preview', 'nextSteps'),
    newContent: null,
    workflowEditNameModal: false,
    workflowShareModal: false,
    workflowNewModal: false, // up to here is for new ones
    editing: true,
    updateGraph: '',
    workflowName: function () {
        var a = this.store.getById('workflow', this.get('workflowID'));
        if (a)
            a = a.get('name');
        else
            a = 'Untitled Workflow - ' + moment().format('YYYY-MM-DD @ HH:mm:ss');
        return a;
    }.property('workflowID', 'model.workflow.name'),
    isPreview: function () {
        var preview = this.get('preview');
        if (typeof preview === 'undefined' || preview === 'undefined' || preview === false || preview === 'false' || preview === null || preview === 'null')
            return false;
        else
            return true;
    }.property('preview'),
    columnLayout: function () {
        if (this.get('isPreview'))
            return 'process-header col-md-12';
        else
            return 'process-header col-md-6';
    }.property('preview'),
    workflowID: null, // available ids will be in model
    preview: null,
    localeSelected: null,
    currentURL: function () {
        return window.document.URL;
    }.property('workflowID', 'model.selected', 'preview', 'generatePreviewLink'),
    previewURL: function () {
        if (queryParamsLookup('preview') !== null && queryParamsLookup('preview') !== 'false' && queryParamsLookup('preview') !== false)
            return window.document.URL;
        else
            return window.document.URL + '&preview=true';
    }.property('workflowID', 'model.selected', 'preview', 'generatePreviewLink'),
    generatePreviewLink: false,
    workflowEditModal : false,
    validateWorkflowName: false,
    validateNewName: false,
    validateNewNewName: false,
    loadingNewNewName: false,
    validateExistingName: false,
    loadingWorkflowName: false,
    loadingNewName: false,
    loadingExistingName: false,
    workflows: function(){
        return this.get('model.selected')._data.workflows;
    }.property('model.selected'),
    workflowsOptionsArray: function () {
        if (typeof this.get('workflows') === 'undefined')
            return [];
        var x =  this.get('workflows').map(function (a) {
            return { name: a.get('name'), id: a.id }
        });
        return x;
    }.property('workflows'),
    workflowGte2: Ember.computed.gte('workflows.length', 2),
    fitVis: function(){
        Ember.run.scheduleOnce('afterRender', this, function(){
            $('body').fitVids();
        })
    }.observes('model.content'),
    //humanReadableName: function () {
    //    var temp = this.get('model.selected.label');
    //    if (temp)
    //        return ToTitleCase(temp.replace(/_/g, ' '));
    //    else
    //        return null;
    //}.property('model.selected.label'),

    // Do something if the s
    graphDataTrigger : function () {

    }.observes('model', 'model.selected', 'model.@each.workflows'),
    changeSelected: function () {
        //var found = false;
        //var edges = Enumerable.From(this.get('graphData').edges);
        //if (edges.Any("f=>f.from=='" + this.get('model.selectedID') + "'") || edges.Any("f=>f.to=='" + this.get('model.selectedID') + "'"))
        this.transitionToRoute('editor', this.get('model.selectedID'), { queryParams: { localeSelected: App.get('localeSelected') } });
    }.observes('model.selectedID'),
    checkWorkflowName: function () {
        var _this = this;
        if (!_this.get('model.workflow.name') || typeof _this.get('model.workflow.name') !== 'string' || _this.get('model.workflow.name').trim().length < 1) {
            _this.set('validateWorkflowName', 'Name required.');
            return;
        }
        _this.set('loadingWorkflowName', true);
        return new Ember.RSVP.Promise(function (resolve, reject) {
            jQuery.getJSON('/Flow/WorkflowDuplicate?id=' + encodeURIComponent(_this.get('model.workflow.name').trim()) + '&guid=' + _this.get('workflowID')
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

    }.observes('model.workflow.name'),
    checkNewNodeName: function () {
        var _this = this;
        if (!_this.get('model.selected.label') || typeof _this.get('model.selected.label') !== 'string' || _this.get('model.selected.label').trim().length < 1) {
            _this.set('validateNewName', 'Name required.');
            return;
        }
        _this.set('loadingNewName', true);
        return new Ember.RSVP.Promise(function (resolve, reject) {
            jQuery.getJSON('/Flow/NodeDuplicate?id=' + encodeURIComponent(_this.get('model.selected.label').trim()) + '&guid=' + _this.get('selectedID')
              ).then(function (data) {
                  _this.set('loadingNewName', false);
                  Ember.run(null, resolve, data);
              }, function (jqXHR) {
                  jqXHR.then = null; // tame jQuery's ill mannered promises
                  Ember.run(null, reject, jqXHR);
              });
        }).then(function (value) {
            _this.set('validateNewName', value ? 'Name already in use.' : false);
        });

    }.observes('model.selected.label'),
    checkNewNewNodeName: function () {
        var _this = this;
        if (!_this.get('newName') || typeof _this.get('newName') !== 'string' || _this.get('newName').trim().length < 1) {
            _this.set('validateNewNewName', 'Name required.');
            return;
        }
        _this.set('loadingNewNewName', true);
        return new Ember.RSVP.Promise(function (resolve, reject) {
            jQuery.getJSON('/Flow/NodeDuplicate?id=' + encodeURIComponent(_this.get('newName').trim()) + '&guid=' + _this.get('selectedID')
              ).then(function (data) {
                  _this.set('loadingNewNewName', false);
                  Ember.run(null, resolve, data);
              }, function (jqXHR) {
                  jqXHR.then = null; // tame jQuery's ill mannered promises
                  Ember.run(null, reject, jqXHR);
              });
        }).then(function (value) {
            _this.set('validateNewNewName', value ? 'Name already in use.' : false);
        });

    }.observes('newName'),
    checkExistingNodeName: function () {
        var _this = this;
        if (!_this.get('model.label') || typeof _this.get('model.label') !== 'string' || _this.get('model.label').trim().length < 1) {
            _this.set('validateExistingName', 'Name required.');
            return;
        }
        _this.set('loadingExistingName', true);
        return new Ember.RSVP.Promise(function (resolve, reject) {
            jQuery.getJSON('/Flow/NodeDuplicate?id=' + encodeURIComponent(_this.get('model.label').trim()) + '&guid=' + _this.get('selectedID')
              ).then(function (data) {
                  _this.set('loadingExistingName', false);
                  Ember.run(null, resolve, data);
              }, function (jqXHR) {
                  jqXHR.then = null; // tame jQuery's ill mannered promises
                  Ember.run(null, reject, jqXHR);
              });
        }).then(function (value) {
            _this.set('validateExistingName', value ? 'Name already in use.' : false);
        });
    }.observes('model.label'),
    validateNames: function () {
        if (this.get('model.selected.name') != null)
            return (typeof this.get('validateNewName') === 'string') || (typeof this.get('validateWorkflowName') === 'string');
        else
            return (typeof this.get('validateExistingName') === 'string') || (typeof this.get('validateWorkflowName') === 'string');
    }.property('validateWorkflowName', 'validateNewName', 'validateExistingName'),

    actions: {
        translateWorkflow: function(workflowID, selectedID){
            this.transitionTo('translate', workflowID, {queryParams: {selected: selectedID}});
        },
        cancelWorkflowName: function (data, callback) {
            var wf = this.store.getById('workflow', this.get('workflowID'));
            if (typeof wf !== 'undefined' && wf && wf.currentState.parentState.dirtyType != 'created') {
                wf.rollback();
                if (typeof this.get('workflow') !== 'undefined')
                    this.get('workflow').set('name', wf.get('name'));
            }
            this.send('toggleworkflowEditNameModal', data, callback);
        },
        toggleworkflowEditNameModal: function () {
            this.toggleProperty('workflowEditNameModal');
        },
        toggleWorkflowNewModal: function (data, callback) {
            this.toggleProperty('workflowNewModal');
        },
        toggleWorkflowEditModal: function (data, callback) {
            this.toggleProperty('workflowEditModal');
        },
        toggleShareModal: function (data, callback) {
            this.toggleProperty('workflowShareModal');
        },
        togglePreview: function (data, callback) {
            if (this.get('preview') === 'true')
                this.set('preview', false);
            else if (this.get('preview') === 'false')
                this.set('preview', true);
            else this.toggleProperty('preview');
        },
        cancelWorkflowShare: function (data, callback) {
            this.set('workflowShareModal', false);
        },
        cancelProcess: function (data, callback) {
            var wf = this.store.getById('node', this.get('selectedID'));
            if (typeof wf !== 'undefined' && wf && wf.currentState.parentState.dirtyType != 'created') { //&& wf.get('label').search(/^Untitled Process - [0-9\@\- :]*$/ig) < 0)
                wf.rollback();
            }
            this.send('toggleWorkflowEditModal', data, callback);
        },
        updateDirtyGraph: function () {
            var _this = this;

            tinyMCE.triggerSave();

            newWorkflow = App.Node.store.getById('workflow', this.get('workflowID'));
            if (newWorkflow === null || typeof newWorkflow.get('name') === 'undefined') {
                newWorkflow = App.Node.store.createRecord('workflow', { id: this.get('workflowID'), name: this.get('model.workflow.name'), StartGraphDataID: _this.get('model.selectedID') });
                this.set('model.workflow', newWorkflow);
            }
            else
                newWorkflow.set('name', this.get('workflowName'));

            var updateDirtyProcesses = function () {
                Enumerable.From(App.Node.store.all('node').content).Where("f=>f.get('isDirty') && f.id !=='" + _this.get('model.selectedID') + "'").ForEach(
                    function (newNode) {
                        if (Enumerable.From(newNode.get('workflows').content.content).Any("f=>f.id=='" + _this.get('workflowID') + "'")) {
                            newNode.save().then(function (f) {
                                Messenger().post({ type: 'success', message: 'Successfully Updated Changed Step' });
                            }, function () {
                                Messenger().post({ type: 'error', message: 'Error Updating Changed Step' });
                            });
                        }
                });
            };

            var updateProcesses = function () {
                var newNode = App.Node.store.getById('node', _this.get('selectedID'));
                if (typeof newNode === 'undefined' || !newNode) {
                    newNode = App.Node.store.createRecord('node', { id: _this.get('model.selectedID'), label: _this.get('model.selected.label'), content: _this.get('model.selected.content'), VersionUpdated: Ember.Date.parse(new Date()) });
                    this.set('model.selected', newNode);
                }
                if (newNode.get('isDirty')) {
                    newNode.save().then(function (f) {
                        Messenger().post({ type: 'success', message: 'Successfully Updated Step' });
                        updateDirtyProcesses();
                        //var a = { id: f.get('id'), label: f.get('label'), shape: f.get('shape'), group: f.get('group') }
                        _this.set('workflowEditModal', false);
                    }, function () {
                        if (_this.get('model'))
                            Messenger().post({ type: 'error', message: 'Error Updating Step' });
                        else
                            Messenger().post({ type: 'error', message: 'Error Adding Step' });;
                    });
                }
                else {
                    updateDirtyProcesses();
                }

            };

            if (newWorkflow.get('isDirty')) {
                this.set('model.workflow', newWorkflow);
                newWorkflow.save().then(function (data) {
                    Messenger().post({ type: 'success', message: 'Successfully Updated Workflow' });

                    //var wfid = _this.get('workflowID');
                    //var all = Enumerable.From(App.Node.store.all('node').content);
                    //var promises = [];
                    //var nodes = [];
                    //var edges = [];
                    //all.ForEach(function (f) {
                    //    promises.push(f.get('workflows').then(function (g) {
                    //        $.each(g.content, function (key, value) {
                    //            if (value.id == wfid) {
                    //                nodes.push(f);
                    //                //f.save();
                    //            }
                    //        });
                    //    }));
                    //});
                    //Ember.RSVP.allSettled(promises).then(function (array) {
                        if (_this.get('workflowGte2')) { //Reload page if update dropdown emberui text (bug in emberui)
                            //Enumerable.From(_this.get('model.workflows')).Where("f=>f.id==='" + _this.get('workflowID') + "'").Single().name = _this.get('model.workflow.name');
                            // _this.refresh();
                            location.reload();
                            // this.get('model.workflows').findProperty('id', this.get('workflowID')) - don't need linq.zzz
                        }
                        _this.set('workflowEditNameModal', false);
                        updateProcesses();
                    //});
                }, function () {
                    if (_this.get('workflowID'))
                        Messenger().post({ type: 'error', message: 'Error Updating Workflow.' });
                    else
                        Messenger().post({ type: 'error', message: 'Error Adding Workflow' });
                });
            } else {
                updateProcesses();
            }
        },
        addNewNode: function () {
            var _this = this;
            this.send('updateDirtyGraph');
            var c = this.get('newContent')
            var n = this.get('newName')
            var id = NewGUID();
            var newNode = App.Node.store.createRecord('node',{ id: id, label: n, content: c, VersionUpdated: Ember.Date.parse(new Date()) });
            newNode.save().then(function (f) {
                f.get('workflows').content.pushObject(_this.store.getById('workflow', _this.get('workflowID')));
                Messenger().post({ type: 'success', message: 'Successfully Added New Step' });
                _this.set('newName', null);
                _this.set('newContent', null);
                _this.toggleProperty('workflowNewModal');
                _this.set('updateGraph', NewGUID());
            }, function (o) {
                Messenger().post({ type: 'error', message: 'Error Adding New Step. No Permission.' });
                _this.store.unloadRecord(newNode);
            });
        },
        addNewEdge: function (data) {
            var _this = this;
            this.send('updateDirtyGraph'); // create a new workflow if it doens't exist
            data.id = NewGUID();
            data.GroupID = this.get('workflowID');
            var newEdge = App.Node.store.createRecord('edge', data);
            newEdge.save().then(function (o) {
                Messenger().post({ type: 'success', message: 'Successfully Added New Connection' });
                _this.set('updateGraph', NewGUID());
            }, function (o) {
                Messenger().post({type:'error', message:'Error Adding New Connection. No Permission. Can\'t connect to or from a node that is not in your permission list.'});
                _this.store.unloadRecord(newEdge);
            });
        },
        deleteGraphItems: function (data, callback) {
            var _this = this;
            var promises = [];
            Enumerable.From(data.edges).ForEach(function (f) {
                var m = App.Node.store.getById('edge', f);
                if (m)
                    promises.push(m.destroyRecord());
            });
            Enumerable.From(data.nodes).ForEach(function (f) {
                var m = App.Node.store.getById('node', f);
                if (m && !m.get('isNew')) {
                    var promise = new Ember.RSVP.Promise(function (resolve, reject) {
                        var wfid = _this.get('workflowID');
                        var gid = m.id;
                        $.ajax({
                            url: '/flow/nodes',
                            type: 'DELETE',
                            data: { id: gid, workflows: wfid },
                            success: function (result) {
                                // Do something with the result
                                m.unloadRecord();
                                resolve(result);
                            },
                            error: function(jqXHR, textStatus, errorThrown) {
                                reject(jqXHR);
                            }
                        });
                    });
                    promises.push(promise);
                }
            });
            Ember.RSVP.allSettled(promises).then(function (array) {
                if (Enumerable.From(array).Any("f=>f.state=='rejected'"))
                    Messenger().post({ type: 'error', message: 'Error Updating Workflow Deletions' });
                else {
                    _this = _this;

                    var currentSelected = _this.get('selected');
                    var currentWorkflow = _this.get('workflowID');
                    var isDeleted = false;
                    // var deletedNode = _this.store.filter('node', { id: currentSelected });

                    // After the delete check all the remaing nodes and edges
                    var all = Enumerable.From(App.Node.store.all('node').content);
                    var promises1 = [];
                    var nodes = [];
                    all.ForEach(function (f) {
                        promises1.push(f.get('workflows').then(function (g) {
                            $.each(g.content, function (key, value) {
                                if (value.id === currentWorkflow)
                                    nodes.push(f);
                            });
                        }));
                    });


                    Ember.RSVP.allSettled(promises1).then(function () {
                        //debugger;
                        var selectedNodeAlive = Enumerable.From(nodes).Where('f=>f.id=="' + currentSelected.get('id') + '"').Any()
                        if (!selectedNodeAlive) { // current node selected, have to redirect
                            if (nodes.length >= 1) {  // if selected node was deleted transition to this node
                                _this.transitionToRoute('editor', nodes[0].id);
                            } else {
                                Messenger().post({ type: 'info', message: 'All steps in current workflow deleted. Redirecting to search.' });
                                _this.transitionToRoute('search');
                            }
                        } else {
                            _this.set('updateGraph', NewGUID()); // make graph rerender
                        }
                    });

                    Messenger().post({ type: 'success', message: 'Successfully Updated Workflow Deletions' });
                }


            }, function (error) {

            });

        }
    }
});

App.EditorView = Ember.View.extend({
    didInsertElement : function(){
        this._super();
        this.selectedContentChanges();
    },
    selectedContentChanges: function () {
        Ember.run.debounce(this, renderDynamic, 150, false);
    }.observes('controller.model.selected.content'),
});

var renderDynamic = function () {
    window.cleanAlternative(this.$());
    window.renderFunctions(this.$());
};

/* Step
-------------------------------------------------- */
App.HandlebarsLiveComponent = Ember.Component.extend({
  template: '',
  setupTemplate: function(){

    var template = this.get('templatestring');

    // Render template with Handelbar and set it to the current view
    this.set('template', Ember.Handlebars.compile(template));
    this.rerender(this); // this is necessary to make sure the dom actually gets updated :)

    // After rerender should initalise all the jQuery plugins
    Ember.run.scheduleOnce('afterRender', this, function(){
        //window.cleanAlternative(this.$());
        //window.renderFunctions(this.$());
        $('body').fitVids();

    });

  }.observes('templatestring').on('init')
});


// doesn't load pulled data yet
App.CompanySelectorComponent = Ember.Component.extend({
    template: "<div {{bind-attr id=internalID}} style='width: 275px;' class='select2' type='hidden'></div>",
    internalID: NewGUID(),
    multiple: true,
    value: '',
    setup: function(){
        var _this = this;
        Ember.run.scheduleOnce('afterRender', this, function(){
            var id = '#' + _this.get('internalID');

            var orgVal = _this.get('value'); 
            // need to preload old value here...
            $(id).val(orgVal)

            $(id).select2({
                placeholder: "Enter Companies...",
                minimumInputLength: 2,
                tags: _this.get('multiple'),
                //createSearchChoice : function (term) { return {id: term, text: term}; },  // thus is good if you want to use the type in item as an option too
                ajax: { // instead of writing the function to execute the request we use Select2's convenient helper
                    url: "/share/getcompanies",
                    dataType: 'json',
                    multiple: true,
                    data: function (term, page) {
                        return {id: term };
                    },
                    results: function (data, page) { // parse the results into the format expected by Select2.
                        if (data.length === 0) {
                            return { results: [] };
                        }
                        var results = Enumerable.From(data).Select("f=>{id:f.Value,tag:f.Text}").ToArray();
                        return { results: results, text: 'tag' };
                    }
                },
                // initSelection: function(element, callback) {
                //        // the input tag has a value attribute preloaded that points to a preselected repository's id
                //        // this function resolves that id attribute to an object that select2 can render
                //        // using its formatResult renderer - that way the repository name is shown preselected
                //        var id = $(element).val();
                //        if (id !== "") {
                //            $.ajax("https://api.github.com/repositories/" + id, {
                //                dataType: "json"
                //            }).done(function(data) { callback(data); });
                //        }
                // },
                formatResult: function(state) {return state.tag; },
                formatSelection: function (state) {return state.tag; },
                escapeMarkup: function (m) { return m; }
            }).on("change", function(e) { 
                _this.set('value', e.val); 
            });
        });
    }.on('didInsertElement')
})


// doesn't load pulled data yet
// Note: App.CompanySelectorComponent inherits from this :)
App.UserSelectorComponent = Ember.Component.extend({
    layout: Ember.Handlebars.compile("<div {{bind-attr id=internalID}} style='width: 275px;' class='select2' type='hidden'></div>"),
    url: "/share/getusernames",
    placeholder: "Enter Usernames...",
    internalID: NewGUID(),
    value: '',
    multiple: true,
    setup: function(){
        var _this = this;
        Ember.run.scheduleOnce('afterRender', this, function(){
            var id = '#' + _this.get('internalID');

            var settings = {
                placeholder: _this.get('placeholder'),
                minimumInputLength: 2,
                //createSearchChoice : function (term) { return {id: term, text: term}; },  // thus is good if you want to use the type in item as an option too
                ajax: { // instead of writing the function to execute the request we use Select2's convenient helper
                    url: _this.get('url'),
                    dataType: 'json',
                    multiple: _this.get('multiple'),
                    data: function (term, page) {
                        return {id: term };
                    },
                    results: function (data, page) { // parse the results into the format expected by Select2.
                        if (data.length === 0) {
                            return { results: [] };
                        }
                        var results = Enumerable.From(data).Select("f=>{id:f.Value,tag:f.Text}").ToArray(); // Use linq ;)
                        return { results: results, text: 'tag' };
                    }
                },
                formatResult: function(state) {return state.tag; },
                formatSelection: function (state) {return state.tag; },
                escapeMarkup: function (m) { return m; }
            };

            // Setup tags depeding if multiple is enabled :)
            if (_this.get('multiple')) {
                settings.tags = true;
            }

            var orgVal = _this.get('value'); 
            // need to preload old value here...

            // Setup Select2
            $(id).select2(settings).on("change", function(e) { 
                _this.set('value', e.val); 
            });
        });
    }.on('didInsertElement')
})

App.CompanySelectorComponent = App.UserSelectorComponent.extend({
    internalID: NewGUID(),
    placeholder: "Enter Companies...",
    url: "/share/getcompanies"
});

App.ContactSelectorComponent = App.UserSelectorComponent.extend({
    internalID: NewGUID(),
    placeholder: "Enter Companies...",
    url: "/share/getcontacts"
});

App.TriggerNodeComponent = Ember.Component.extend({
    defaultRow: {},
    tSmatchesRules: [{value: 'All'}, {value: 'Any'}],
    tStTriggerTypes: [{value: 'Email'}, {value: 'Zapier'}],
    tSvariables: [{value: 'Test'}, {value:'Awesome'}], // - this should be loaded from the variables on the current page context
    tSmatches: [{value: 'contains'}, {value:'does not contain'}, {value:'is'}, {value:'is not'}, {value:'begins with'}, {value:'ends with'}],
    graphID: '', // this is the edge ID on the item we are editing
    workflowID: '', // this is the workflow ID on the item we are editing
    edge: '',
    loading: true,
    setup: function(){
        
        // Get graph & Workflow ID
        var graphID = this.get('graphID');
        var workflowID = this.get('workflowID');

        var _this = this;

        this.set('loading', true);
  
        var store = this.get('targetObject.store');


        // TODO - Does the trigger API work?
        store.findQuery('trigger',{ GraphDataGroupID: workflowID, GraphDataID: graphID}).then(function(a) {
            return a
        }, function(){
            return [];
        }).then(function(a){
            _this.set('loading', false);


            if (a.get('length') == 1) {
                _this.set('config', JSON.parse(a.get('firstObject.JSON')))
            }
            
            if (a.get('length') < 1) {
                _this.set('config', _this.get('defaultConfig'));
            }

            if (a.get('length') > 1) {
                Messenger().post({ type: 'error', message: 'There should only be one edge condition. Please contact support!' });
            }
        })    


    }.observes('graphID', 'workflowID').on('didInsertElement'),
    config: {},
    defaultConfig: {
        matchSelect: 'All',
        triggerConditions: false,
        fields: [
            {        
                type: {
                    varLabel: '',
                    varSelect: '',
                    matchSelect: '',
                    matchInput: ''
                }
            }
        ],
        trigger: [
            {
                type: 'Email',
                email: {
                    sender: 'paul@flowpro.io',
                    messsage: 'Test message'
                }
            }
        ]
    },
    configEvaluation: function(config){
        var c = config;

        var s = ''; // this is the magic string later

        if (!c.triggerConditions)
          return s; // if the trigger condition is false just return nothing

        // Loop through each line in the condition array
        if (c.fields.length > 0) {
          $.each(c.fields, function(i, a){
    
            if (a.type != null) {
              b = a.type

              // Create the comparions part 
              var l = '';
              if (b.matchSelect == 'contains') {
                  l = '.match(/' + b.matchInput + '/ig) != null';
              }
              if (b.matchSelect == 'does not contain') {
                  l = '.match(/' + b.matchInput + '/ig) == null';
              }
              if (b.matchSelect == 'is') {
                  l = '.match(/^' + b.matchInput + '$/ig) != null';
              }
              if (b.matchSelect == 'is not') {
                  l = '.match(/^' + b.matchInput + '$/ig) == null';
              }
              if (b.matchSelect == 'begins with') {
                  l = '.match(/^' + b.matchInput + '/ig) != null';
              }
              if (b.matchSelect == 'ends with') {
                l = '.match(/' + b.matchInput + '$/ig) != null'; // "abc".match(/cdf$/ig) != null
              }


              var comparison = ''; // this will be either AND, OR (or empty for the last line ;)
              if (i != (c.fields.length - 1)) {
                comparison = (c.matchSelect == 'All' ? ' && ' : ' || ');
              }

              // Put it all together
              s += '( ' + '{{' + b.varSelect + '}}' + l  + ')' + comparison; 

            }
          });
        }

        return s
        


    },
    defaultConfigItem: {        
        type: {
            varLabel: '',
            varSelect: '',
            matchSelect: '',
            matchInput: ''
        }
    },
    actions: {
        'saveConditions': function(context){
            var _this = this;
            this.get('edge').get('EdgeConditions').then(function(a){
                
                 if (a.get('length') != 0) {
                    // edit a
                    z = a.get('firstObject');
                    z.set('JSON', JSON.stringify(_this.get('config')));

                    var conditionStr =  _this.get('configEvaluation')(_this.get('config'));
                    z.set('Condition', conditionStr);

                    z.save().then(function(){
                         Messenger().post({ type: 'success', message: 'Successfully saved edge conditions' });

                    }, function(){
                         Messenger().post({ type: 'error', message: 'Error saving edge conditions' });

                    });
                 } else {
                    // create a new one and save to it
                    var store = _this.get('targetObject.store');
                    var newCondition = store.createRecord('edgeCondition', {
                        ConditionID: NewGUID(),
                        GraphDataRelationID: _this.get('edgeID'),
                        JSON: JSON.stringify(_this.get('config')),// paul super easy array
                        Condition: _this.get('configEvaluation')(_this) // andy's js condition
                    })

                    _this.get('edge.EdgeConditions').addObject(newCondition);

                    // newCondition.save().then(function(){
                    //     return _this.get('edge').save()
                    // }).then(function(){
                    //      Messenger().post({ type: 'success', message: 'Successfully saved edge conditions' });
                    //     alert('Successful save!!!')
                    // })

                     newCondition.save().then(function(){
                         Messenger().post({ type: 'success', message: 'Successfully saved edge conditions' });
                    })

                 }
            });
        },
        'addRow': function (context) {
            var positionCurrent = this.get('config.fields').indexOf(context.itemInsertAfter) + 1;
            this.get('config.fields').insertAt(positionCurrent, JSON.parse(JSON.stringify(this.get('defaultConfigItem'))));

        },
        'deleteRow': function(context) {
            this.get('config.fields').removeObject(context.itemToDelete);
        },
        'addTriggerRow': function (context) {
            var positionCurrent = this.get('config.fields').indexOf(context.itemInsertAfter) + 1;
            this.get('config.fields').insertAt(positionCurrent, JSON.parse(JSON.stringify(this.get('defaultConfigItem'))));

        },
        'deleteTriggerRow': function(context) {
            this.get('config.fields').removeObject(context.itemToDelete);
        }                                                
    }
});



App.TriggerSetupComponent = Ember.Component.extend({
    defaultRow: {},
    tSmatchesRules: [{value: 'All'}, {value: 'Any'}],
    tSvariables: [{value: 'Test'}, {value:'Awesome'}], // - this should be loaded from the variables on the current page context
    tSmatches: [{value: 'contains'}, {value:'does not contain'}, {value:'is'}, {value:'is not'}, {value:'begins with'}, {value:'ends with'}],
    edgeID: '', // this is the edge ID on the item we are editing
    workflowID: '', // this is the workflow ID on the item we are editing
    edge: '',
    loading: true,
    setup: function(){
        var edgeID = this.get('edgeID');
        var workflowID = this.get('workflowID')
        var store = this.get('targetObject.store');

        var _this = this;

        // Load available variables
        var contextName = store.findQuery('contextName', {wfid: workflowID}).then(function(al){
            var test = Enumerable.From(al.content).Select('i=>{value:i.get("CommonName"), label:i.get("CommonName")}').Distinct().ToArray();
            _this.set('tSvariables', test);
        })

        // Setup the config - load values from the server

        this.set('loading', true);
        if (IsGUID(edgeID)){

            var edge = store.getById('edge', edgeID);
           
            this.set('edge', edge); 
            var conditions = edge.get('EdgeConditions').then(function(a){
                // here you need to set the this.config :) - 
                _this.set('loading', false);

                if (a.get('length') == 1) {
                //debugger;  

                    _this.set('config', JSON.parse(a.get('firstObject.JSON')))
                }
                if (a.get('length') < 1) {
                    _this.set('config', _this.get('defaultConfig'));
                }

                 if (a.get('length') > 1) {
                    Messenger().post({ type: 'error', message: 'There should only be one edge condition. Please contact support!' });
                    
                }

            })
         
                 
        }




    }.observes('edgeID').on('didInsertElement'),
    config: {},
    defaultConfig: {
        matchSelect: 'All',
        triggerConditions: false,
        fields: [
            {        
                type: {
                    varLabel: '',
                    varSelect: '',
                    matchSelect: '',
                    matchInput: ''
                }
            }
        ]
    },
    configEvaluation: function(config){
        var c = config;

        var s = ''; // this is the magic string later

        if (!c.triggerConditions)
          return s; // if the trigger condition is false just return nothing

        // Loop through each line in the condition array
        if (c.fields.length > 0) {
          $.each(c.fields, function(i, a){
    
            if (a.type != null) {
              b = a.type

              // Create the comparions part 
              var l = '';
              if (b.matchSelect == 'contains') {
                  l = '.match(/' + b.matchInput + '/ig) != null';
              }
              if (b.matchSelect == 'does not contain') {
                  l = '.match(/' + b.matchInput + '/ig) == null';
              }
              if (b.matchSelect == 'is') {
                  l = '.match(/^' + b.matchInput + '$/ig) != null';
              }
              if (b.matchSelect == 'is not') {
                  l = '.match(/^' + b.matchInput + '$/ig) == null';
              }
              if (b.matchSelect == 'begins with') {
                  l = '.match(/^' + b.matchInput + '/ig) != null';
              }
              if (b.matchSelect == 'ends with') {
                l = '.match(/' + b.matchInput + '$/ig) != null'; // "abc".match(/cdf$/ig) != null
              }


              var comparison = ''; // this will be either AND, OR (or empty for the last line ;)
              if (i != (c.fields.length - 1)) {
                comparison = (c.matchSelect == 'All' ? ' && ' : ' || ');
              }

              // Put it all together
              s += '( ' + '{{' + b.varSelect + '}}' + l  + ')' + comparison; 

            }
          });
        }

        return s
        


    },
    defaultConfigItem: {        
        type: {
            varLabel: '',
            varSelect: '',
            matchSelect: '',
            matchInput: ''
        }
    },
    actions: {
        'saveConditions': function(context){
            var _this = this;
            this.get('edge').get('EdgeConditions').then(function(a){
                
                 if (a.get('length') != 0) {
                    // edit a
                    z = a.get('firstObject');
                    z.set('JSON', JSON.stringify(_this.get('config')));

                    var conditionStr =  _this.get('configEvaluation')(_this.get('config'));
                    z.set('Condition', conditionStr);

                    z.save().then(function(){
                         Messenger().post({ type: 'success', message: 'Successfully saved edge conditions' });

                    }, function(){
                         Messenger().post({ type: 'error', message: 'Error saving edge conditions' });

                    });
                 } else {
                    // create a new one and save to it
                    var store = _this.get('targetObject.store');
                    var newCondition = store.createRecord('edgeCondition', {
                        ConditionID: NewGUID(),
                        GraphDataRelationID: _this.get('edgeID'),
                        JSON: JSON.stringify(_this.get('config')),// paul super easy array
                        Condition: _this.get('configEvaluation')(_this) // andy's js condition
                    })

                    _this.get('edge.EdgeConditions').addObject(newCondition);

                    // newCondition.save().then(function(){
                    //     return _this.get('edge').save()
                    // }).then(function(){
                    //      Messenger().post({ type: 'success', message: 'Successfully saved edge conditions' });
                    //     alert('Successful save!!!')
                    // })

                     newCondition.save().then(function(){
                         Messenger().post({ type: 'success', message: 'Successfully saved edge conditions' });
                    })

                 }
            });
        },
        'addRow': function (context) {
            var positionCurrent = this.get('config.fields').indexOf(context.itemInsertAfter) + 1;
            this.get('config.fields').insertAt(positionCurrent, JSON.parse(JSON.stringify(this.get('defaultConfigItem'))));

        },
        'deleteRow': function(context) {
            this.get('config.fields').removeObject(context.itemToDelete);
        }                                             
    }
});


App.StepRoute = Ember.Route.extend({
    queryParams: {
        workflowID: { refreshModel: true }  // this ensure that new data is loaded if another element is selected
    },
    steps: '',
    project: '',
    model: function (params, data) {
        var _this = this;
        return this.store.findQuery('step', { id: params.id, workflowID: params.workflowID, includeContent: true }).then(function(a){
                _this.set('steps', a);                
                return a.content[0].get('Project')
            }).then(function (b) {
                // debugger;
                _this.set('project', b)
                return b.get('ProjectData')
            }).then(function (c){
                // debugger;
                return {
                    steps: _this.get('steps'),
                    project: _this.get('project'),
                    data: c
                }
            });
    
    }
   
});
App.StepController = Ember.ObjectController.extend({
    queryParams: ['projectID', 'workflowID', 'nodeID', 'taskID'],
    needs: ['application'],
    context: {},
    html: Ember.computed.alias('model.steps.firstObject.content'), // Just in case we later change where the value is pulled from
    contextData: {},
    formtemplatestring: '',
    templatestring: function(){
        var _this = this;
        var temp = {};
        Enumerable.From(this.get('context')).ForEach(function (m) {
            if (m.Value.get('ProjectID') == _this.get('model.project.id'))
                temp[m.Key] = m.Value;
        });
        _this.set('context', temp);
        var template = this.get('html')
        // Create template from parsing content
        // #TODO

        // template = "{{lform-text}}" + template;


        // 1. Clean the JSON strings using the existing code
        // NOT FOR NOW

        var contextData = {};

        var templateString = '';
        // 2. Go through content and extract data JSON
        var projectData = Enumerable.From(this.store.all('projectDatum').content).Where("f=> f.get('ProjectID') === '" + _this.get('model.project.id') + "'");
        $template = $(template);
        $template.find('*').andSelf().filter('.tiny').each(function () {
            var $this = $(this)
            var type = $this.attr('type');
            if (type == "tiny-form") {

                var data = $this.data('json');
                var id = $this.attr('id');

                // var text = '{{#lform-wrapper}}';
                var text ='';


                $.each(data.fields, function (i, d) {
                    var oldValue = projectData.Where("f=>f.get('CommonName') === '" + d.label + "'").FirstOrDefault();
                    if (!oldValue)
                    {
                        var newRecord = _this.store.createRecord('projectDatum', {
                            CommonName: d.label,
                            ProjectID: _this.get('model.project.id'),
                            ProjectDataTemplateID: d.uid,
                            ProjectPlanTaskResponseID: _this.get('stepID'),
                            TemplateStructure: JSON.stringify(d),
                            Value: ''
                        })
                        oldValue = newRecord;
                    }
                    text += "{{lform-" + d.field_type + " s=contextData." + oldValue.id + " testvar=testVar}} <br>"

                    // ember data here
                    contextData[oldValue.id] = { d: d, record: oldValue}

                
                })
                
                // text += ' {{/lform-wrapper}}';

                // text += "This should be a varialbel : {{testVar}} {{id}}!!!!!"

                templateString += text;
                $this.data('json', '').html(text);


            }
        });
        var currentNames = Enumerable.From(contextData).Select("{cn : $.Value.d.label}");
        var pds = projectData.Select("{ cn: $.get('CommonName'), o: $}").Except(currentNames, "$.cn").Select("$.o").ForEach(function (m) {
                var mid = m.get('id');
                var ctx = { d: JSON.parse(m.get('TemplateStructure')), record: m };
                if (!ctx.d)
                    return;
                ctx.d.readOnly = true;
                contextData[mid] = ctx;
                templateString += "{{lform-" + ctx.d.field_type + " s=contextData." + mid + " testvar=testVar}} <br>"
        });

        Enumerable.From(contextData).ForEach(function (m) {
            var cn = m.Value.d.label;
            if (!cn)
                return;
            cn = cn.replace(/[ \'\"]/ig, "_");
            var co = _this.get('context.' + cn);
            if (co || cn.length < 1)
                return;
            _this.set('context.' + cn, m.Value.record)
        });

        // prepernd template string with other form on other pages


        this.set('formtemplatestring', templateString)
        this.set("contextData", contextData);

        // 3. Put the JSON into an array that can be binded to
        // {
        //     guid: {value: (value), settings: (settings to draw the form)},
        //     guid: {value: (value), settings: (settings to draw the form)}
        // }

        // 4. Do a text replace. Put the appropriate handlebars into the content string

        // 5. Move all the actions into components instead of having akward jquery plugin with heaps of dom scans



        //window.renderFunctions(this.$());

        // Convert jQuery object into plain html string
        template = $('<div>').html($template).html()


        return template;
    }.property('html'),
    sampleVarControllerLevel: '123-sampleVarControllerLevel',
    workflowID: null,
    projectID: null,
    stepID: function(){
        return this.get('model.steps.firstObject.id');
    }.property('model.steps.firstObject.id'),
    nodeID: null,
    taskID: null,
    lastEdited: function() {
        return moment(this.get('model.steps.firstObject.VersionUpdated')).fromNow();
    }.property('model.steps.firstObject.VersionUpdated'),
    actions: {
        nextStep: function(){

            var _this = this;

            var context = this.get('contextData');
            console.log(context);
            var promises = [];
            Enumerable.From(context).ForEach(function(d){
                
                var formData = d.Value.d;
                var formVal = d.Value.value;

                
                if (d.Value.record.get('isDirty')) {
                    promises.push(d.Value.record.save().then(function () {
                        Messenger().post({ type: 'success', message: 'Saved' });
                    }, function () {
                        Messenger().post({ type: 'error', message: 'Error' });
                    }));
                }
            });

            Ember.RSVP.allSettled(promises).then(function (p) {
                $.ajax({
                    url: "/flow/WebMethod/DoNext/" + _this.get('stepID'),
                    type: "GET"
                }).then(function (response) {
                    _this.store.findQuery('step', { id: _this.get('stepID') }).then(function (m) {
                        //_this.transitionToRoute('step', { id: _this.get('stepID') });
                        //Messenger().post({ type: 'success', message: 'Transitioned' });
                        window.scrollTo(0, 0);
                    });
                }, function (response) {
                    Messenger().post({ type: 'error', message: 'Error:' + response.statusText });
                });


            });



        }
    }
});


App.TodoRoute = Ember.Route.extend({
    model: function (params, data) {
        return Ember.RSVP.hash({
            steps: this.store.findQuery('step')
        });
    },
    afterModel: function (model) {
        //debugger;
    }

});

App.TodoController = Ember.ObjectController.extend({
    needs: ['application']
});


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

        if (workflows && workflows.length && workflows.length > 0) {
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
                VersionUpdated: a.VersionUpdated,
                edges: a.edges,
                workflows: a.workflows
            };
        });

        payload = { "Nodes": nodes, "Edges": edges, "Workflows": workflows };

        //if (!edges || (edges.length === 0 && nodes.length === 1)) {
        //    delete payload.Edges;
        //}
        //We want to exclude updates where content exists and new entries are null
        var stock = Enumerable.From(payload.Nodes);

        var produce = Enumerable.From(store.all('node').content).Where("f=>f.get('content') != null");
        var exclude = stock.Where("f=>f.content==null").Intersect(produce, "$.id");
        payload.Nodes = stock.Except(produce, "$.id").ToArray();

        return this._super(store, type, payload, id, requestType);
    }
});




var refetchLocale = function (context) {
    if (!context.get('isNew')) {
        context.set('_localePromise', context.store.find('translation', { docid: context.get('id'), TranslationCulture: App.get('locale.l'), DocType: context.get('constructor.typeKey') })
        .then(function (m) {
            context.set('_localName', m.content[0].get('TranslationName'));
            context.set('_localContent', m.content[0].get('TranslationText'));
        }));

    }
};

DS.Model.reopen({
    _recordCreated: Date.now(),
    _localeTrigger: false,
    _localePromise : null,
    _locale: function () {
        var _this = this;
        var updateLocale = function () {
            var locale = App.get('locale.l');
            if (defaultLocale !== locale) {
                var translations = Enumerable.From(_this.store.all('translation').content).Where("f=>f.get('DocID')==='" + _this.get('id') + "' && f.get('TranslationCulture') === '" + App.get('locale.l') + "'").ToArray();
                if (translations.get('length') > 0) {
                    var tx = translations.objectAt(0);
                    if (!tx.get('Translation') || _this.get('_recordCreated') + 15000 < +Date.now())
                        Ember.run.debounce(_this, refetchLocale, _this, 150, true); // get record again if text is null or old, helps with partially loaded records
                    else {
                        _this.set('_localName', tx.get('TranslationName'));
                        _this.set('_localContent', tx.get('TranslationText'));
                    }
                }
                else
                    Ember.run.debounce(_this, refetchLocale, _this, 150, true);
            }
            else {
                _this.set('_localePromise', new Promise(function (resolve, reject) {
                    _this.set('_localName', _this.get('humanName'));
                    _this.set('_localContent', _this.get('humanContent'));
                    resolve(value);
                }));
            }

        };


        if (!this.get('_localeTrigger')) {
            this.set('_localeTrigger', true);
            App.get('locale').addObserver('l', defaultLocale, function () {
                Ember.run.debounce(_this, updateLocale, _this, 150, true);
            });
        }
        Ember.run.debounce(_this, updateLocale, _this, 150, true);

    }.property('humanName', 'humanContent', 'label', 'content', 'name'),
    _localName: '...',
    _localContent: '...',
    localName: function (key, value, previousValue) {
        Ember.run.scheduleOnce('sync', this, this.get, '_locale');
        return this.get('_localName');
    }.property('_localName', 'label', 'name', 'humanName'),
    localContent: function (key, value, previousValue) {
        Ember.run.scheduleOnce('sync', this, this.get, '_locale');
        return this.get('_localContent');
    }.property('_localContent', 'humanContent', 'content'),
    Error: DS.attr('string', { defaultValue: null }),
    Status: DS.attr('string', { defaultValue: null })
});


App.Node = DS.Model.extend({
    international: true,
    label: DS.attr('string'),
    content: DS.attr('string'),
    edges: DS.hasMany('edge', { async: true }),
    workflows: DS.hasMany('workflow', { async: true }),
    shape: function() {
        return 'ellipse'; // can also us circle
    }.property(),
    _group : null,
    group: function () {
        return Enumerable.From(this._data.edges).Select("f=>f.get('GroupID')").Distinct().ToArray().toString(); // any string, will be grouped - random color
    }.property(),
    humanName: function () {
        var temp = this.get('label');
        if (temp)
            return ToTitleCase(temp.replace(/_/g, ' '));
        else
            return '';
    }.property('label'),
    humanContent: function () {
        return this.get('content');
    }.property('content'),
    VersionUpdated: DS.attr('')
});

App.Task = DS.Model.extend({
    TaskID : DS.attr('string'),
    TaskName : DS.attr('string'),
    WorkTypeID : DS.attr('string'), 
    WorkCompanyID : DS.attr('string'),
    WorkContactID : DS.attr('string'),
    GraphDataGroupID : DS.attr('string'), 
    GraphDataID : DS.attr('string'), 
    DefaultPriority : DS.attr('string'), 
    EstimatedDuration : DS.attr('string'), 
    EstimatedLabourCosts : DS.attr('string'), 
    EstimatedCapitalCosts : DS.attr('string'),
    EstimatedValue : DS.attr('string'), 
    EstimatedIntagibleValue: DS.attr('string'),
    EstimatedRevenue: DS.attr('string'),
    PerformanceMetricParameterID : DS.attr('string'),
    PerformanceMetricQuantity : DS.attr('string'),
    Comment: DS.attr('string')
});

App.Trigger = DS.Model.extend({
    TriggerID: DS.attr('string'),
    CommonName: DS.attr('string'),
    TriggerType: DS.attr('string'),
    JsonMethod: DS.attr('string'), //
    JsonProxyApplicationID: DS.attr('string'),
    JsonProxyContactID: DS.attr('string'),
    JsonProxyCompanyID: DS.attr('string'),
    JsonAuthorizedBy: DS.attr('string'),
    JsonUsername: DS.attr('string'),
    JsonPassword: DS.attr('string'),
    JsonPasswordType: DS.attr('string'),
    JSON: DS.attr('string'), //
    SystemMethod: DS.attr('string'),
    ConditionID: DS.attr('string'), //
    ExternalUrl: DS.attr('string'), //http://dothis/rest/url
    ExternalRequestMethod: DS.attr('string', { defaultValue: 'GET' }),
    ExternalFormType: DS.attr('string', { defaultValue: 'JSON' }), 
    PassThrough: DS.attr('string', { defaultValue: false }), 
    condition: DS.belongsTo('condition', { async: true }),
});



App.GraphDataTrigger = App.Trigger.extend({
    GraphDataTriggerID: DS.attr('string'),
    GraphDataID: DS.attr('string'),
    TriggerID: DS.attr('string'),
    OnEnter: DS.attr('string'),
    OnDataUpdate: DS.attr('string'),
    OnExit: DS.attr('string'),
    RunOnce: DS.attr('string')
});




App.Step = App.Node.extend({
    TaskName: DS.attr('string'),
    WorkTypeID: DS.attr('string'),
    WorkCompanyID: DS.attr('string'),
    WorkContactID: DS.attr('string'),
    GraphDataGroupID: DS.attr('string'),
    GraphDataID: DS.attr('string'),
    ProjectID: DS.attr('string'),
    ProjectPlanTaskID: DS.attr('string'),
    ResponsibleCompanyID: DS.attr('string'),
    ResponsibleContactID: DS.attr('string'),
    ActualTaskID: DS.attr('string'),
    ActualWorkTypeID: DS.attr('string'),
    ActualGraphDataGroupID: DS.attr('string'),
    ActualGraphDataID: DS.attr('string'),
    Began: DS.attr('string'),
    Completed: DS.attr('string'),
    Hours: DS.attr('string'),
    EstimatedProRataUnits: DS.attr('string'),
    EstimatedProRataCost: DS.attr('string'),
    EstimatedValue: DS.attr('string'),
    EstimatedDuration: DS.attr('string'),
    EstimatedDurationUnitID: DS.attr('string', { defaultValue: '0D542D4C-DACE-4702-83B0-3C9BA85D4183' }), //hours
    EstimatedLabourCosts: DS.attr('string'),
    EstimatedCapitalCosts: DS.attr('string'),
    DefaultPriority: DS.attr('string'),
    PerformanceMetricParameterID: DS.attr('string'),
    PerformanceMetricQuantity: DS.attr('string'),
    PerformanceMetricContributedPercent: DS.attr('string'),
    ApprovedProRataUnits: DS.attr('string'),
    ApprovedProRataCost: DS.attr('string'),
    Approved: DS.attr('string'),
    ApprovedBy: DS.attr('string'),
    Comments: DS.attr('string'),
    PreviousStepID: DS.attr('string'),
    NextStepID: DS.attr('string'),
    Project: DS.belongsTo('project', { async: true }),
    Row :  DS.attr('string'),
    TotalRows :  DS.attr('string'),
    Score :  DS.attr('string'),
    ProjectName :  DS.attr('string'),
    ProjectCode :  DS.attr('string'),
    GraphDataGroupName :  DS.attr('string'),
    GraphName :  DS.attr('string'),
    GraphContent :  DS.attr('string'),
    LastEditedBy: DS.attr('string'),
    projectCode: function () {
        return this.get('ProjectCode');
    }.property('ProjectCode'),
    humanName: function () {
    var temp = this.get('GraphName');
    if (temp)
        return ToTitleCase(temp.replace(/_/g, ' '));
    else
        return '';
}.property('GraphName'),
});

App.Project = DS.Model.extend({
    ProjectName: DS.attr('string', { defaultValue: null }),
    ProjectCode: DS.attr('string', { defaultValue: null }),
    ClientCompanyID: DS.attr('string', { defaultValue: null }),
    ClientContactID: DS.attr('string', { defaultValue: null }),
    ProjectData: DS.hasMany('projectDatum', { async: true }),
    Steps: DS.hasMany('step', { async: true }),
});

App.ProjectDataTemplate = DS.Model.extend({
    CommonName: DS.attr('string', { defaultValue: null }),
    UniqueID: DS.attr('string', { defaultValue: null }),
    UniqueIDSystemDataType: DS.attr('string', { defaultValue: null }),
    TemplateStructure: DS.attr('string', { defaultValue: null }),
    TemplateStructureChecksum: DS.attr('string', { defaultValue: null }),
    TemplateActions: DS.attr('string', { defaultValue: null }),
    TemplateType: DS.attr('string', { defaultValue: null }),
    TemplateMulti: DS.attr('string', { defaultValue: null }),
    TemplateSingle: DS.attr('string', { defaultValue: null }),
    TableType: DS.attr('string', { defaultValue: null }),
    ReferenceID: DS.attr('string', { defaultValue: null }),
    UserDataType: DS.attr('string', { defaultValue: null }),
    SystemDataType: DS.attr('string', { defaultValue: null }),
    IsReadOnly: DS.attr('string', { defaultValue: null }),
    IsVisible: DS.attr('string', { defaultValue: null }),
    ProjectDataTemplateID: DS.attr('string', { defaultValue: null })
});

App.ProjectDatum = App.ProjectDataTemplate.extend({
    Label: '',
    Options: null,
    Type: 'text',
    ProjectID: DS.attr('string', { defaultValue: null }),
    ProjectPlanTaskResponseID: DS.attr('string', { defaultValue: null }),
    Value: DS.attr('string', { defaultValue: null })
})

App.Edge = DS.Model.extend({
    from: DS.attr(),
    to: DS.attr(),
    style: function(){
        return 'arrow'; // Type available ['arrow','dash-line','arrow-center']
    }.property(),
    widthSelectionMultiplier: function(){
        return 5;
    },
    width: function(){
        return 1;
    }.property(),
    color: function () {
        var rt = this.get('RelationTypeID');
        if (rt && rt.toLowerCase() == 'bc4f2c1f-e25e-4849-a7f9-878c41aa6847')
            return {color: 'green', highlight: 'lime'};
        else if (rt && rt.toLowerCase() == '4d6a4003-2dd7-404e-a3cc-8d96d8237aa7')
            return {color: 'red', highlight: 'salmon'};
        else
            return {color: 'grey', highlight: 'black'};
    }.property(),
    origin: function () {
        return this.get('from').replace("'", "\\\'");
    }.property(),
    destination: function () {
        return this.get('to').replace("'", "\\\'");
    }.property(),
    GroupID: DS.attr(),
    groupName: function () {
        var temp = App.Workflow.store.getById('workflow', this.get('GroupID'))
        if (temp)
            return temp.get('shortName');
        else
            return null;
    }.property(),
    Related: DS.attr(''),
    RelationTypeID: DS.attr(),
    Weight: DS.attr(),
    Sequence: DS.attr(),
    EdgeConditions: DS.hasMany('edgeCondition', { async: true }),
});

App.Condition = DS.Model.extend({
    OverrideProjectDataWithJsonCustomVars: DS.attr(''),
    Condition: DS.attr(''),
    JSON: DS.attr('')
})

App.EdgeCondition = App.Condition.extend({
    Grouping: DS.attr(''),
    Sequence: DS.attr(''),
    JoinedBy: DS.attr(''),
    ConditionID: DS.attr(''),
    GraphDataRelationID: DS.attr('')
});


App.Workflow = DS.Model.extend({
    name: DS.attr('string'),
    comment: DS.attr('string'),
    firstNode: DS.attr('string'),
    StartGraphDataID: DS.attr('string'),
    humanName: function () {
        var temp = this.get('name');
        if (temp)
            return ToTitleCase(temp.replace(/_/g, ' '));
        else
            return null;
    }.property('name'),
    shortName: function () {
        var temp = this.get('name');
        if (temp) {
            if (temp.length > 17)
                return ToTitleCase(temp.substring(0, 17).replace(/_/g, ' ')) + '...';
            else
                return ToTitleCase(temp.replace(/_/g, ' '));
        }
        else
            return null;
    }.property('name')
});

App.MyWorkflow = App.Search.extend({});
App.MyNode = App.Search.extend({});
App.MyFile = App.Search.extend({});
App.File = App.Search.extend({});
App.Location = App.Search.extend({});
App.Context = App.Search.extend({});
App.WorkType = App.Search.extend({});


App.MySecurityList = DS.Model.extend({
    SecurityTypeID: DS.attr(''),
    SecurityType: DS.attr(''),
    security: DS.attr(''),
    OwnerUserID: DS.attr(''),
    AccessorUserID: DS.attr(''),
    OwnerContactID: DS.attr(''),
    OwnerCompanyID: DS.attr(''),
    OwnerTableType: DS.attr(''),
    OwnerReferenceID: DS.attr(''),
    ReferenceID: DS.attr(''),
    ReferenceName: DS.attr(''),
    AccessorCompanyID: DS.attr(''),
    AccessorCompanyName: DS.attr(''),
    AccessorContactID: DS.attr(''),
    AccessorContactName: DS.attr(''),
    AccessorRoleID: DS.attr(''),
    AccessorRoleName: DS.attr(''),
    AccessorProjectID: DS.attr(''),
    AccessorProjectName: DS.attr(''),
    Updated: DS.attr(''),
    CanCreate: DS.attr(''),
    CanRead: DS.attr(''),
    CanUpdate: DS.attr(''),
    CanDelete: DS.attr(''),
    humanName: function () {
        var temp = this.get('ReferenceName');
        if (temp)
            return ToTitleCase(temp.replace(/_/g, ' '));
        else
            return null;
    }.property('ReferenceName')
});

//  Don't need these
// App.MyWhiteList = App.MySecurityList.extend({});
// App.MyBlackList = App.MySecurityList.extend({});

App.Myinfo = DS.Model.extend({
    Companies: DS.attr(''),
    ContactName: DS.attr(''),
    CurrentCompany: DS.attr(''),
    CurrentCompanyID: DS.attr(''),
    IsPartner: DS.attr(''),
    IsSubscriber: DS.attr(''),
    Licenses: DS.attr(''),
    Roles: DS.attr(''),
    UserID: DS.attr(''),
    UserName: DS.attr('')
});

App.ContextName = DS.Model.extend({
    FormID: DS.attr(''),
    GraphDataID: DS.attr(''),
    CommonName: DS.attr('')
})

App.MyLicense = DS.Model.extend({
    LicenseID: DS.attr(''),
    CompanyID: DS.attr(''),
    ContactID: DS.attr(''),
    LicenseeGUID: DS.attr(''),
    LicenseeName: DS.attr(''),
    LicenseeUsername: DS.attr(''),
    LicenseeUniqueMachineCode1: DS.attr(''),
    LicenseeUniqueMachineCode2: DS.attr(''),
    LicenseeGroupID: DS.attr(''),
    LicensorIP: DS.attr(''),
    LicensorName: DS.attr(''),
    LicenseTypeID: DS.attr(''),
    LicenseType: DS.attr(''),
    LicenseURL: DS.attr(''),
    RootServerName: DS.attr(''),
    RootServerID: DS.attr(''),
    ServerName: DS.attr(''),
    ServerID: DS.attr(''),
    "LicenseeUsernameTags": function(){
        var a = this.get('LicenseeUsername')
        if (a) {
            return '<span class="label label-success">' + a + '</span>'
        }
        return ''
    }.property('LicenseeUsername'),
    ApplicationID: DS.attr(''),
    ServiceAuthenticationMethod: DS.attr(''),
    ServiceAuthorisationMethod: DS.attr(''),
    ValidFrom: DS.attr(''),
    ValidFromNice: function(){
        var a = this.get('ValidFrom');
        return moment(a).format('DD/MM/YYYY');
    }.property('ValidFrom'),
    Expiry: DS.attr(''),
    ExpiryNice: function(){
        var a = this.get('Expiry');
        return moment(a).format('DD/MM/YYYY');
    }.property('Expiry'),
    SupportExpiry: DS.attr(''),
    ValidForDuration: DS.attr(''),
    ValidForUnitID: DS.attr(''),
    ValidForUnitName: DS.attr(''),
    ProRataCost: DS.attr(''),
    ModelID: DS.attr(''),
    ModelName: DS.attr(''),
    ModelRestrictions: DS.attr(''),
    ModelPartID: DS.attr(''),
    PartName: DS.attr(''),
    PartRestrictions: DS.attr(''),
    AssetID: DS.attr(''),
    IsExpired: function () {
        var d = new Date(this.get('Expiry'));
        if (d < new Date())
            return true;
        else
            return false;
    }.property()
});

App.MyProfile = DS.Model.extend({
    ContactID: DS.attr(''),
    ContactName: DS.attr(''),
    Title: DS.attr(''),
    Surname: DS.attr(''),
    Firstname: DS.attr(''),
    Username: DS.attr(''),
    OldPassword: DS.attr(''),
    Password: DS.attr(''),
    Hash: DS.attr(''),
    DefaultEmail: DS.attr(''),
    DefaultEmailValidated: DS.attr(''),
    DefaultMobile: DS.attr(''),
    DefaultMobileValidated: DS.attr(''),
    MiddleNames: DS.attr(''),
    Initials: DS.attr(''),
    DOB: DS.attr(''),
    BirthCountryID: DS.attr(''),
    BirthCity: DS.attr(''),
    AspNetUserID: DS.attr(''),
    XafUserID: DS.attr(''),
    OAuthID: DS.attr(''),
    Photo: DS.attr(''),
    ShortBiography: DS.attr(''),
    AddressID: DS.attr(''),
    AddressTypeID: DS.attr(''),
    AddressName: DS.attr(''),
    Sequence: DS.attr(''),
    Street: DS.attr(''),
    Extended: DS.attr(''),
    City: DS.attr(''),
    State: DS.attr(''),
    Country: DS.attr(''),
    Postcode: DS.attr(''),
    IsHQ: DS.attr(''),
    IsPostBox: DS.attr(''),
    IsBusiness: DS.attr(''),
    IsHome: DS.attr(''),
    Phone: DS.attr(''),
    Fax: DS.attr(''),
    Email: DS.attr(''),
    Mobile: DS.attr(''),
    LocationID: DS.attr(''),
    Thumb: function () {
        return "/share/photo/" + this.get('AspNetUserID');
    }.property('AspNetUserID')
});

App.Translation = DS.Model.extend({
    DocType: DS.attr(''),
    DocID: DS.attr(''),
    DocName: DS.attr(''),
    TranslationCulture: DS.attr(''),
    TranslationName: DS.attr(''),
    TranslationText: DS.attr(''),
    VersionUpdated: DS.attr(''),
    DocUpdated: DS.attr('')
});


App.Locale = DS.Model.extend({
    TranslationCulture: DS.attr(''), // de
    Label: DS.attr(''), //search.info.no_keyword  - (where,type.what)
    OriginalText: DS.attr(''), // “Please add a keyword to start searching..."
    OriginalCulture: DS.attr(''),  // en - always english for now, but just hardcoded for now
    Translation: DS.attr(''), //  “Bitten ein Suchwort"
    VersionUpdated: DS.attr('') // version number
});



App.Wikipedia = DS.Model.extend({
    label: DS.attr('string'),
    content: DS.attr('string'),
    edges: DS.hasMany('edge'),
    group: function() {
        return 'wikipedia';
    }.property(),
    humanName: function () {
        var temp = this.get('label');
        if (temp)
            return ToTitleCase(temp.replace(/_/g, ' '));
        else
            return null;
    }.property(),
    dataName: function () {
        return this.get('id').replace(/'/g, '\\\'');
    }.property('label'),
    localName: function () {
        return this.get('humanName');
    }.property()
});



/* Wikipedia
-------------------------------------------------- */
App.WikipediaRoute = Ember.Route.extend({
    lastTransition : null,
    model: function (params) {
        if (typeof params !== 'undefined' && params.id && params.id.indexOf(' ') > 0) {
            this.transitionTo('wikipedia', params.id.replace(/ /ig, "_"));
            return null;
        }
        //console.log(params.id);
        return Ember.RSVP.hash({
            graphData: this.store.findQuery('wikipedia', params.id),
            selected: params.id,
            content: '',
            title: ((typeof params.id === 'string' && params.id !== null && params.id.length > 0) ? decodeURIComponent(params.id).replace(/_/ig, " ") : params.id),
            encodedTitle: encodeURIComponent(params.id.replace(/ /ig, "_")),
            humanName: ((typeof params.id === 'string' && params.id !== null && params.id.length > 0) ? ToTitleCase(decodeURIComponent(params.id).replace(/_/ig, " ")) : params.id)
        });
    },
    afterModel: function (m) {
        var sel = m.selected;
        var array = { nodes: [], edges: [] };
        var depthMax = 1; // currently depthMax is limited to 1 unless the data is already in ember store
        var nodeMax = 25;
        var data = recurseGraphData(sel, array, this, 1, depthMax, nodeMax, 'wikipedia');
        data.origin = 'wikipedia';
        m.graphData = data;
        var article = this.store.getById('wikipedia', sel);
        if (article)
            m.content = article.get('content');
    },
    actions: {
        // then this hook will be fired with the error and most importantly a Transition
        // object which you can use to retry the transition after you handled the error
        error: function (error, transition) {
            if (error && error.id && error.redirect)
                this.transitionTo(error.redirect, error.id);
        },
        willTransition: function (transition) {
            //this.set('controller.model.title', ''); //Doesnt work
            //$(".filteredData")    .html(''); //HACK ?
            //this.model.rollback();
            //var model = this.modelFor('wikipedia');
            //var controller = this.controllerFor('wikipedia');
            //var route = this;
            //var newModel = route.model();
            //controller.set('model', newModel);
            //$(".filteredData").remove();

            var _this = this;
            var cleanWikipedia = function () {
                Enumerable.From(_this.store.all('edge').content).Where("f=>typeof f.get('GroupID') === 'undefined'").ForEach(function (data) {
                    _this.store.unloadRecord(data);
                });
                Enumerable.From(_this.store.all('wikipedia').content).ForEach(function (data) {
                    _this.store.unloadRecord(data);
                });
            };
            if (transition.targetName !== 'wikipedia') {
                Ember.run.later(_this, cleanWikipedia, 1);
            } else {
                if (transition.params.wikipedia.id.match(/.*_$/) !== null)
                    transition.abort();
                else if (this.get('lastTransition') !== transition.params.wikipedia.id) {
                    transition.abort();
                    this.set('lastTransition', transition.params.wikipedia.id);
                    Ember.run.debounce(this, this.transitionTo, 'wikipedia', transition.params.wikipedia.id, 1300, false);

                }
                else {
                    var duplicate = Enumerable.From(App.Wikipedia.store.all('wikipedia').content).Where("f=>f.id.toLowerCase()=='" + transition.params.wikipedia.id + "'.toLowerCase()").FirstOrDefault();
                    if (duplicate && transition.params.wikipedia.id !== duplicate.id) {
                        transition.abort();
                        this.replaceWith('wikipedia', duplicate.id);
                    }
                }
            }
        }
    }
});


App.WikipediaController = Ember.ObjectController.extend({
    needs: ['application'],
    changeSelected: function () {
        //console.log('Selection changed, should redirect!')
        this.transitionToRoute('wikipedia', this.get('model.selected'));
    }.observes('model.selected'),
    watchSearch: function () {

        var title = encodeURIComponent(this.get('model.title').replace(/ /ig, "_"));
        if (encodeURIComponent(this.get('selected').replace(/ /ig, "_")) !== title || title !== this.get('model.encodedTitle')) {
            this.transitionToRoute('wikipedia', title);
        }
    }.observes('model.title'),
    actions: {
        showDemo: function () {
            _this = this;
            Enumerable.From(_this.store.all('edge').content).Where("f=>typeof f.get('GroupID') === 'undefined'").ForEach(function (data) {
                _this.store.unloadRecord(data);
            });
            Enumerable.From(_this.store.all('wikipedia').content).ForEach(function (data) {
                _this.store.unloadRecord(data);
            });



            var updateWikiSearch = function (text) {
                $("#wikiSearch").blur();
                _this.set('model.title', text);
                setTimeout(function () { $("#wikiSearch").select(); }, 250);
            }

            Ember.run.later(_this, function () { Messenger().post({ type: 'success', message: "Let's compare cats with dogs.<br/><br/>1. Type in cat.", id: 'wiki-help', hideAfter: 6 }) }, 0);

            Ember.run.later(_this, updateWikiSearch, 'c', 400);
            Ember.run.later(_this, updateWikiSearch, 'ca', 800);
            Ember.run.later(_this, updateWikiSearch, 'cat', 1200);

            Ember.run.later(_this, function () { Messenger().post({ type: 'success', message: "2. Now type in dog.", id: 'wiki-help' }) }, 6500);

            Ember.run.later(_this, updateWikiSearch, 'd', 9000);
            Ember.run.later(_this, updateWikiSearch, 'do', 9400);
            Ember.run.later(_this, updateWikiSearch, 'dog', 9800);

            Ember.run.later(_this, function () {
                Messenger().post({ type: 'success', message: "The colors show how the topics connect. <br/><br/>Red: connected<br/>White: start<br/>Black: unexplored<br/>Blue: explored", id: 'wiki-help', hideAfter: 20 });
                Ember.run.later(_this, function () {
                    $('html, body').animate({
                        scrollTop: $(".network-frame").offset().top
                    }, 400);
                }, 8000)}
                ,'dog'
                , 11000);


        }
    }
});

App.WikipediaAdapter = DS.Adapter.extend({
    find: function (store, type, id) {
        return this.findMany(store, type, id);
    },
    findMany: function (store, type, ids) {
        return this.findQuery(store, type, ids);
    },
    findQuery: function (store, type, query, array) {
        var _this = this;
        var context = this;
        var id = query;
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
        if (id == 'Special:Random') {
            return new Ember.RSVP.Promise(function (resolve, reject) {
                var randomURL = 'http://en.wikipedia.org/w/api.php?format=json&action=query&list=random&prop=revisions&rvprop=content&rnnamespace=0';
                jQuery.getJSON("https://query.yahooapis.com/v1/public/yql?" +
                   "q=select%20content%20from%20data.headers%20where%20url%3D%22" +
                   encodeURIComponent(randomURL) +
                   "%22&format=json&diagnostics=true&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys&callback=?"
                 ).then(function (data) {
                     var title;
                     if (data.query.results.resources.content.query)
                         title = data.query.results.resources.content.query.random.title;
                     else
                         title = data.query.results.resources.content.json.query.random.title;
                    Ember.run(null, reject, { redirect: 'wikipedia', id: title });
                     //Ember.run(null, reject, { replaceWith: 'wikipedia', id: data.query.results.resources.content.query.random.title });
                     // _this.replaceWith('wikipedia', data.query.results.resources.content.query.random.title)
                 }, function (jqXHR) {
                     jqXHR.then = null; // tame jQuery's ill mannered promises
                     Ember.run(null, reject, jqXHR);
                 });
            });
        }
        else
            return new Ember.RSVP.Promise(function (resolve, reject) {
                var processContent = function () {
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
                                edges.push({ id: id + '-' + leaf, from: id, to: leaf.replace(/ /g, '_') });
                            }
                        });
                    }
                    edges = Enumerable.From(edges)
                        .Where("$.to.search(/^(file|image|category):.*/i)!==0")
                        .GroupBy("$.id", "", "key,e=>{id: key, from: e.source[0].from, to: e.source[0].to}")
                        .ToArray();
                    var edgeids = Enumerable.From(edges).Select("$.id").ToArray();
                    var sequence = 1;
                    Enumerable.From(edges).ForEach(function (f) { f.sequence = sequence; sequence++; App.Wikipedia.store.push('edge', f); });
                    Enumerable.From(edges).Where("$.to!='" + id.replace("'", "\\\'") + "'").ForEach(function (f) { App.Wikipedia.store.push('wikipedia', { id: f.to, label: f.to }); });
                    App.Wikipedia.store.push('wikipedia', { id: id, label: id, edges: edgeids, content: content });
                    setTimeout(function () {
                        if (typeof array === 'undefined')
                            Ember.run(null, resolve, { id: id, label: id, content: content, edges: edgeids });
                        else {
                            var toReturn = { Nodes: [{ id: id, label: id, content: content, edges: edgeids }], Edges: edges };
                            Ember.run(null, resolve, toReturn);
                        }
                    }, 300);
                };
            var url = 'http://en.wikipedia.org/w/api.php?format=json&action=query&titles=' + encodeURIComponent(id) + '&prop=revisions&rvprop=content';
            jQuery.getJSON("https://query.yahooapis.com/v1/public/yql?" +
                "q=select%20content%20from%20data.headers%20where%20url%3D%22" +
                encodeURIComponent(url) +
                "%22&format=json&diagnostics=true&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys&callback=?"
              ).then(function (data) {
                  $.each(data, function (key, val) {
                      recurse(key, val, id);
                      if (html)
                          return false;
                  });

                  if (!html) {
                      url = 'http://en.wikipedia.org/w/api.php?format=json&action=opensearch&search=' + encodeURIComponent(id) + '&limit=1';
                      jQuery.getJSON("https://query.yahooapis.com/v1/public/yql?" +
                        "q=select%20content%20from%20data.headers%20where%20url%3D%22" +
                        encodeURIComponent(url) +
                        "%22&format=json&diagnostics=true&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys&callback=?"
                      ).then(function (result) {
                          if (result.query.count == 1 && result.query.results.resources.content.json.json[1]) {
                              var sr = result.query.results.resources.content.json.json[1].json;
                              sr = sr.replace(/ /ig, '_');

                              document.location.hash = '#/wikipedia/' + sr;
                              Ember.run(null, reject, result);

                              //url = 'http://en.wikipedia.org/w/api.php?format=json&action=query&titles=' + encodeURIComponent(sr) + '&prop=revisions&rvprop=content';
                              //jQuery.getJSON("https://query.yahooapis.com/v1/public/yql?" +
                              //"q=select%20content%20from%20data.headers%20where%20url%3D%22" +
                              //encodeURIComponent(url) +
                              //"%22&format=json&diagnostics=true&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys&callback=?"
                              //).then(function (srData) {
                              //    $.each(srData, function (key, val) {
                              //        recurse(key, val, id);
                              //        if (html)
                              //            return false;
                              //    });
                              //    processContent();
                              //}, function (jqXHR) {
                              //    jqXHR.then = null; // tame jQuery's ill mannered promises
                              //    Ember.run(null, reject, jqXHR);
                              //})

                          }
                          else {
                              processContent();
                          }
                      }, function (jqXHR) {
                          jqXHR.then = null; // tame jQuery's ill mannered promises
                          Ember.run(null, reject, jqXHR);
                      });

                  }
                  else {
                      processContent();
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
    // [... add as needed ...]
    data = data.replace(/^null$/, '');
    data = data.replace(/<pre><\/pre>/ig, '');
    data = data.replace(/\(\)/, '');
    //Fix Vids
    //data = $(data).fitVids().prop('outerHTML');
    var tags = ['em', 'div', 'p', 'span']; //Attempt to fix eufeeds
    for (var i = 0; i < tags.length; i++) {
        var rxOpen = new RegExp("<"+ tags[i], "ig");
        var rxClose = new RegExp("<\/" + tags[i], "ig");
        divOpen = data.match(rxOpen);
        divClose = data.match(rxClose);
        if (!divOpen)
            divOpen = 0;
        else
            divOpen = divOpen.length;
        if (!divClose)
            divClose = 0;
        else
            divClose = divClose.length;
        for (; divOpen < divClose; divOpen++) {
            data = "<"+ tags[i] +">" + data;
        }
        for (; divClose < divOpen; divClose++) {
            data = data + "</"+ tags[i] +">";
        }
    }


    return '<div class=\'filteredData\'>' + data + '</div>';
}

Ember.Handlebars.registerHelper('eachProperty', function (context, options) {
    var ret = "";
    var newContext = Ember.get(this, context);
    for (var prop in newContext) {
        if (newContext.hasOwnProperty(prop)) {
            ret = ret + options.fn({ property: prop, value: newContext[prop] });
        }
    }
    return ret;
});

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


Ember.Handlebars.helper('prettyDate', function (item, options) {
    var escaped = '';
    if (this.results && this.results.length) {
        var obj = Enumerable.From(options.contexts[0].results).Where("$.get('id')=='" + options.data.keywords.result.id + "'").FirstOrDefault();
        if (obj) {
            escaped = moment('' + obj.get(options.data.properties[0].split('.')[1])).format('YYYY-MM-DD @ HH:mm');
            //console.log(escaped);
            if (escaped === 'Invalid date') {
                return ''
            }
            return new Handlebars.SafeString(escaped);
        }
    }
    else {
        escaped = moment('' + options.contexts[0].get(options.data.properties[0])).format('YYYY-MM-DD @ HH:mm');
        if (escaped === 'Invalid date') {
            return ''
        }
        return new Handlebars.SafeString(escaped);
    }
    return '';
});

Handlebars.registerHelper('ifeq', function (value) {
    var prop = Object.keys(value.hash)[0];
    if (value.hash[prop] == value.hashContexts[prop][prop]) {
        return value.fn(this);
    }
    return value.inverse(this);
});


Ember.Handlebars.helper('wikiurl', function (item, options) {

});


Ember.TextField.reopen({
    attributeBindings: ['style'],
    style: 'style'
});


App.HomeNavView = Ember.View.extend({
    tagName: 'li',
    classNameBindings: ['active'],
    layoutName: 'home-nav',
    isActive: Ember.computed.equal('activeTagzz', 'active'),
    activeTagzz: null,
    tagThisMate: function () {
        var _this = this;
        $('body').on('pathChanged', function () {
          Ember.run.scheduleOnce('afterRender', this, function(){
                
                // Get the targeted element
                var isActive  = false;
                var a = _this.$('a');   
                       
                a.each(function (i, j, y) {
                    j = $(j);

                    // This is to highlight the workflow button when looking at a step ;)
                    var specialRule = (j.attr('href').replace(/^#\//, '') == 'workflow/undefined') && (window.location.hash.substring(2).indexOf('process') == 0)
                    
                    // Either matches active in the elements below
                    // OR url href matches window.location
                    // OR any special rules
                    if (specialRule || j.attr('class').indexOf('active') != -1 || j.attr('href').replace(/^#\//, '') === window.location.hash.substring(2))       
                        isActive = true;   
                }); 
                
                _this.set('activeTagzz', isActive);

            })
        }).trigger('pathChanged')
    }.on('didInsertElement')
});


/* TinyMCE Component
-------------------------------------------------- */
App.TinymceEditorComponent = Ember.Component.extend({
    // Warning!!! only use tinyMCE not tinymce !!!
    editor: null,
    data: {},
    watchData: true,
    disableWatchDataOnStartup: '',
    didInsertElement: function () {
        var _this = this;

        // The magic config - http://www.tinymce.com/wiki.php/Configuration
        var config = {};

        config = $.extend(config, {
            statusbar: false,
            resize: false,


            // inline: true
            // mode: "exact",
            width: "100%",
            //height: '100%',// this is for auto height onhly
            height: "400",
            autoresize: true
        });


        //// this code make it adjust to container height
        //function resize() {
        //    setTimeout(function () {
        //        var max = $('.mce-tinymce').css('border', 'none').parent().outerHeight()
        //        max = max - $('.mce-menubar.mce-toolbar').outerHeight() //menubar
        //        max = max - $('.mce-toolbar-grp').outerHeight() //toolbar
        //        max = max - 1;
        //        $('.mce-edit-area').height(max)
        //    }, 200);
        //}
        //$(window).on('resize', function () {
        //    resize();
        //})

        config.forced_root_block = false;
        config.forced_root_block = false;
        //tinyMCE.init({
        //    theme: "advanced",
        //    schema: "html5",
        //    mode: "specific_textareas",
        //    editor_selector: "tinymce",
        //    plugins: "fullscreen,autoresize,searchreplace,filepicker,locationpicker,inlinepopups",
        //    theme_advanced_toolbar_location: "top",
        //    theme_advanced_toolbar_align: "left",
        //    theme_advanced_buttons1: "search,replace,|,cut,copy,paste,|,undo,redo,|,link,unlink,charmap,emoticon,codeblock,|,filepicker,|,locationpicker,|,bold,italic,|,numlist,bullist,formatselect,|,code,fullscreen",
        //    theme_advanced_buttons2: "",
        //    theme_advanced_buttons3: "",
        //    convert_urls: false,
        //    valid_elements: "*[*]",
        //    // shouldn't be needed due to the valid_elements setting, but TinyMCE would strip script.src without it.
        //    extended_valid_elements: "script[type|defer|src|language]"
        //});

        // Setup plugins and toolbar
        config.plugins = ["locationpicker myfilepicker code link noneditable myformeditor myliveformeditor myworkflow"];
        config.toolbar = ["undo redo | styleselect | bold italic | alignleft aligncenter alignright | bullist numlist outdent indent | code link | locationpicker myfilepicker myformeditor myworkflow"];
        config.schema = "html5";
        config.menubar = false;
        config.valid_elements = "*[*]";
        config.valid_elements = '*[margin-left|class|href|src|width|height|onlick|role|id|name|title|placeholder]';

        //config.extended_valid_elements = "script[type|defer|src|language]";
        config.extended_valid_elements = "div[data-json|id|class|type]";
        // Choose selector
        config.selector = "#" + _this.get("elementId");
        config.convert_urls = false;
        config.content_css = ["/Modules/EXPEDIT.Flow/Static/AdminTheme/css/bootstrap/bootstrap.css", "/Modules/EXPEDIT.Flow/Static/AdminTheme/css/bootstrap/bootstrap-overrides.css", "/Modules/EXPEDIT.Flow/Styles/expedit-flow.css", "/Modules/EXPEDIT.Share/Styles/expedit-share.css", "/Modules/EXPEDIT.Flow/Static/AdminTheme/css/lib/font-awesome.css"];
        config.object_resizing = false; //'table' should have worked
        // Setup what happens on data changes
        config.setup = function (editor) {
            //editor.execCommand('mceRepaint', false);
            editor.settings.object_resizing = false;
            editor.on('change', function (e) {
                var newData = e.level.content;

                // clean new data
                var cleanData = '';
                window.cleanFunctions($(newData));
                $(newData).each(function (i, d) {
                    var temp = $(d)
                    temp = window.cleanFunctions(temp);


                    window.renderFunctions(temp);

                    if (typeof temp[0] !== 'undefined') {
                        cleanData += temp[0].outerHTML;
                    }

                })

                _this.set('watchData', false);
                if (newData && (_this.get('disableWatchDataOnStartup')!=="yes"))  {
                    _this.set('data', cleanData);
                }
                _this.set('watchData', true);
                _this.set('disableWatchDataOnStartup', "")

            });

        }

        // Set content once initialized
        config.init_instance_callback = function (editor) {
            _this.update();
            //resize();
            $('div').each(function () {
                $(this).attr('title', $(this).attr('aria-label'));
            });
        }

        tinyMCE.init(config);


    },
    update: function () {
        if (this.get('watchData')) {
            var content = this.get('data');
            if (content && tinyMCE.activeEditor !== null) {
                //content = $(content);
                //var tinyFrame = $(tinyMCE.activeEditor.contentDocument.firstElementChild);
                //window.renderFunctions(content);
                //tinyFrame.html(content);
                //tinyMCE.activeEditor.setContent(content);
                tinyMCE.execCommand('mceSetContent', false, content);

            }
        }
    }.observes('data')
});



App.MylicensesRoute = Ember.Route.extend({
    model: function () {
        return Ember.RSVP.hash({
            licenses: this.store.find('myLicense'),
        });
    }
})

App.MylicensesController = Ember.ObjectController.extend({
    needs: ['application'],
    title: 'My Licenses',
    userModal: false,
    activeItem: null,
    actions: {
        editPermission: function (item) {

            // So in the submit we know what file we should be diting
            this.set('activeItem', item);

            this.set('userModal', true); // Show the modal before anything else

            // Make selectbox work after it's been inserted to the view - jquery hackss
            Ember.run.scheduleOnce('afterRender', this, function () {
                $('#add-user-perm').select2({
                    placeholder: "Enter Username...",
                    minimumInputLength: 2,
                    multiple: false,
                    //createSearchChoice : function (term) { return {id: term, text: term}; },  // thus is good if you want to use the type in item as an option too
                    ajax: { // instead of writing the function to execute the request we use Select2's convenient helper
                        url: "/share/getusernames",
                        dataType: 'json',
                        multiple: false,
                        data: function (term, page) {
                            return { id: term };
                        },
                        results: function (data, page) { // parse the results into the format expected by Select2.
                            if (data.length === 0) {
                                return { results: [] };
                            }
                            var results = Enumerable.From(data).Select("f=>{id:f.Value,tag:f.Text}").ToArray();
                            return { results: results, text: 'tag' };
                        }
                    },
                    formatResult: function (state) { return state.tag; },
                    formatSelection: function (state) { return state.tag; },
                    escapeMarkup: function (m) { return m; }
                })
            });
        },
        submitPermission: function () {
            var _this = this;
            var newusers = $('#add-user-perm').val()
            var id = this.get('activeItem').id;
            // console.log(this.get('activeItem'));
            if (newusers !== '') {
                Enumerable.From(newusers.split(',')).ForEach(function (f) {
                    var a = _this.store.getById('myLicense', id);
                    a.set('LicenseeGUID', f);
                    a.save().then(function (lic) {
                        Messenger().post({ type: 'success', message: "Successfully updated license.", id: 'user-security' })
                        _this.store.find('myLicense', { id: id });
                    }, function () {
                        Messenger().post({ type: 'error', message: "Could not update license.", id: 'user-security' })

                    });
                });
            }
            this.set('userModal', false);
        },
        cancelPermission: function () {
            this.set('userModal', false);
        }
    }
});

App.MyprofilesRoute = Ember.Route.extend({
    model: function () {
        var _this = this;
        return Ember.RSVP.hash({
            profiles: this.store.find('myProfile')
        });
    },
    afterModel: function (m) {
        if (m.profiles.content && m.profiles.content.length > 0)
            m.profile = m.profiles.get('firstObject');
    }
});

App.MyprofilesController = Ember.ObjectController.extend({
    needs: ['application'],
    title: function() {
        return 'My Profile'
    }.property('profile'),
    emailValid: function() {
        var emailpat = /^[^@]+@[^@]+\.[^@\.]{2,}$/;
        var email = this.get('profile.DefaultEmail');
        if (email.match(emailpat)) {
            return false;
        } else {
            return 'Please use a valid email address.';
        }
    }.property('profile.DefaultEmail'),
    actions: {
        updateProfile: function (profile) {
            if (this.get('emailValid'))
                return;
            var _this = this;
            //Let's be strict what we can save
            var firstname = profile.get('Firstname');
            var surname = profile.get('Surname');
            var company = profile.get('AddressName');
            var email = profile.get('DefaultEmail');
            //profile.rollback();
            var m = this.store.getById('myProfile', profile.id);
            m.set('Firstname', firstname);
            m.set('Surname', surname);
            m.set('AddressName', company);
            m.set('DefaultEmail', email);
            m.save().then(function (pro) {
                Messenger().post({ type: 'success', message: "Successfully updated profile.", id: 'user-security' })
                _this.store.find('myProfile', { id: profile.id });
            }, function () {
                Messenger().post({ type: 'error', message: "Could not update profile.", id: 'user-security' })
            });

        }
    }
});


App.WorkflowController = Ember.ArrayController.extend({
});

App.WorkflowView = Ember.View.extend(Ember.ViewTargetActionSupport, {
    template: Ember.Handlebars.compile(''), // Blank template
    dataBinding: "controller.data",
    selectedBinding: "controller.selected",
    selectedChanged: function () {

        // Properties required to build view
        var p = this.getProperties("elementId", "data", "lastCount", "selected");

        // Used to gain context of controller in on selected changed event
        var controller = this;
        var container = document.getElementById(p.data.outlet);
        var options = {
            width: '85%',
            height: '400px',
            navigation: false,
            smoothCurves: true,
            physics: { barnesHut: { centralGravity: 0.0155, springConstant: 0.01, damping: 0.1, springLength: 170 } },
            stabilize: false,
            stabilizationIterations: 200,
            dataManipulation: {
                enabled: false,
                initiallyVisible: false
            }
        };
        var _this = this;
        //debugger;
        //console.log(new Date());
        //var network = new vis.Graph(container, data, options);
        var all = Enumerable.From(App.Node.store.all('node').content);
        var promises = [];
        var nodes = [];
        var edges = [];
        Enumerable.From(App.Node.store.all('node').content).ForEach(function (f) {
            promises.push(f.get('workflows').then(function (g) {
                $.each(g.content, function (key, value) {
                    if (value.id == _this.data.wfid)
                        nodes.push(f);
                });
            }));
        });


        var updateGraph = function (nodes,edges) {
            var edges = Enumerable.From(App.Node.store.all('edge').content).Where("f=>f.get('GroupID')=='" + _this.data.wfid + "'").ToArray();
            nodes = $.map(nodes, function (item) { return { id: item.get('id'), label: item.get('localName') }; });
            edges = $.map(edges, function (item) { return { from: item.get('from'), to: item.get('to'), style: item.get('style'), widthSelectionMultiplier: item.get('widthSelectionMultiplier'), color: item.get('color') }; });
            Enumerable.From(nodes).ForEach(
                function (value) {
                    delete value.color;
                    delete value.fontColor;
                    if (!Enumerable.From(edges).Where("f=>f.to=='" + value.id + "'").Any()) {
                        value.color = "#FFFFFF"; //BEGIN
                        value.fontColor = "#000000";
                    }
                    else if (!Enumerable.From(edges).Where("f=>f.from=='" + value.id + "'").Any()) {
                        value.color = "#333333"; //END
                        value.fontColor = "#FFFFFF";
                    }
                    else {
                        value.color = "#6fa5d7"; //Current
                        value.fontColor = "#000000";
                    }
                    return value;
                });

            var data = {
                nodes: nodes,
                edges: edges,
            };
            var network = new vis.Network(container, data, options);
            network.scale = 0.82; //Zoom out a little
            $("#" + p.data.outlet).append('<h4>' + _this.data.wfname + ' Workflow</h4>');
            network.on('click', function (data) {
                if (data.nodes.length > 0) {
                    //_this.get('controller').send('transition');
                    //_this.transitionTo('search'); // m.selectedID, { queryParams: { workflowID: newwf.id }}
                    var preview = queryParamsLookup('preview');
                    if (preview !== null)
                        document.location.hash = '#/process/' + data.nodes[0] + '?workflowID=' + _this.data.wfid + "&preview=" + preview;
                    else
                        document.location.hash = '#/process/' + data.nodes[0] + '?workflowID=' + _this.data.wfid;
                }
            });
        };

        var waitForLocalePromises = function (nodes, edges) {
            Enumerable.From(nodes).Select("$.get('localName')").ToArray();
            if (Enumerable.From(nodes).Select("typeof $.get('_localePromise') === 'undefined'").Any("f=>f"))
                setTimeout(function () { waitForLocalePromises(nodes, edges) }, 150);
            else
                Ember.RSVP.allSettled(Enumerable.From(nodes).Select("$.get('_localePromise')").ToArray()).then(function (array) {
                    updateGraph(nodes, edges);
                });
        };

        Ember.RSVP.allSettled(promises).then(function (array) {
            waitForLocalePromises(nodes, edges);
        });


        //Ember.run.scheduleOnce('afterRender', this, getData);



        //this.notifyPropertyChange("selected");
    }, //.observes("selected"),

    didInsertElement: function () {

        this.selectedChanged();

    }
});



////

function uploadPhoto(event) {
    var files = event.files;
    var iSize = files[0].size / 1024;
    if (iSize > 2000) {
        Messenger().post({ type: 'error', message: "File is too large to upload.", id: 'file-security' });
        return;
    }

    // START A LOADING SPINNER HERE

    // Create a formdata object and add the files
    var data = new FormData();
    $.each(files, function (key, value) {
        data.append(key, value);
    });

    $.ajax({
        url: '/share/uploadphoto',
        type: 'POST',
        data: data,
        cache: false,
        dataType: 'json',
        processData: false, // Don't process the files
        contentType: false, // Set content type to false as jQuery will tell the server its a query string request
        success: function (data, textStatus, jqXHR) {
            if (typeof data.error === 'undefined') {
                // Success so call function to process the form
                //submitForm(event, data);
                d = new Date();
                var old = $("#profileImageThumb").attr("src").replace(/(\?.*)/ig, '');
                $("#profileImageThumb").attr("src",  old + "?" + d.getTime());
            }
            else {
                // Handle errors here
                console.log('ERRORS: ' + data.error);
            }
        },
        error: function (jqXHR, textStatus, errorThrown) {
            // Handle errors here
            console.log('ERRORS: ' + textStatus);
            // STOP LOADING SPINNER
        }
    });
}


Ember.HelloModalComponent = Ember.Component.extend({
    classNames: ['hello-modal'],
     actions: {
       gotIt: function() {
         this.sendAction('dismiss');
       },
       change: function() {
         this.sendAction('changeSalutation');
       }
     }
})
