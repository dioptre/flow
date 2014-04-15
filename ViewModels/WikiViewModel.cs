using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace EXPEDIT.Flow.ViewModels
{
    [JsonObject]
    public class WikiViewModel 
    {
        public Guid? GraphDataID { get; set; }
        public Guid? GraphGroupID {get;set;}
        [Required, DisplayName("Page Name")]
        public string GraphName { get; set; }
        [Required, DisplayName("Wiki")]
        public string GraphData { get; set; }

        public dynamic Media {get;set;}
      
    }
}