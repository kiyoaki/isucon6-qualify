using Microsoft.AspNetCore.Mvc;

namespace isutar.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
