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
            Child = 0, //nCr like students to tutor
            Peer = 1, //nCr peer review
            Parent = 2, //nCr like tutor to students
            Self = 3 //like a class test
        }
        public Guid? OldWorkflowID { get; set; }
        public Guid? OldWorkflowCompanyID { get; set; }
        public Guid? OldWorkflowContactID { get; set; }
        public Guid? OldStepID { get; set; }
        public Guid? OldStepCompanyID { get; set; }
        public Guid? OldStepContactID { get; set; }

        //Single
        public Guid? NewCompanyID {get;set;}
        public Guid? NewContactID {get;set;}
        public Guid? NewWorkflowID { get; set; }

        //Multiple
        public int? CompanyLevel { get; set; }
        public uint? Relationship { get; set; } //*REQUIRED: Parent,Peer,Child as above

    }
}