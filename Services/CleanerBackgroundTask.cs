using JetBrains.Annotations;
using Orchard.Logging;
using Orchard.Tasks;
using Orchard;
using Orchard.Localization;
using Orchard.ContentManagement;
using Orchard.Services;
using NKD.Models;
using System.Linq;
using Orchard.Data;
using Orchard.ContentManagement.Handlers;
using Orchard.Caching;
using System;
using NKD.Module.BusinessObjects;

namespace EXPEDIT.Flow.Services {
    /// <summary>
    /// Regularly fires user sync events
    /// </summary>
    [UsedImplicitly]
    public class CleanerBackgroundTask : IBackgroundTask
    {
       
        private IFlowService _flow { get; set; }
        public ILogger Logger { get; set; }
        public Localizer T { get; set; }
        

        public CleanerBackgroundTask(IFlowService flow)
        {
            _flow = flow;
            Logger = NullLogger.Instance;
        }


        public void Sweep()
        {
            try
            {
                _flow.Cleanup();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in CleanerBackgroundTask");
            }
        }
    }
}
