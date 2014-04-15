$(function($){

  // Extend jQuery to support reduce function
  $.fn.reduce = function(callback, initial){
      return Array.prototype.reduce.call(this, callback, initial);
    }

  // Setup location modal
  $('.add-location').click(function(){
    bootbox.dialog({
      message: "Insert html for location filter here!<br><input placeholder='Add your city' class='location'/>",
      title: "Add a location filter.",
      buttons: {
        default: {
          label: "Cancel",
          className: "btn-default"
        },
        success: {
          label: "Apply Location Filter",
          className: "btn-success",
          callback: function() {
              var txt = $('input.location').val();
              $('.input input').before('<span class="tag">'+ txt +'</span>');
              fitInput();
          }
        }
      }
    });
  });

  // Setup date modal

  $('.add-date').click(function(){
    bootbox.dialog({
      message: "Insert jQuery datepicker here.<div class='datepicker1'></div><div class='datepicker2'></div>",
      title: "Add a date filter.",
      buttons: {
        default: {
          label: "Cancel",
          className: "btn-default"
        },
        success: {
          label: "Apply Date Filter",
          className: "btn-success",
          callback: function() {
              var txt = datepicker1.val() + " - " + datepicker2.val();
              $('.input input').before('<span class="tag">'+ txt +'</span>');
              fitInput();
          }
        }
      }
    });
    var datepicker1 = $('.datepicker1').datepicker({
          inline:true,
          defaultDate: "+1w",
          changeMonth: true,
          onClose: function( selectedDate ) {
            $('.datepicker2').datepicker( "option", "minDate", selectedDate );
          }
      });
    var datepicker2 = $('.datepicker2').datepicker({
        inline:true,
        defaultDate: "+1w",
        changeMonth: true,
        onClose: function( selectedDate ) {
          $('.datepicker1').datepicker( "option", "maxDate", selectedDate );
        }
      });
  })


  $('.search').click(search)
  function search() {
    // Get contents from tags
    var tagsContent = $('.input span.tag').reduce(function(pV, cV, i, a){
        return pV + $(cV).html() + " ";
    }, "")

    var inputContent = $('.input input').val();

    console.log('Search started: ', tagsContent, inputContent);
  }


  // Search if the user presses enter.
  $('.input input').on('keyup',function( e ){
        if(/(13)/.test(e.which)) {
          $(this).focusout();
          search();
        }

  });

  // Deleting tags/filters
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