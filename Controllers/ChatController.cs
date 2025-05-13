using System.Web.Mvc;

namespace Health_Care_MIS.Controllers
{
    [Authorize(Roles = "user,Doctor")]
    public class ChatController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}
