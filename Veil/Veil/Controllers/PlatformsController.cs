/* PlatformsController.cs
 * Purpose: Controller for displaying a list of all Platforms
 * 
 * Revision History:
 *      Isaac West, 2015.11.27: Created
 */ 

using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Models;

namespace Veil.Controllers
{
    /// <summary>
    ///     Controller for displaying checkboxes for a list of all Platforms
    /// </summary>
    public class PlatformsController : BaseController
    {
        private readonly IVeilDataAccess db;

        /// <summary>
        ///     Instantiates a new instance of PlatformsController with the provided arguments
        /// </summary>
        /// <param name="veilDataAccess">
        ///     The <see cref="IVeilDataAccess"/> to use for database access
        /// </param>
        public PlatformsController(IVeilDataAccess veilDataAccess)
        {
            db = veilDataAccess;
        }

        /// <summary>
        ///     Produces a partial view containing a checkbox for each Platform.
        /// </summary>
        /// <param name="selected">
        ///     The list of platforms to set as selected
        /// </param>
        /// <returns>
        ///     The partial view to be rendered
        /// </returns>
        [ChildActionOnly]
        public PartialViewResult Index(List<Platform> selected)
        {
            var platformsViewModel = new PlatformViewModel()
            {
                AllPlatforms = db.Platforms.ToList(),
                Selected = selected
            };

            return PartialView(platformsViewModel);
        }
    }
}