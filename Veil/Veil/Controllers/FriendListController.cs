using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Veil.DataAccess.Interfaces;

namespace Veil.Controllers
{
    public class FriendListController : BaseController
    {
        protected readonly IVeilDataAccess db;

        public FriendListController(IVeilDataAccess veilDataAccess)
        {
            db = veilDataAccess;
        }

        // GET: FriendList
        public ActionResult Index()
        {
            return View();
        }
    }
}