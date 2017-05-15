using FreeMarket.Models;
using System.Linq;
using System.Web.Mvc;

namespace FreeMarket.Controllers
{
    [RequireHttps]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            WelcomeViewModel model = new WelcomeViewModel();

            return View(model);
        }

        public ActionResult Gallery()
        {
            return View();
        }

        public ActionResult About()
        {
            AboutViewModel model = new AboutViewModel();

            return View(model);
        }

        public ActionResult Contact()
        {
            ContactUsViewModel model = new ContactUsViewModel();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SendEmail(ContactUsViewModel model)
        {
            if (ModelState.IsValid)
            {
                EmailService service = new EmailService();
                service.SendAsync(model.FromEmail, model.DestinationEmail, model.Message);

                return View("ThankYouForYourEmail");
            }

            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                model.SupportInfo = db.Supports.FirstOrDefault();
            }

            return View("Contact", model);
        }

        public ActionResult TermsAndConditionsModal()
        {
            string terms = "";
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                SiteConfiguration temp = db.SiteConfigurations
                    .Where(c => c.Key == "TermsAndConditions")
                    .FirstOrDefault();

                if (temp != null)
                    terms = temp.Value;
            }

            return PartialView("_TermsAndConditionsModal", terms);
        }

        public ActionResult TermsAndConditions()
        {
            TermsAndConditionsViewModel model = new TermsAndConditionsViewModel();
            using (FreeMarketEntities db = new FreeMarketEntities())
            {
                SiteConfiguration temp = db.SiteConfigurations
                    .Where(c => c.Key == "TermsAndConditions")
                    .FirstOrDefault();

                if (temp != null)
                    model.Content = temp.Value;
            }

            return View("TermsAndConditions", model);
        }

        public ActionResult Privacy()
        {
            return View("Privacy");
        }

        [ChildActionOnly]
        public ActionResult SpecialMessage()
        {
            SpecialMessageViewModel model = new SpecialMessageViewModel();

            return PartialView("_SpecialMessage", model);
        }
    }
}