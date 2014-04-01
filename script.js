$(function($){

  // Extend jQuery to support reduce function
  $.fn.reduce = function(callback, initial){
      return Array.prototype.reduce.call(this, callback, initial);
    }


  $('.add-location').click(function(){
    bootbox.dialog({
      message: "Insert html for location filter here!<input class='location'/>",
      title: "Add a filter by location.",
      buttons: {
        default: {
          label: "Cancel",
          className: "btn-default"
        },
        success: {
          label: "Insert Location Filter",
          className: "btn-success",
          callback: function() {
              var txt = $('input.location').val();
              $('.input input').before('<span class="tag">'+ txt +'</span>');
              fitInput();
          }
        }
      }
    });
  })




  // $('.input input').on('focusout',function(){
  //   var txt= this.value.replace(/[^a-zA-Z0-9\+\-\.\#]/g,''); // allowed characters
  //   if(txt) {
  //     $(this).before('<span class="tag">'+ txt.toLowerCase() +'</span>');
  //     fitInput();
  //   }
  //   this.value="";
  // }).on('keyup',function( e ){
  //   // if: comma,enter (delimit more keyCodes with | pipe)
  //   if(/(188|13)/.test(e.which)) $(this).focusout();

  // });


  $('.input').on('click','.tag',function(el){
     bootbox.confirm("Really delete this tag?", function(result){
        if (result) {
          $(el.toElement).remove(); // Delete the tag clicked
          fitInput();
        }
     })
  });


  // This function is used to adjust size of input box depending on tags
  function fitInput() {

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

  };

  // Run on initalize
  fitInput();

});