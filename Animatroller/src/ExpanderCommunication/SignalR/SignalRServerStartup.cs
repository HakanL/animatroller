using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Owin;
using Microsoft.AspNet.SignalR;

namespace Animatroller.ExpanderCommunication
{
    public class SignalRServerStartup
    {
        public void Configuration(IAppBuilder app)
        {
            GlobalHost.HubPipeline.AddModule(new SignalRErrorHandlingPipelineModule());

            var hubConfiguration = new HubConfiguration
            {
                EnableJSONP = true,
                EnableDetailedErrors = true
            };

            app.MapSignalR(hubConfiguration);
        }
    }
}
