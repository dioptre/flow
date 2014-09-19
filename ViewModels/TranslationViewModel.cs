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
    public class TranslationViewModel 
    {
        public string TranslationCulture { get; set; }
        public string TranslationText { get; set; }
        public string TranslationName { get; set; }
        public TranslationViewModel translation { get; set; }

        public Guid? id { get { return TransalationDataID; } set { TransalationDataID = value; } }
        [JsonIgnore]
        [HiddenInput, Required, DisplayName("Translation ID:")]
        public Guid? TransalationDataID { get; set; }

        public string DocType { get; set;}
        [JsonIgnore]
        public SearchType SearchType { get; set; }
        [JsonIgnore]
        public string TableType {get;set;}
        
        public Guid? DocID { get {return ReferenceID;} set { ReferenceID = value;}}   
        [JsonIgnore]
        [HiddenInput, Required, DisplayName("Reference ID:")]
        public Guid? ReferenceID {get;set;}
        
        public string DocName { get {return ReferenceName;} set {ReferenceName = value;}}
        [JsonIgnore]
        [HiddenInput, Required, DisplayName("Reference Name:")]
        public string ReferenceName {get;set;}
      
        public string OriginCulture {get;set;}


    }

}