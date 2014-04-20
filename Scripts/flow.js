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