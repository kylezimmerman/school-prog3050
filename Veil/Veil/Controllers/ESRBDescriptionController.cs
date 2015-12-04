/* ESRBDescriptionController.cs
 * Purpose: Controller for displaying checkboxes for all ESRB Content Descriptors
 * 
 * Revision History:
 *      Kyle Zimmerman, 2015.11.27: Created
 */ 

using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Models;

namespace Veil.Controllers
{
    /// <summary>
    ///     Controller for displaying checkboxes for all ESRB Content Descriptors
    /// </summary>
    public class ESRBDescriptionController : Controller
    {
        private readonly IVeilDataAccess db;

        /// <summary>
        ///     Instantiates a new instance of ESRBDescriptionController with the provided arguments
        /// </summary>
        /// <param name="veilDataAccess">
        ///     The <see cref="IVeilDataAccess"/> to use for database access
        /// </param>
        public ESRBDescriptionController(IVeilDataAccess veilDataAccess)
        {
            db = veilDataAccess;
        }

        /// <summary>
        ///     Produces a partial view containing a checkbox for each ESRB Content Descriptor.
        /// </summary>
        /// <param name="selected">
        ///     The list of descriptors to set as selected
        /// </param>
        /// <returns>
        ///     The partial view to be rendered
        /// </returns>
        [ChildActionOnly]
        public PartialViewResult Index(List<ESRBContentDescriptor> selected)
        {
            var viewModel = new ESRBDescriptionViewModel
            {
                All = db.ESRBContentDescriptors.ToList(),
                Selected = selected
            };

            return PartialView(viewModel);
        }
    }
}