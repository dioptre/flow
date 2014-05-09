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
        Flow,
        FlowLocation
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
        public FlowViewModel node { get; set; }
        public Guid? id { get { return GraphDataID; } set { GraphDataID = value; } }
        public string label { get { return GraphName; } set { GraphName = value; } }
        public string content { get { return GraphData; } set { GraphData = value; } }

        [JsonIgnore]
        public Guid? GraphDataID { get; set; }
        [JsonIgnore]
        public string GraphName { get; set; }
        [JsonIgnore]
        public string GraphData { get; set; }
    }

    [JsonObject]
    public class FlowViewModelDetailed : FlowViewModel
    {
        public Guid?[] edges { get { return (Parents != null ? (from o in Parents select o.GraphDataRelationID).ToArray() : new Guid?[] {}); } set { edges = value; } }
        [JsonIgnore]
        public IEnumerable<FlowEdgeViewModel> Parents { get; set; }
        [JsonIgnore]
        public IEnumerable<FlowEdgeViewModel> Children { get; set; }
    }

    [JsonObject]
    public class FlowEdgeViewModel
    {
        public FlowEdgeViewModel edge { get; set; }
        public Guid? id { get { return GraphDataRelationID; } set { GraphDataRelationID  = value; } }
        public Guid? from { get { return FromID; } set { FromID = value; } }
        public Guid? to { get { return ToID; } set { ToID = value; } }
        [JsonIgnore]
        public Guid? GraphDataRelationID { get; set; }
        public Guid? GroupID { get; set; }
        [JsonIgnore]
        public Guid? FromID { get; set; }
        [JsonIgnore]
        public Guid? ToID { get; set; }
        public decimal? Weight { get; set; }
        public Guid? RelationTypeID { get; set; }
        public int? Sequence { get; set; }
        [JsonIgnore]
        public IEnumerable<FlowEdgeWorkflowViewModel> Workflows { get; set; }

    }


    [JsonObject]
    public class FlowEdgeWorkflowViewModel
    {
        public FlowEdgeWorkflowViewModel workflow { get; set; }
        public Guid? id { get { return GraphDataGroupID; } set { GraphDataGroupID = value; } }
        public string name { get { return GraphDataGroupName; } set { GraphDataGroupName = value; } }
        [JsonIgnore]
        public string GraphDataGroupName { get; set; }
        [JsonIgnore]
        public Guid? GraphDataGroupID { get; set; }
        [JsonIgnore]
        public string Comment { get; set; }

    }

    [JsonObject]
    public class FlowGroupViewModel
    {
        public IEnumerable<FlowViewModelDetailed> Nodes { get; set; }
        public IEnumerable<FlowEdgeViewModel> Edges { get; set; }
    }

}