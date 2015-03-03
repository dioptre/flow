/**
 * @class Images
 * This class loads images and keeps them stored.
 */
Images = function () {
  this.images = {};

  this.callback = undefined;
};

/**
 * Set an onload callback function. This will be called each time an image
 * is loaded
 * @param {function} callback
 */
Images.prototype.setOnloadCallback = function(callback) {
  this.callback = callback;
};

/**
 *
 * @param {string} url          Url of the image
 * @return {Image} img          The image object
 */
Images.prototype.load = function(url) {
  var img = this.images[url];
  if (img == undefined) {
    // create the image
    var images = this;
    img = new Image();
    this.images[url] = img;
    img.onload = function() {
      if (images.callback) {
        images.callback(this);
      }
    };
    img.src = url;
  }

  return img;
};
