$(function($){

  $.fn.reduce = function(callback, initial){
      return Array.prototype.reduce.call(this, callback, initial);
    }

  $('.input input').on('focusout',function(){
    var txt= this.value.replace(/[^a-zA-Z0-9\+\-\.\#]/g,''); // allowed characters
    if(txt) {
      $(this).before('<span class="tag">'+ txt.toLowerCase() +'</span>');
      fitInput();
    }
    this.value="";
  }).on('keyup',function( e ){
    // if: comma,enter (delimit more keyCodes with | pipe)
    if(/(188|13)/.test(e.which)) $(this).focusout();

  });


  $('.input').on('click','.tag',function(){
     if(confirm("Really delete this tag?")) $(this).remove();
  });

  fitInput();
  function fitInput() {
    var $input = $('.input input');

    var parentLength = $input.parent().innerWidth();

    var tagsLength = $('.input span.tag').reduce(
        function(pV, cV, i, a){
              return pV + $(cV).outerWidth(true);
         }, 0)

     // Set input.length = parent - tags
     $input.width(parentLength - tagsLength - 32);

  }

});