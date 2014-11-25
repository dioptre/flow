using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using JetBrains.Annotations;
using Orchard.ContentManagement;
using Orchard.FileSystems.Media;
using Orchard.Localization;
using NKD.Models;
using Orchard.Security;
using Orchard.Settings;
using Orchard.Validation;
using Orchard;
using Orchard.Media.Models;
using Orchard.Media.Services;
using System.Transactions;
using Orchard.Logging;
using NKD.ViewModels;
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using NKD.Helpers;
using Orchard.Tasks.Scheduling;

using System.Runtime.DurableInstancing;
using System.ServiceModel.Activities.Description;
using System.Activities.DurableInstancing;
using System.Activities;
using NKD.Workflow;
using NKD.Workflow.Helpers;
using System.Xml.Linq;
using System.Activities.XamlIntegration;
using System.Activities.Tracking;
using NKD.Services;

namespace EXPEDIT.Flow.Services
{

    [UsedImplicitly]
    public class MetadataWorkflowService : IMetadataWorkflowService
    {

        private static readonly XName workflowHostTypePropertyName = XNamespace.Get("urn:schemas-microsoft-com:System.Activities/4.0/properties").GetName("WorkflowHostType");
        
        private readonly IUsersService _users;
        private readonly WorkflowApplication _wfApp;

        public MetadataWorkflowService(
            IUsersService users

          )
        {
            _users = users;
            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
            SqlWorkflowInstanceStore store = new SqlWorkflowInstanceStore(_users.ApplicationConnectionString);
            _wfApp = new WorkflowApplication(new NKD.Workflow.AssignMetadata());
            _wfApp.InstanceStore = store;
            
            XName wfHostTypeName = XName.Get("NKD", _users.ApplicationID.ToString());
            Dictionary<XName, object> wfScope = new Dictionary<XName, object> { { workflowHostTypePropertyName, wfHostTypeName } };
            _wfApp.AddInitialInstanceValues(wfScope);
            
            _wfApp.Extensions.Add(new MetadataExtension());
            List<XName> variantProperties = new List<XName>() 
            { 
                MetadataExtension.xNS.GetName("CompanyID"), 
                MetadataExtension.xNS.GetName("ContactID"),
                MetadataExtension.xNS.GetName("IsCompleted"),
                MetadataExtension.xNS.GetName("ReferenceClass"),
                MetadataExtension.xNS.GetName("ReferenceTable"),
                MetadataExtension.xNS.GetName("ReferenceID"),
                MetadataExtension.xNS.GetName("RecordState")
                       
            };
            store.Promote("Metadata", variantProperties, null);

            InstanceHandle handle = store.CreateInstanceHandle(null);
            var cmd = new CreateWorkflowOwnerCommand
            {
                InstanceOwnerMetadata =
                    {
                        {workflowHostTypePropertyName, new InstanceValue(wfHostTypeName)}
                    }
            };
            InstanceOwner owner = store.Execute(handle, cmd, TimeSpan.MaxValue).InstanceOwner;
            store.DefaultInstanceOwner = owner;
            
            handle.Free();
   
            _wfApp.PersistableIdle = delegate(WorkflowApplicationIdleEventArgs e)
            {
                return PersistableIdleAction.Persist;
            };

            _wfApp.Completed = delegate(WorkflowApplicationCompletedEventArgs e)
            {
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    foreach (var item in e.Outputs)
                    {
                        System.Diagnostics.Debug.WriteLine("Variable:{0} has value: {1}", item.Key, item.Value);
                    }
                }
            };

            var trackingParticipant = new TrackingHelper.DebugTrackingParticipant
            {
                TrackingProfile = TrackingHelper.SimpleProfile
            };
            _wfApp.Extensions.Add(trackingParticipant);
        }

        public Localizer T { get; set; }
        public ILogger Logger { get; set; }
                

        private static Activity LoadActivity(string path)
        {
            using (StringReader rdr = new StringReader(string.Join("\n", File.ReadAllLines(path))))
            {
                return ActivityXamlServices.Load(rdr);
            }
        }

        public Guid AssignMetadata(Guid? tryWorkflowID, Dictionary<string,object> lookup)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                if (tryWorkflowID.HasValue && tryWorkflowID.Value != default(Guid))
                    _wfApp.Load(tryWorkflowID.Value);
                //else
                //    _wfApp.LoadRunnableInstance(); // if any in SQL store
                _wfApp.Run();
                //var b = _wfApp.GetBookmarks();
                var r = _wfApp.ResumeBookmark("SubmitMetadata", lookup);
                
                //Todo: remove
                _wfApp.Unload();
                //WorkflowInvoker.Invoke(
                
                
            }
            return _wfApp.Id; //Real Workflow ID
        }

        public string CurrentState
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public T GetMetadata<T>(Guid workflowID, string lookup)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress))
            {
                if (workflowID != default(Guid))
                {
                    _wfApp.Load(workflowID);

                    _wfApp.Run();
                    //var result = WorkflowInvoker.Invoke(_wfApp,);
                    //return (T)_wfApp.InstanceStore...scope.GetExtension<MetadataExtension>().ReferenceClass
                }
                    
                //WorkflowInvoker.Invoke()

            }
            return default(T);
        }

        public bool CompleteProcess(Guid companyID, Guid contactID, Guid workflowID)
        {
            return false;
        }


       
    }
}
