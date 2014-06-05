Ember.FEATURES["query-params"] = true;

function RedirectToLogin() {
    window.location.hash = '#/login'
    location.reload();
}

App = Ember.Application.create({
    // LOG_TRANSITIONS: true,
    rootElement: '#emberapphere'
});

App.Router.map(function () {
    this.route('graph', { path: 'process/:id' });
    this.route('workflow', { path: 'workflow/:id' });
    this.route('wikipedia', { path: "/wikipedia/:id" });
    this.route('search');
    this.route('myaccount');
    this.route('myworkflows');
    this.route('file');
    this.route('permission');
    this.route('login');


// Currently unused.
// this.route('userlist');
// this.route('userprofile');
// this.route('usernew');
});


//App.MyAccountRoute = Ember.Route.extend({
//    model: function () {
            
//    }
//});


//App.MyAccountController = Ember.ObjectController.extend({
//    needs: ['application']
//})




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
            return this.transitionTo('graph', NewGUID(), { queryParams: {workflowID: NewGUID()}});
        }
        var fn = m.get('firstNode');
        var id = m.get('id');
        if (typeof fn !== 'undefined' && fn) {
            //this.controllerFor('application').set('workflowID', id);
            return this.transitionTo('graph', fn, { queryParams: {workflowID: id }});
        }
        else {
            if (!id)
                id = NewGUID();
            //this.controllerFor('application').set('workflowID', id);
            return this.transitionTo('graph', NewGUID(), { queryParams: { workflowID: id } });

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
    queryParams: ['type'],
    types: [{ id: 'node', text: 'Processes' }, { id: 'workflow', text: 'Workflows' }, { id: 'file', text: 'Files' }],
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
                    var a = _this.store.createRecord('MySecurityList', {
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
                    var a = _this.store.createRecord('MySecurityList', {
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
                    var a = _this.store.createRecord('MySecurityList', {
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
                    var a = _this.store.createRecord('MySecurityList', {
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




App.LoginController = Ember.Controller.extend({
    needs: ['application'],
    email: "",
    rememberme: false,
    password: "",
    bindingChercker: function(){
        //console.log('value changed')
    }.observes('email', 'password', 'rememberme'),
    actions: {
        loginUser: function(){
            var UserName = this.get('email');
            var Password = this.get('password');
            var RememberMe = this.get('rememberme');
            var _this = this;

            this.set('password', '');

            $.post('/share/login', {
                UserName: UserName,
                Password: Password,
                RememberMe: RememberMe
            }).then(function(data){
                if (data === true) {
                    Messenger().post({ type: 'success', message: 'Successfully logged in.', id: 'authenticate' });
                    _this.set('controllers.application.isLoggedIn', true)
                    // Duplicate code starts here, but works fine
                    $.ajax({
                      url: "/flow/myuserinfo"
                    }).then(function(data){
                        data.UserName = ToTitleCase(data.UserName);
                        _this.set('controllers.application.userProfile', data);
                    }, function (jqXHR) {
                      jqXHR.then = null; // tame jQuery's ill mannered promises
                    });
                    // end duplicate code
                    _this.transitionToRoute('search');
                } else {
                    Messenger().post({type:'error', message:'Incorrect username and/or password. Please try again.', id:'authenticate'});
                }

            }, function (jqXHR) {
                  jqXHR.then = null; // tame jQuery's ill mannered promises
            });
            // debugger;
        }
    }


})

App.ApplicationController = Ember.Controller.extend({
    ////queryParams: ['workflowID'],
    //workflowID: null,
    currentPathDidChange: function () {
        window.scrollTo(0, 0); // THIS IS IMPORTANT - makes the window scroll to the top if changing route
        var currentPath = this.get('currentPath');

        //if (currentPath !== "graph" && currentPath !== "workflow") { // remove workflow id query param unless on graph/wk route
        //    this.set('workflowID', null)
        //}
        App.set('currentPath', currentPath);  // Set path to the top
    }.observes('currentPath'), // This set the current path App.get('currentPath');
    isLoggedIn: false,
    logoutModal: false,
    userProfile: '',
    actions: {
        logoutUser: function(){
            var _this = this;
            $.post('/share/logout').then(function(data){
                // _this.set('isLoggedIn', false); - not necessary as reloads
                // _this.set('userProfile', '');
                // App.reset(); - should reset
                // _this.transitionToRoute('login');
                // Messenger().post({ type: 'success', message: 'Successfully logged out.', id: 'authenticate' });
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
                        $.ajax({
                          url: "/flow/myuserinfo"
                        }).then(function(data){
                            data.UserName = ToTitleCase(data.UserName);
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


        Ember.run.scheduleOnce('afterRender', this, function () {
            // navbar notification popups
            $(".notification-dropdown").each(function (index, el) {
                var $el = $(el);
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

                $trigger.click(function (e) {
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
                });
            });


            // skin changer
            $(".skins-nav .skin").click(function (e) {
                e.preventDefault();
                if ($(this).hasClass("selected")) {
                    return;
                }
                $(".skins-nav .skin").removeClass("selected");
                $(this).addClass("selected");

                if (!$("#skin-file").length) {
                    $("head").append('<link rel="stylesheet" type="text/css" id="skin-file" href="">');
                }
                var $skin = $("#skin-file");
                if ($(this).attr("data-file")) {
                    $skin.attr("href", $(this).data("file"));
                } else {
                    $skin.attr("href", "");
                }

            });


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
                }
            });
            $menu.click(function(e) {
                e.stopPropagation();
            });
            $("#menu-toggler").click(function (e) {
                e.stopPropagation();
                $("body").toggleClass("menu");
            });
            $(window).resize(function() {
                $(this).width() > 769 && $("body.menu").removeClass("menu")
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



 App.ApplicationRoute = Ember.Route.extend({
   actions: {
     loading: function() {

        // Remove menu link - mobile test
        Ember.run.scheduleOnce('afterRender', this, function(){
            $('body').removeClass('menu');
        });

       Pace.restart();
       this.router.one('didTransition', function() {
         return setTimeout((function() {
           return Pace.stop();
         }), 0);
       });
       return true;
     },
     error: function() {
       return setTimeout((function() {
         return Pace.stop();
       }), 0);
     }
   }
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
    "InternalUrl": DS.attr(''),
    "ExternalUrl": DS.attr(''),
    "ResourcePath": function(){
        return '/share/file/' + this.get('ReferenceID');
    }.property('ReferenceID'),
     "ResourcePreviewPath": function(){
        return '/share/preview/' + this.get('ReferenceID');
    }.property('ReferenceID'),
    "Author": DS.attr(''),
    "Updated": DS.attr(''),
    humanName: function () {
        var temp = this.get('Title');
        if (temp)
            return ToTitleCase(temp.replace(/_/g, ' '));
        else
            return null;
    }.property()
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
        return 'col-md-' + (12 / i);
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
    loadGraph: function(){
        var controller = this;
        if (this.get('graph') && this.get('componentURI').length > 0) {
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
    loadFile: function(){
        var controller = this;
        if (this.get('file') && this.get('componentURI').length > 0) {
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
    }.observes('pageSize', 'tags'), // the did insert element here doesn't  work that's why the view is setup below to kickoff the initial search
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
                geos[geo.id] = { name: '<a href="/flow/#/process/' + a.get('ReferenceID') + '">' + a.get('Title') + '</a>', id: geo.id, geo: geo.data };
            }
            else {
                geos[geo.id].name += '<br/><a href="/flow/#/process/' + a.get('ReferenceID') + '">' + a.get('Title') + '</a>';
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
    queryParams: {
        workflowID: {
            refreshModel: true
        }
    },
    actions: {
        error: function(){
            Messenger().post({ type: 'error', message: 'Could not find process. Ensure you have permission and are logged in.' });
            // Ember.run.later(null, RedirectToLogin, 3000); maybe not do a refresh
        }
    },
    model: function (params) {
        var id = params.id;
        id = id.toLowerCase(); // just in case
        return Ember.RSVP.hash({
            data: this.store.find('node', { id: id, groupid: params.workflowID }),
            selectedID: id,
            content: '',
            label: '',
            editing: false,  // This gets passed to visjs to enable/disable editing dependig on context
            params: params
        });
    },
    afterModel: function (m) {
        if (m.data && m.data.content && m.data.content.length > 0) {
            //Get the selected item from m.data
            m.selected = Enumerable.From(m.data.content).Where("f=>f.id==='" + m.selectedID + "'").FirstOrDefault();
            if (typeof m.selected === 'undefined') {
                m.selected = Enumerable.From(m.data.content).FirstOrDefault();
                if (typeof m.selected === 'undefined') {
                    if (m.params)
                        this.transitionTo('graph', NewGUID(), { queryParams: { workflowID: m.params.workflowID } });
                    else 
                        this.transitionTo('graph', NewGUID());
                }
                else {
                    this.transitionTo('graph', m.selected.id);
                }
                return;
            }
            var _this = this;
            var array = { nodes: [], edges: [] };
            var depthMax = 15; // currently depthMax is limited to 1 unless the data is already in ember store
            var nodeMax = -1;
            var prime = {};
            prime.edges = [];
            prime.workflows = [];
            var edgePromises = [];
            var workflowPromises = [];
            prime.nodes = Enumerable.From(m.data.content).Select(
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
                        //if (m.params.workflowID == edge.get('GroupID')) //Hide connected wf edges
                        prime.edges.push({ id: edge.get('id'), from: edge.get('from'), to: edge.get('to'), color: edge.get('color'), width: edge.get('width'), style: edge.get('style'), group: edge.get('GroupID') });
                    });
                }
            };
            var getWorkflow = function (workflows) {
                if (workflows.get('length') > 0) {
                    workflows.forEach(function (workflow) {
                        prime.workflows.push({ id: workflow.get('id'), name: workflow.get('name'), humanName: workflow.get('humanName'), firstNode: workflow.get('firstNode') });
                    });
                }
            };
            var sessionNodes = [];
            var residents = App.Node.store.filter('node', function (record) {
                return (record.get('edges').content == null || record.get('edges').content.loadingRecordsCount == 0) && typeof record.get('VersionUpdated') !== 'undefined';
            }).then(function (val) {
                //Don't show orphans anymore
                //sessionNodes = Enumerable.From(val.content).Select(
                //    function (f) {
                //        return {
                //            id: f.get('id'), label: f.get('label'), shape: f.get('shape'), group: f.get('group')
                //        }
                //    }).ToArray();
            });

            Ember.RSVP.allSettled([Ember.RSVP.map(edgePromises, addEdge), Ember.RSVP.map(workflowPromises, getWorkflow), residents])
                .then(function () {
                    if (!Enumerable.From(prime.workflows).Any("f=>f.id=='" + m.params.workflowID + "'")) {
                        var newwf = Enumerable.From(prime.workflows).FirstOrDefault();
                        if (typeof newwf !== 'undefined' && newwf) {

                            _this.transitionTo('graph', m.selectedID, { queryParams: { workflowID: newwf.id }})
                        }
                    }
                    prime.nodes = Em.A(prime.nodes.concat(sessionNodes));
                    //debugger;
                    prime = Ember.Object.create(prime);
                    m.workflows = Em.A(Enumerable.From(prime.workflows)
                        .GroupBy("$.id", "", "key,e=>Ember.Object.create({id: key, name: e.source[0].name, humanName: e.source[0].humanName, firstNode: e.source[0].firstNode})")
                      .ToArray());

                    m.workflow = Ember.Object.create(Enumerable.From(m.workflows).Where("f=>f.id==='" + m.params.workflowID + "'").SingleOrDefault());
                    if (typeof m.workflow.get('name') === 'undefined') {
                        m.workflow.set('content').name = 'Untitled Workflow - ' + moment().format('YYYY-MM-DD @ HH:mm:ss');
                        m.workflow.set('id', m.params.workflowID);
                    }
                    delete prime.workflows;
                    m.graphData = prime;
                    m.graphData.workflowID = m.params.workflowID;
                });

        }
        else { //NEW NODE
            m.content = '';
            m.label = '';
            m.editing = false;
            m.humanName = '';
            //TODO WORKFLOW
            this.store.create
            m.graphData = { nodes: [], edges: [] };
            m.workflow = this.store.find('workflow', m.params.workflowID);
            m.workflow.then(function (item) {
                m.workflows = Em.A([{ id: item.get('id'), name: item.get('name'), humanName: item.get('humanName'), firstNode: item.get('firstNode') }]);
            }, function (item) {
                //m.workflow = App.Workflow.store.createRecord('workflow', { id: NewGUID(), name: 'Untitled Workflow - ' + new Date() });
                m.workflow.set('content').name = 'Untitled Workflow - ' + moment().format('YYYY-MM-DD @ HH:mm:ss');
                m.workflows = Em.A([m.workflow]);
            });
            m.selected = App.Node.store.createRecord('node', { id: m.selectedID, label: 'Untitled Process - ' + moment().format('YYYY-MM-DD @ HH:mm:ss'), content: '', VersionUpdated: Ember.Date.parse(moment().format('YYYY-MM-DD @ HH:mm:ss')) });


        }



    }

});


App.GraphController = Ember.ObjectController.extend({
    queryParams: ['workflowID'],
    newName: null,
    newContent: null,
    existingNodeModal: false,
    workflowEditNameModal: false,
    workflowNewModal: false, // up to here is for new ones
    editing: true,
    workflowID: null, // available ids will be in model
    workflowEditModal : false,
    validateWorkflowName: false,
    validateNewName: false,
    validateNewNewName: false,
    loadingNewNewName: false,
    validateExistingName: false,
    loadingWorkflowName: false,
    loadingNewName: false,
    loadingExistingName: false,
    workflowGte2: Ember.computed.gte('model.workflows.length', 2),
    graphDataLte2: Ember.computed.lte('model.graphData.length', 2),
    fitVis: function(){
        Ember.run.scheduleOnce('afterRender', this, function(){
            $('body').fitVids();
        })
    }.observes('model.content'),
    humanReadableName: function () {
        var temp = this.get('model.selected.label');
        if (temp)
            return ToTitleCase(temp.replace(/_/g, ' '));
        else
            return null;
    }.property('model.selected.label'),

    // Do something if the s
    graphDataTrigger : function () {

    }.observes('model', 'model.selected', 'model.@each.workflows'),
    changeSelected: function () {
        //alert('')

     this.transitionToRoute('graph', this.get('model.selectedID'));
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
        showExistingNodeModal: function(item){


            this.set('existingNodeModal', true); // Show the modal before anything else

            // Make selectbox work after it's been inserted to the view - jquery hackss
            Ember.run.scheduleOnce('afterRender', this, function(){
                $('#existingNodesel').select2({
                    placeholder: "Enter Process...",
                    minimumInputLength: 2,
                    tags: true,
                    //createSearchChoice : function (term) { return {id: term, text: term}; },  // thus is good if you want to use the type in item as an option too
                    ajax: { // instead of writing the function to execute the request we use Select2's convenient helper
                        url: "/flow/searches",
                        dataType: 'json',
                        multiple: true,
                        data: function (term, page) {
                            return {keywords: term, type: 'flow', pageSize: 8, page: page - 1 };
                        },
                        results: function (data, page) { // parse the results into the format expected by Select2.
                            // debugger;
                            if (data.search.length === 0) {
                                return { results: [] };
                            }
                            var total = data.search[0].TotalRows;
                            var more = (page * 8) < total;
                            var results = Enumerable.From(data.search).Select("f=>{id:f.id + f.Title,tag:f.Title}").ToArray();
                            return { results: results, text: 'tag', more: more };
                        }
                    },
                    formatResult: function(state) {return state.tag; },
                    formatSelection: function (state) {return state.tag; },
                    escapeMarkup: function (m) { return m; }
                });
            });
        },
        submitExistingNodeModal: function(){
            var _this = this;

            var existingNodes = $('#existingNodesel').val()



            if (existingNodes !== '') {
                Enumerable.From(existingNodes.split(',')).ForEach(function (f) {
                    // check if node already in store???
                    var id = f.substring(0,36)
                    var name = f.substring(36)



                    var result = _this.store.getById('node', id);
                    if (result === null) {
                        // need to push record into store
                        _this.store.push('node', {
                            id: id,
                            label: name
                        });
                    }

                    // check if already in graph
                    var currentNodesonScreen = _this.get('model').graphData;

                    var found = false
                    if (currentNodesonScreen) {
                         found = (Enumerable.From(currentNodesonScreen.nodes).Any("f=>f.id==='" + f + "'"));
                    }
                    if (!found) {
                        var newNode = _this.store.getById('node', id);
                        var a = { id: newNode.get('id'), label: newNode.get('label'), shape: newNode.get('shape'), group: newNode.get('group') };
                        currentNodesonScreen.nodes.push(a);
                        _this.set('model.graphData.nodes', currentNodesonScreen.nodes.concat([]));

                    } else {
                        alert('Node already on screen.')
                    }
                    //debugger;
                });
            }

            this.set('existingNodeModal', false);
        },
        cancelExistingNodeModal: function(){
            this.set('existingNodeModal', false);
        },
        updateWorkflowNameNow: function(){
            //new name - model.workflow.name
            //debugger;

            var _this = this;
            newWorkflow = App.Node.store.getById('workflow', this.get('workflowID'));
            if (newWorkflow === null || typeof newWorkflow.get('name') === 'undefined') {
                newWorkflow = App.Node.store.createRecord('workflow', {id: this.get('workflowID'), name: this.get('model.workflow.name') });
                this.set('model.workflow', newWorkflow);
            }
            else
                newWorkflow.set('name', this.get('model.workflow.name'));
            newWorkflow.save().then(function (data) {
                Messenger().post({ type: 'success', message: "Workflow successfully renamed." })

                if (_this.get('workflowGte2')) {
                    Enumerable.From(_this.get('model.workflows')).Where("f=>f.id==='" + _this.get('workflowID') + "'").Single().name = _this.get('model.workflow.name');
                    // _this.refresh();
                    location.reload();

                    // this.get('model.workflows').findProperty('id', this.get('workflowID')) - don't need linq.zzz
                }

                _this.set('workflowEditNameModal', false);
            }, function () {
                Messenger().post({ type: 'error', message: "Rename failed. Try again please." })

            })

        },
        toggleworkflowEditNameModal: function() {
            this.toggleProperty('workflowEditNameModal');
        },
        toggleWorkflowNewModal: function (data, callback) {
            this.toggleProperty('workflowNewModal');
        },
        toggleWorkflowEditModal: function (data, callback) {
            this.toggleProperty('workflowEditModal');
        },
        updateWorkflow: function () {
            var _this = this;
            newWorkflow = App.Node.store.getById('workflow', this.get('workflowID'));
            if (newWorkflow === null || typeof newWorkflow.get('name') === 'undefined') {
                newWorkflow = App.Node.store.createRecord('workflow', { id: this.get('workflowID'), name: this.get('model.workflow.name') });
                this.set('model.workflow', newWorkflow);
            }
            else
                newWorkflow.set('name', this.get('model.workflow.name'));
            newWorkflow.save().then(function (data) {
                Messenger().post({ type: 'success', message: 'Successfully Updated Workflow' });
                var newNode = App.Node.store.getById('node', _this.get('selectedID'));
                if (typeof newNode === 'undefined' || !newNode) {
                    newNode = App.Node.store.createRecord('node', { id: _this.get('model.selectedID'), label: _this.get('model.selected.label'), content: _this.get('model.selected.content'), VersionUpdated: Ember.Date.parse(new Date()) });
                    this.set('model.selected', newNode);
                }
                newNode.save().then(function (f) {
                    Messenger().post({ type: 'success', message: 'Successfully Updated Process' });
                    var a = { id: f.get('id'), label: f.get('label'), shape: f.get('shape'), group: f.get('group') }
                    var duplicate = Enumerable.From(_this.get('graphData.nodes')).Where("g=>g.id=='" + f.get('id') + "'").FirstOrDefault();
                    if (duplicate)
                        _this.get('model.graphData.nodes').removeObject(duplicate);
                    _this.get('model.graphData.nodes').pushObject(a);
                    _this.set('model.graphData.nodes', _this.get('model.graphData.nodes').concat([])); //HACK TODO
                    _this.set('workflowEditModal', false);
                }, function () {
                    if (_this.get('model'))
                        Messenger().post({ type: 'error', message: 'Error Updating Process' });
                    else
                        Messenger().post({ type: 'error', message: 'Error Adding Process' });;
                });
            }, function () {
                if (_this.get('workflowID'))
                    Messenger().post({type:'error', message:'Error Updating Workflow.'});
                else
                    Messenger().post({type:'error', message:'Error Adding Workflow'});
            });
        },
        addNewNode: function () {
            var _this = this;
            newWorkflow = App.Node.store.getById('workflow', this.get('workflowID'));
            if (newWorkflow === null || typeof newWorkflow.get('name') === 'undefined') {
                this.send('updateWorkflowNameNow');
            }
            var c = this.get('newContent')
            var n = this.get('newName')
            var id = NewGUID();
            var newNode = { id: id, label: n, content: c, VersionUpdated: Ember.Date.parse(new Date()) };
            App.Node.store.createRecord('node', newNode).save().then(function (f) {
                Messenger().post({ type: 'success', message: 'Successfully Added Process' });
                var a = { id: f.get('id'), label: f.get('label'), shape: f.get('shape'), group: f.get('group') }
                _this.get('graphData').nodes.pushObject(a);
                _this.set('newName', null);
                _this.set('newContent', null);
                _this.toggleProperty('workflowNewModal');
               _this.set('graphData.nodes', _this.get('graphData.nodes').concat([])); //HACK TODO
            }, function () {
                Messenger().post({type:'error', message:'Error Adding Process'});
            });
        },
        addNewEdge: function (data) {
            var _this = this;
            data.id = NewGUID();
            data.GroupID = this.get('workflowID');
            App.Node.store.createRecord('edge', data).save().then(function () {
                Messenger().post({ type: 'success', message: 'Successfully Added Connection' });
                var f = App.Node.store.getById('edge', data.id);
                var a = { id: f.get('id'), from: f.get('from'), to: f.get('to'), color: f.get('color'), width: f.get('width'), style: f.get('style') }
                _this.get('graphData.edges').pushObject(a);
                _this.set('graphData.edges', _this.get('graphData.edges').concat([]));
            }, function () {
                Messenger().post({type:'error', message:'Error Adding Connection'});
                Messenger().post({type:'info', message:'No Workflow name set. Please try again.'});
                _this.toggleProperty('workflowEditModal'); //Hack TODO can't save without wf
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
                if (m)
                    promises.push(m.destroyRecord());
            });
            Ember.RSVP.allSettled(promises).then(function (array) {
                if (Enumerable.From(array).Any("f=>f.state=='rejected'"))
                    Messenger().post({type:'error', message:'Error Updating Workflow'});
                else {
                    _this = _this;

                    var currentSelected = _this.get('selected');

                    var graphData = _this.get('graphData');
                    var removedNode = false;
                    Enumerable.From(graphData.nodes).Where(function (f) {
                        if (data.nodes.indexOf(f.id) > -1)
                            return true;
                        else
                            return false;
                    }).ForEach(function (f) {
                        removedNode = true;
                        graphData.nodes.removeObject(f);
                    });
                    if (removedNode) {
                        var nodeRedirect = Enumerable.From(graphData.nodes).FirstOrDefault();
                        if (nodeRedirect)
                            _this.transitionToRoute('graph', nodeRedirect.id);
                        else
                            _this.transitionToRoute('graph', data.nodes[0].id);
                    }
                    _this.set('graphData.nodes', _this.get('graphData.nodes').concat([])); //TODO HACK

                    Enumerable.From(graphData.edges).Where(function (f) {
                        if (data.edges.indexOf(f.id) > -1)
                            return true;
                        else
                            return false;
                    }).ForEach(function (f) {
                        graphData.edges.removeObject(f);
                    });
                    _this.set('graphData.edges', _this.get('graphData.edges').concat([])); //TODO HACK


                    Messenger().post({ type: 'success', message: 'Successfully Updated Workflow' });
                    //Enumerable.From(data.edges).ForEach(function (f) {
                    //    var m = App.Node.store.getById('edge', f);
                    //    if (m)
                    //        promises.push(m.destroyRecord());
                    //});


                }

            }, function (error) {

            });

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
    classNames: ['vis-component'],
    selected: '',
    graph: null,
    setup: function () {

        var _this = this;
        var centralGravity = 0.02; //TODO HACK, AG, Less gravity for known graphs
        if (!IsGUID(this.selected)) {
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
            labels:{
                  add:"Add Process",
                  edit:"Edit",
                  link:"Add Connection",
                  del:"Delete selected",
                  editNode:"Edit Process",
                  back:"Back",
                  addDescription:"Click the empty space to create a Process.",
                  linkDescription:"Connect Processes by dragging.",
                  addError:"The function for add does not support two arguments (data,callback).",
                  linkError:"The function for connect does not support two arguments (data,callback).",
                  editError:"The function for edit does not support two arguments (data, callback).",
                  editBoundError:"No edit function has been bound to this button.",
                  deleteError:"The function for delete does not support two arguments (data, callback).",
                  deleteClusterError:"Clusters cannot be deleted."
            },
            //physics: {barnesHut: {enabled: false}, repulsion: {nodeDistance: 150, centralGravity: 0.15, springLength: 20, springConstant: 0, damping: 0.3}},
            smoothCurves: true,
            //hierarchicalLayout: {enabled:true},
            //physics: {barnesHut: {enabled: false, gravitationalConstant: -13950, centralGravity: 1.25, springLength: 150, springConstant: 0.335, damping: 0.3}},
            //physics: {barnesHut: {enabled: false}},
            //physics: { barnesHut: { gravitationalConstant: -8425, centralGravity: 0.1, springLength: 150, springConstant: 0.058, damping: 0.3 } },
            physics: { barnesHut: { centralGravity: centralGravity, springConstant: 0.01, damping: 0.1, springLength: 170 } },
            stabilize: false,
            stabilizationIterations: 200,
            dataManipulation: this.get('editing'),
            onAdd: function (data, callback) {
                _this.sendAction('toggleWorkflowNewModal', data, callback);
            },
            onDelete: function (data, callback) {
                if (data.nodes.length > 0) {
                    var r = confirm("Sure you want to delete this process?");
                    if (!r) {
                        return false;
                    }
                }
                _this.sendAction('deleteGraphItems', data, callback);
            },
            onEdit: function (data, callback) {
                _this.sendAction('toggleWorkflowEditModal', data, callback);
            },
            onConnect: function (data, callback) {
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
            }
        };

        // Initialise vis.js
        this.graph = new vis.Graph(container, data, options);

        // This sets the new selected item on click
        this.graph.on('click', function (data) {
            if (data.nodes.length > 0) {
                _this.set('selected', data.nodes[0]);
                if (IsGUID(data.nodes[0])) {
                    var md = _this.get('data'); // has to be synched with data
                    var d = _this.get('vizDataSet');
                    var edges = d.edges.get();
                    var nodes = d.nodes.get();
                    var n = d.nodes.get(data.nodes[0]);
                    Enumerable.From(nodes).ForEach(
                        function (value) {
                            delete value.color;
                            if (!Enumerable.From(edges).Where("f=>f.to=='" + value.id + "' && f.group == '" + md.workflowID + "'").Any() && value.group.indexOf(md.workflowID) > -1) {
                                value.color = "#FFFFFF"; //BEGIN                   
                            }
                            if (!Enumerable.From(edges).Where("f=>f.from=='" + value.id + "' && f.group == '" + md.workflowID + "'").Any()
                                    && (value.group.indexOf(md.workflowID) > -1
                           || (value.group.indexOf(md.workflowID) < 0 && Enumerable.From(edges).Where("f=>f.to=='" + value.id + "' && f.group == '" + md.workflowID + "'").Any()))) {
                                value.color = "#000000"; //END
                                value.fontColor = "#FFFFFF";
                            }
                            else {
                                value.fontColor = "#000000";
                            }

                            return value;
                        });
                    d.nodes.update(nodes);
                }
                //TODO: check for node workflow currency and update color
                //if (value.group.indexOf(md.workflowID) < 0)
                //    value.color = "#00FF00";

            }
        });

        //this.graph.on('stabilized', function (iterations) {
        //    _this.graph.zoomExtent(); //Not working?!
        //});
        this.graph.scale = 0.82; //Zoom out a little

        $(window).resize(function () {
            _this.graph.zoomExtent(); //Not working?!
            _this.graph.redraw(); // This makes the graph responsive!!!
        });
    },
    dataUpdates: function () {

        //console.log('updated')
        //var _this = this;
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

        var firstNode = true;
        //Step 1a: Clean Nodes for Presentation
        md.nodes = Enumerable.From(md.nodes).Select(
            function (value, index) {
                if (typeof value !== 'undefined' && typeof value.label === 'string')
                    value.label = value.label.replace(/_/g, ' ');
                if (firstNode) {
                    value.x = 150;
                    firstNode = false;
                }
                value.mass = 1.2;
                if (IsGUID(value.id)) {
                    if (!Enumerable.From(md.edges).Where("f=>f.to=='" + value.id + "' && f.group == '" + md.workflowID + "'").Any() && value.group.indexOf(md.workflowID) > -1) {
                        value.color = "#FFFFFF"; //BEGIN                   
                    }
                    if (!Enumerable.From(md.edges).Where("f=>f.from=='" + value.id + "' && f.group == '" + md.workflowID + "'").Any()
                        && (value.group.indexOf(md.workflowID) > -1
                            || (value.group.indexOf(md.workflowID) < 0 && Enumerable.From(md.edges).Where("f=>f.to=='" + value.id + "' && f.group == '" + md.workflowID + "'").Any()))) {
                        value.color = "#000000"; //END
                        value.fontColor = "#FFFFFF";
                    }
                    else {
                        value.fontColor = "#000000";
                    }
                }
                return value;
            }).ToArray();

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
        //    //debugger;
        //});
        d.edges.update(newEdges);      


    }.observes('data', 'data.nodes', 'data.edges').on('didInsertElement')
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
    _group : null,
    group: function () {
        return Enumerable.From(this._data.edges).Select("f=>f.get('GroupID')").Distinct().ToArray().toString(); // any string, will be grouped - random color
    }.property(),
    humanName: function () {
        var temp = this.get('label');
        if (temp)
            return ToTitleCase(temp.replace(/_/g, ' '));
        else
            return null;
    }.property('label'),
    VersionUpdated: DS.attr('date')
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
    comment: DS.attr('string'),
    firstNode: DS.attr('string'),
    humanName: function () {
        var temp = this.get('name');
        if (temp)
            return ToTitleCase(temp.replace(/_/g, ' '));
        else
            return null;
    }.property()
});


App.MyWorkflow = App.Search.extend({});
App.MyNode = App.Search.extend({});
App.MyFile = App.Search.extend({});


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
    CanDelete: DS.attr('')
});

//  Don't need these
// App.MyWhiteList = App.MySecurityList.extend({});
// App.MyBlackList = App.MySecurityList.extend({});


App.Wikipedia = DS.Model.extend({
    label: DS.attr('string'),
    content: DS.attr('string'),
    edges: DS.hasMany('edge'),
    humanName: function () {
        var temp = this.get('label');
        if (temp)
            return ToTitleCase(temp.replace(/_/g, ' '));
        else
            return null;
    }.property()
});


App.WikipediaRoute = Ember.Route.extend({
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
            encodedTitle: encodeURIComponent(params.id.replace(/ /ig, "_"))
        });
    },
    afterModel: function (m) {
        var sel = m.selected;
        var array = { nodes: [], edges: [] };
        var depthMax = 1; // currently depthMax is limited to 1 unless the data is already in ember store
        var nodeMax = 25;
        var data = recurseGraphData(sel, array, this, 1, depthMax, nodeMax, 'wikipedia');
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
        }
    }
});


App.WikipediaController = Ember.ObjectController.extend({
    changeSelected: function () {
        //console.log('Selection changed, should redirect!')
        this.transitionToRoute('wikipedia', this.get('model.selected'));
    }.observes('model.selected'),
    watchSearch: function () {
        
        var title = encodeURIComponent(this.get('model.title').replace(/ /ig, "_"));
        if (encodeURIComponent(this.get('selected').replace(/ /ig, "_")) !== title || title !== this.get('model.encodedTitle')) {
            this.transitionToRoute('wikipedia', title);
        }
    }.observes('model.title')
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
                jQuery.getJSON("http://query.yahooapis.com/v1/public/yql?" +
                   "q=select%20content%20from%20data.headers%20where%20url%3D%22" +
                   encodeURIComponent(randomURL) +
                   "%22&format=json&diagnostics=true&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys&callback=?"
                 ).then(function (data) {
                     Ember.run(null, reject, { redirect: 'wikipedia', id: data.query.results.resources.content.query.random.title });
                 }, function (jqXHR) {
                     jqXHR.then = null; // tame jQuery's ill mannered promises
                     Ember.run(null, reject, jqXHR);
                 });
            });
        }
        else
        return new Ember.RSVP.Promise(function (resolve, reject) {
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
                              edges.push({ id: id + '-' + leaf, from: id, to: leaf.replace(/ /g, '_') });
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
                  Enumerable.From(edges).Where("$.to!='" + id.replace("'", "\\\'") + "'").ForEach(function (f) { App.Wikipedia.store.push('wikipedia', { id: f.to, label: f.to }); });
                  App.Wikipedia.store.push('wikipedia', { id: id, label: id, edges: edgeids, content: content });
                  if (typeof array === 'undefined')
                      Ember.run(null, resolve, { id: id, label: id, content: content, edges: edgeids });
                  else {
                      var toReturn = { Nodes: [{ id: id, label: id, content: content, edges: edgeids }], Edges: edges };
                      Ember.run(null, resolve, toReturn);
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



Ember.Handlebars.helper('wikiurl', function (item, options) {

});


Ember.TextField.reopen({
    attributeBindings: ['autofocus', 'style'],
    autofocus: 'autofocus',
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
        $(window).on('hashchange', function () {
            _this.set('activeTagzz', false);
            _this.$('a').each(function (i, j, y) {
                if (j.getAttribute('href').replace(/^#\//, '') === window.location.hash.substring(2)) {
                    _this.set('activeTagzz', true);
                }
            });
        }).trigger('hashchange')
    }.on('didInsertElement')
});


App.TinymceEditorComponent = Ember.Component.extend({
    // Warning!!! only use tinyMCE not tinymce !!!
    editor: null,
    data: {},
    watchData: true,
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
        config.plugins = ["locationpicker myfilepicker code"];
        config.toolbar = ["undo redo | styleselect | bold italic | alignleft aligncenter alignright | bullist numlist outdent indent code | locationpicker | myfilepicker"];
        config.schema = "html5";
        config.menubar = false;
        config.valid_elements = "*[*]";
        config.extended_valid_elements = "script[type|defer|src|language]";
        // Choose selector
        config.selector = "#" + _this.get("elementId");
        config.convert_urls = false;
        config.extended_valid_elements = "script[type|defer|src|language]";

        // Setup what happens on data changes
        config.setup = function (editor) {
            editor.on('change', function (e) {
                var newData = e.level.content;
                _this.set('watchData', false);
                if (newData) _this.set('data', newData);
                _this.set('watchData', true);
            });
        }

        // Set content once initialized
        config.init_instance_callback = function (editor) {
            _this.update();
            //resize();
        }

        tinyMCE.init(config);

    },
    update: function () {
        if (this.get('watchData')) {
            var content = this.get('data');
            if (content && tinyMCE.activeEditor !== null) {
                tinyMCE.activeEditor.setContent(content);
            }
        }
    }.observes('data')
});
