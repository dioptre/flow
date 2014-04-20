using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace EXPEDIT.Flow.ViewModels
{

    public enum SearchType
    {
        Model,
        File,
        Flow
    }

    public interface IFlow
    {
        Guid? GraphDataID { get; set; }
        string GraphName { get; set; }
        string GraphData { get; set; }
      
    }

    [JsonObject]
    public class FlowViewModel : IFlow
    {
        public Guid? GraphDataID { get; set; }
        public string GraphName { get; set; }
        public string GraphData { get; set; }
    }

    [JsonObject]
    public class FlowViewModelDetailed : FlowViewModel
    {
        public IEnumerable<FlowEdgeViewModel> Parents { get; set; }
        public IEnumerable<FlowEdgeViewModel> Children { get; set; }
    }

    [JsonObject]
    public class FlowEdgeViewModel
    {
        public Guid? GraphDataRelationID { get; set; }
        public Guid? GroupID { get; set; }
        public Guid? FromID { get; set; }
        public Guid? ToID { get; set; }
        public decimal? Weight { get; set; }
        public Guid? RelationTypeID { get; set; }
        public int? Sequence { get; set; }

    }

    [JsonObject]
    public class FlowGroupViewModel
    {
        public IEnumerable<FlowViewModel> Nodes { get; set; }
        public IEnumerable<FlowEdgeViewModel> Edges { get; set; }
    }

}