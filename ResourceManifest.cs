using Orchard.UI.Resources;

namespace EXPEDIT.Flow {
    public class ResourceManifest : IResourceManifestProvider {
        public void BuildManifests(ResourceManifestBuilder builder) {
            builder.Add().DefineStyle("Flow").SetUrl("expedit-flow.css");
            builder.Add().DefineScript("Flow").SetUrl("flow.js");
        }
    }
}
