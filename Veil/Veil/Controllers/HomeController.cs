using System.Web.Mvc;
using Stripe;
using Veil.DataAccess.Interfaces;

namespace Veil.Controllers
{
    public class HomeController : Controller
    {
        private readonly IVeilDataAccess db;

        public HomeController(IVeilDataAccess dataAccess)
        {
            db = dataAccess;
        }

        public ActionResult Index()
        {
            /*
            var myCustomer = new StripeCustomerCreateOptions();

            // set these properties if it makes you happy
            myCustomer.Email = "pork@email.com";
            myCustomer.Description = "Johnny Tenderloin (pork@email.com)";

            // setting up the card
            myCustomer.Source = new StripeSourceOptions()
            {
                Number = "4242424242424242",
                ExpirationYear = "2022",
                ExpirationMonth = "1",
                AddressCountry = "CA", // optional
                AddressLine1 = "445 Wes Graham Way", // optional
                //AddressLine2 = "Apt 24", // optional
                AddressCity = "Waterloo", // optional
                AddressState = "ON", // optional
                AddressZip = "N2L 6R2", // optional
                Name = "Joe Meatballs", // optional
                Cvc = "1223", // optional
                Object = "card"
            };

            var customerService = new StripeCustomerService();

            customerService.Create(myCustomer);
            */

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