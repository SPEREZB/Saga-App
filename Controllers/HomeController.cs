using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Saga_App.Models;

namespace Saga_App.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // GET: /Home/Details  
        //simplemente envimos por defecto un Id=1 y un monto de 10
        public string Details()
        {
            Saga s = new Saga();
            var order = new Order
            {
                Id = 1
            };
            var payment = new Payment
            {
                Amount = 10
            };

            return s.ProcessOrder(order, payment);
        }

    }
}