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
    public class ProjectDataViewModel
    {
        public Guid? id { get; set; }
        public string CommonName { get; set; }
        public string UniqueID { get; set; }
        public string UniqueIDSystemDataType { get; set; }
        public string TemplateStructure { get; set; }
        public string TemplateStructureChecksum { get; set; }
        public string TemplateActions { get; set; }
        public string TemplateType { get; set; }
        public string TemplateMulti { get; set; }
        public string TemplateSingle { get; set; }
        public string TableType { get; set; }
        public Guid? ReferenceID { get; set; }
        public string UserDataType { get; set; }
        public string SystemDataType { get; set; }
        public bool? IsReadOnly { get; set; }
        public bool? IsVisible { get; set; }
        public Guid? ProjectDataTemplateID { get; set; }
        public Guid? ProjectID { get; set; }
        public Guid? ProjectPlanTaskResponseID { get; set; }
        public string Value { get; set; }
        public Guid? Project { get { return ProjectID; } }

        public string JSON { get; set; }

        public string Error { get; set; }


        public ProjectDataViewModel projectData { get; set; }

    }


}