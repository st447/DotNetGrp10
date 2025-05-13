using Microsoft.Owin;
using Owin;
using Microsoft.AspNet.SignalR;

[assembly: OwinStartupAttribute(typeof(Health_Care_MIS.Startup))]
namespace Health_Care_MIS
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Configure SignalR
            app.MapSignalR();
            
            // Configure Authentication
            ConfigureAuth(app);
        }
    }
}
