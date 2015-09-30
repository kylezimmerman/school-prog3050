using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Veil.DataAccess;
using Veil.Models;

namespace Veil.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            VeilDataContext db = new VeilDataContext();

            List<Member> members = db.Members.ToList();

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}