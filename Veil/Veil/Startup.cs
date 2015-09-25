using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Veil.Startup))]
namespace Veil
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
