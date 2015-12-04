/* TagsController.cs
 * Purpose: Controller for displaying checkboxes for all Tags
 * 
 * Revision History:
 *      Kyle Zimmerman, 2015.11.03: Created
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
    ///     Controller for displaying checkboxes for all Tags
    /// </summary>
    public class TagsController : BaseController
    {
        private readonly IVeilDataAccess db;

        /// <summary>
        ///     Instantiates a new instance of TagsController with the provided arguments
        /// </summary>
        /// <param name="veilDataAccess">
        ///     The <see cref="IVeilDataAccess"/> to use for database access
        /// </param>
        public TagsController(IVeilDataAccess veilDataAccess)
        {
            db = veilDataAccess;
        }

        /// <summary>
        ///     Produces a partial view containing a checkbox for each Tag.
        /// </summary>
        /// <param name="selected">
        ///     The list of tags to set as selected
        /// </param>
        /// <returns>
        ///     The partial view to be rendered
        /// </returns>
        [ChildActionOnly]
        public PartialViewResult Index(List<Tag> selected)
        {
            var tagsViewModel = new TagViewModel()
            {
                AllTags = db.Tags.ToList(),
                Selected = selected
            };

            return PartialView(tagsViewModel);
        }
    }
}