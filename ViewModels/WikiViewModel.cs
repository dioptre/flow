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
    public class WikiViewModel : IFlow
    {
        public Guid? GraphDataID { get; set; }
        public Guid? GraphGroupID {get;set;}
        [DisplayName("Page Name")]
        public string GraphName { get; set; }
        [DisplayName("Wiki")]
        public string GraphData { get; set; }
        public bool IsDuplicate { get; set; }
        public bool IsNew { get; set; }
        public Guid?[] edges { get; set; }
        public Guid?[] workflows { get; set; }
      
    }
}