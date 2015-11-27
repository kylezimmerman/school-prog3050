using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Veil.DataAccess.Interfaces;
using Veil.DataModels.Models;
using Veil.Models;

namespace Veil.Controllers
{
    public class ESRBDescriptionController : Controller
    {
        protected readonly IVeilDataAccess db;

        public ESRBDescriptionController(IVeilDataAccess veilDataAccess)
        {
            db = veilDataAccess;
        }

        [ChildActionOnly]
        public PartialViewResult Index(List<ESRBContentDescriptor> selected )
        {
            var viewModel = new ESRBDescriptionViewModel
            {
                All = db.ESRBContentDescriptors,
                Selected = selected
            };

            return PartialView(viewModel);
        }
    }
}