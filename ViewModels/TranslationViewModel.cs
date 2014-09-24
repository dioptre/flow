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
    public class TranslationViewModel 
    {
        public bool Refresh { get; set; }
        public DateTime? VersionUpdated { get; set; }
        public DateTime? DocUpdated { get; set; }
        public string TranslationCulture { get; set; }
        public string OriginalText { get; set; } //Only in Output
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


        [JsonIgnore]
        public Dictionary<Guid,Translation> TranslationQueue { get; set; }
        [JsonIgnore]
        public TranslationViewModel[] TranslationResults { get; set; }


    }

    public class Translation
    {
        public Guid? TranslationDataID { get; set; }
        public string OriginalName { get; set; }
        public string OriginalText { get; set; }
        public DateTime? OriginalUpdated {get;set;}
        public Guid? OriginalContact {get;set;}
        public Guid? OriginalCompany {get;set;}
        public string TranslatedName { get; set; }
        public string TranslatedText { get; set; }
        public DateTime? TranslationUpdated { get; set; }
        public string HumanName { get { return SlugHelper.FromSlug(OriginalName); } }
        public string TranslationCulture { get; set; }

        public Translation(string OriginalName = null, DateTime? OriginalUpdated = null, Guid? OriginalContact = null, Guid? OriginalCompany = null, Guid? TranslationDataID = null, string TranslatedName = null, string TranslatedText = null, DateTime? TranslationUpdated = null, string TranslationCulture = null)
        {
            this.OriginalName = OriginalName;
            this.OriginalUpdated = OriginalUpdated;
            this.OriginalContact = OriginalContact;
            this.OriginalCompany = OriginalCompany;
            this.TranslationDataID = TranslationDataID;
            this.TranslatedName = TranslatedName;
            this.TranslatedText = TranslatedText;
            this.TranslationUpdated = TranslationUpdated;
            this.TranslationCulture = TranslationCulture;

        }
    }

}