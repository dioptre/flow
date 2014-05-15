Hi, I hope you find this theme useful!

Here are some docs and info to get started.


CSS folder structure:

/bootstrap: the original css bootstrap files, if you want to change versions, just drop and replace them here and put your overrides in _bootstrap-overrides.scss.

/vendor: in here are all the plugins, libraries and external stuff used throughout the theme.

/modules: contains reusable element styles like new buttons, form control styles, etc plus the mixins library with some cool helpers used for the theme.

/partials:
	/pages: contains all the pages styles separated by html page, each one is uniquely contained by adding an ID into the body of their respective page, like a namespace.

	/snippets: in here are blocks of styles for html sections that can be reusable in different pages, e.g. the Screenshots Slider used in the landing page and in the features page reuse the same styles located in snippets.

	_config.scss: is used to import fonts and apply variables used throughout the scss files.
	
	_layout.scss: contains the styles that apply for the layout of the theme, in this case the navbar header and the footer.


Everything is imported onto the theme.scss file using the @import statement except for vendor files (these are referenced explicitly in each page when needed). The compiled file for the theme.scss is located under compiled/theme.css, this way the html pages only reference a single file which makes it easier to maintain.

If you prefer to use normal css, there's the expanded-theme.css which has all the styles in an expanded format, it's easy to edit styles thanks to the body ID that identifies each page.


************


Fonts folder:
contains all the font files used in the theme:
	- Brankic icon set
	- Entypo icon set
	- Flexslider icon set
	- Font awesome set
	- Glyphicons set
	- Icomoon set
	- Ionicons set


************


Images folder: images used in the theme.


************

JS folder:	

/bootstrap: the original bootstrap js files, to change versions replace them here with the new ones.

/vendor: all the js libraries and plugins used.


************

Icons folder:

Includes the downloaded files from http://icomoon.io/app for the Brankic, Entypo and Icomoon free icon fonts set:
Brankic: http://www.brankic1979.com/icons/
Icomoon: http://icomoon.io/#icons
Entypo: http://www.entypo.com/

You don't need any of this, as the css font files are already in the vendor folder, but you can use the demo.html in each folder to see all of the icons included and how to reference them in css classes.


************

Background uses:

For large background images, you can find some really cool free to use at 
- http://unsplash.com
- http://imcreator.com/free
- http://superfamous.com
- http://littlevisuals.co
- http://compfight.com
- http://picjumbo.com/

You can also use https://placeit.net to drop your screenshots into devices and get the large background pics for your site.


************

Credits:

- unsplash.com for all the bg pictures.
- imcreator.com/free for background pictures.
- uifaces.com/authorized for the user display pics.
- daneden.me/animate for the animate.css library.
- fontawesome.io
- isotope.metafizzy.co plugin for the gallery.
- sketchappsources.com for all the awesome resources for devices, mobile, browsers and such.
- graphicburger.com for all the awesome resources and icons.
- entypo.com
- brankic1979.com/icons
- ionicons.com
- elegantthemes.com for the Flat Circle Icons found at graphicburger.com/90-beautiful-flat-icons 
- flipclockjs.com for FlipClock countdown plugin
- github.com/jzaefferer/jquery-validation for the jquery validate plugin


************

Changelog

v1.2
- Update to latest Bootstrap version
- Added a new more complete footer and 'Our clients' section to Home page and alts
- Changed hero unit design from Home page
- Changed slide styles and new option of a white navbar in Home page alt #2
- Added Timeline page
- Added Invoice page
- Added a complete ready to use API Documentation page
- Added Coming Soon page
- Added 404 page with 1 alt
- Added jquery validate plugin with example on Contact us form

v1.0
- Initial release


*** 

For any help, doubts, suggestions, ideas for new features/pages to keep improving this theme or just want to say hi you can contact me at e.adrian90 at gmail.com

