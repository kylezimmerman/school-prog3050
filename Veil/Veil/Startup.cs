/* Startup.cs
 * Purpose: 
 * 
 * Revision History:
 *      Drew Matheson, 2015.09.25: Created
 */ 

using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof (Veil.Startup))]

namespace Veil
{
    /// <summary>
    ///     OWIN startup and configuration class
    /// </summary>
    public partial class Startup
    {
        /// <summary>
        ///     Configures the OWIN usage within the app
        /// </summary>
        /// <param name="app">
        ///     The <see cref="IAppBuilder"/> to configure
        /// </param>
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}