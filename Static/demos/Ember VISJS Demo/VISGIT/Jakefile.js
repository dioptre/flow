/**
 * Jake build script
 */
var jake = require('jake'),
    browserify = require('browserify'),
    wrench = require('wrench'),
    CleanCSS = require('clean-css'),
    fs = require('fs');

require('jake-utils');

// constants
var DIST = './dist';
var VIS = DIST + '/vis.js';
var VIS_CSS = DIST + '/vis.css';
var VIS_TMP = DIST + '/vis.js.tmp';
var VIS_MIN = DIST + '/vis.min.js';
var VIS_MIN_CSS = DIST + '/vis.min.css';

/**
 * default task
 */
desc('Default task: build all libraries');
task('default', ['build', 'minify'], function () {
  console.log('done');
});

/**
 * build the visualization library vis.js
 */
desc('Build the visualization library vis.js');
task('build', {async: true}, function () {
  jake.mkdirP(DIST);
  jake.mkdirP(DIST + '/img');

  // concatenate and stringify the css files
  concat({
    src: [
      './src/timeline/component/css/timeline.css',
      './src/timeline/component/css/panel.css',
      './src/timeline/component/css/groupset.css',
      './src/timeline/component/css/itemset.css',
      './src/timeline/component/css/item.css',
      './src/timeline/component/css/timeaxis.css',
      './src/timeline/component/css/currenttime.css',
      './src/timeline/component/css/customtime.css',

      './src/graph/css/graph-manipulation.css',
      './src/graph/css/graph-navigation.css'
    ],
    dest: VIS_CSS,
    separator: '\n'
  });
  console.log('created ' + VIS_CSS);

  // concatenate the script files
  concat({
    dest: VIS_TMP,
    src: [
      './src/module/imports.js',

      './src/shim.js',
      './src/util.js',
      './src/DataSet.js',
      './src/DataView.js',

      './src/timeline/TimeStep.js',
      './src/timeline/Stack.js',
      './src/timeline/Range.js',
      './src/timeline/Controller.js',
      './src/timeline/component/Component.js',
      './src/timeline/component/Panel.js',
      './src/timeline/component/RootPanel.js',
      './src/timeline/component/TimeAxis.js',
      './src/timeline/component/CurrentTime.js',
      './src/timeline/component/CustomTime.js',
      './src/timeline/component/ItemSet.js',
      './src/timeline/component/item/*.js',
      './src/timeline/component/Group.js',
      './src/timeline/component/GroupSet.js',
      './src/timeline/Timeline.js',

      './src/graph/dotparser.js',
      './src/graph/shapes.js',
      './src/graph/Node.js',
      './src/graph/Edge.js',
      './src/graph/Popup.js',
      './src/graph/Groups.js',
      './src/graph/Images.js',
      './src/graph/graphMixins/physics/PhysicsMixin.js',
      './src/graph/graphMixins/physics/HierarchialRepulsion.js',
      './src/graph/graphMixins/physics/BarnesHut.js',
      './src/graph/graphMixins/physics/Repulsion.js',
      './src/graph/graphMixins/HierarchicalLayoutMixin.js',
      './src/graph/graphMixins/ManipulationMixin.js',
      './src/graph/graphMixins/SectorsMixin.js',
      './src/graph/graphMixins/ClusterMixin.js',
      './src/graph/graphMixins/SelectionMixin.js',
      './src/graph/graphMixins/NavigationMixin.js',
      './src/graph/graphMixins/MixinLoader.js',
      './src/graph/Graph.js',

      './src/module/exports.js'
    ],

    separator: '\n'
  });

  // copy images
  wrench.copyDirSyncRecursive('./src/graph/img', DIST + '/img/graph', {
    forceDelete: true
  });
  wrench.copyDirSyncRecursive('./src/timeline/img', DIST + '/img/timeline', {
    forceDelete: true
  });

  var timeStart = Date.now();
  // bundle the concatenated script and dependencies into one file
  var b = browserify();
  b.add(VIS_TMP);
  b.bundle({
    standalone: 'vis'
  }, function (err, code) {
    if(err) {
      throw err;
    }
    console.log("browserify",Date.now() - timeStart); timeStart = Date.now();
    // add header and footer
    var lib = read('./src/module/header.js') + code;

    // write bundled file
    write(VIS, lib);
    console.log('created js' + VIS);

    // remove temporary file
    fs.unlinkSync(VIS_TMP);

    // update version number and stuff in the javascript files
    replacePlaceholders(VIS);

    complete();
  });
});

/**
 * minify the visualization library vis.js
 */
desc('Minify the visualization library vis.js');
task('minify', {async: true}, function () {
  // minify javascript
  minify({
    src: VIS,
    dest: VIS_MIN,
    header: read('./src/module/header.js')
  });

  // update version number and stuff in the javascript files
  replacePlaceholders(VIS_MIN);

  console.log('created minified ' + VIS_MIN);

  var minified = new CleanCSS().minify(read(VIS_CSS));
  write(VIS_MIN_CSS, minified);
  console.log('created minified ' + VIS_MIN_CSS);
});

/**
 * test task
 */
desc('Test the library');
task('test', function () {
  // TODO: use a testing suite for testing: nodeunit, mocha, tap, ...
  var filelist = new jake.FileList();
  filelist.include([
    './test/**/*.js'
  ]);

  var files = filelist.toArray();
  files.forEach(function (file) {
    require('./' + file);
  });

  console.log('Executed ' + files.length + ' test files successfully');
});

/**
 * replace version, date, and name placeholders in the provided file
 * @param {String} filename
 */
var replacePlaceholders = function (filename) {
  replace({
    replacements: [
      {pattern: '@@date',    replacement: today()},
      {pattern: '@@version', replacement: version()}
    ],
    src: filename
  });
};
