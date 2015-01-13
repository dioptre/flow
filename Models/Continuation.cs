using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EXPEDIT.Flow.Models
{
    public class Continuation
    {

        [Flags]
        public enum RelationshipType : uint 
        {
            Parent = 0,
            Peer = 1,
            Child = 2
        }

        public Guid? OldStepID { get; set; }
        
        //Single
        public Guid? NewCompanyID {get;set;}
        public Guid? NewContactID {get;set;}
        public Guid? NewWorkflowID { get; set; }

        //Multiple
        public int? CompanyLevel { get; set; }
        public uint? Relationship { get; set; } //*REQUIRED: Parent,Peer,Child as above

    }
}