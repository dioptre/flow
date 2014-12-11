using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NKD.Module.BusinessObjects;
using NKD.Helpers;

namespace EXPEDIT.Flow.ViewModels
{

    [JsonObject]
    public class CompanyViewModel
    {
        public Guid? id { get; set; }
        public Guid? CompanyID {get { return id; } set {id = value;} }

        private Guid? _parentCompanyID { get; set; }
        public bool IsParentCompanyUpdated { get; set; }
        public Guid? ParentCompanyID { get { return _parentCompanyID; } set { IsParentCompanyUpdated = true; _parentCompanyID = value; } }
        public string CompanyName {get; set;}
        public string CountryID {get; set;}
        public Guid? PrimaryContactID {get; set;}
        public string Comment {get; set;}
        public Guid? Owner {get; set;}
        public bool IsPeopleSet { get; set; }
        private Guid?[] _people;
        public Guid?[] People { get { return _people; } set { IsPeopleSet = true; _people = value; } }
        public Guid?[] Experiences {get; set;}

       
        
        public string JSON { get; set; }

        public string Error { get; set; }


        public CompanyViewModel company { get; set; }

        public CompanyViewModel[] companies { get; set; }

    }



}