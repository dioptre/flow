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
        public Guid? ParentCompanyID { get; set; }
        public string CompanyName {get; set;}
        public string Dashboard { get; set; }
        public string CountryID {get; set;}
        public Guid? PrimaryContactID {get; set;}
        public string Comment {get; set;}
        public Guid? Owner {get; set;}
        private Guid?[] _people { get; set; }
        public IEnumerable<Guid?> PeopleEnum { get { return _people; } set {  _people = value.ToArray(); } }
        public Guid?[] PeopleArray { get { return _people; } set { _people = value; } }
        public string People
        {
            get { if (_people != null) return string.Join(",", _people); else return null; }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var temp = value.Split(',');
                    List<Guid?> pl = new List<Guid?>();
                    Guid tgid;
                    foreach (var t in temp)
                    {
                        if (Guid.TryParse(t, out tgid))
                            pl.Add(tgid);                        
                    }
                    PeopleArray = pl.ToArray();
                }
            }
        }
        public Guid?[] Experiences {get; set;}

       
        
        public string JSON { get; set; }

        public string Error { get; set; }


        public CompanyViewModel company { get; set; }

        public CompanyViewModel[] companies { get; set; }

    }


}