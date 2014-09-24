using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NKD.Helpers;

namespace EXPEDIT.Flow.ViewModels
{
    [JsonObject]
    public class LocaleViewModel 
    {

        public const string DocType = "E_ApplicationLocale";
        
        public LocaleViewModel locale { get; set; }

        public Guid? id { get { return TranslationDataID; } set { TranslationDataID = value; } }
        [JsonIgnore]
        [HiddenInput, Required, DisplayName("Translation ID:")]
        public Guid? TranslationDataID { get; set; }
        [JsonIgnore]
        public string ReferenceName { get { return Label; } set { Label = value; } }
        public string Label {get;set;} //fp.search.info.no_keyword  - (app,where,type.what)
        public string OriginalText { get; set; } // “Please add a keyword to start searching..."
        [JsonIgnore]
        public string TranslationName { get { return OriginalText; } set { OriginalText = value; } }
        public string OriginalCulture {get;set;} // en - always english for now, but just hardcoded for now
        public string Translation {get;set;} //“Bitten ein Suchwort"
        public string TranslationCulture {get;set;} // de
                
        [JsonIgnore]
        public bool Refresh { get; set; }
        public DateTime? VersionUpdated { get; set; }

        [JsonIgnore]
        public LocaleViewModel[] LocaleQueue { get; set; }



    }

  
}