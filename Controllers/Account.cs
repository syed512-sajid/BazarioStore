using Microsoft.AspNetCore.Mvc;

namespace EcommerceStore.Controllers
{
    public class Account : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
