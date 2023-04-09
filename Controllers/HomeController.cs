using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using Saga_App.Models;

namespace Saga_App.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly List<AppDbContext> _dbContexts;
        private readonly IConnection _rabbitMqConnection; 

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;

            // Crear y configurar la lista de AppDbContext
            _dbContexts = new List<AppDbContext>();
            // Agregar instancias de AppDbContext a la lista (puedes agregar tantos como necesites)
            _dbContexts.Add(new AppDbContext()); 
            // ...

            // Crear y configurar la instancia de IConnection (por ejemplo, usando la biblioteca RabbitMQ.Client)
            _rabbitMqConnection = CreateRabbitMqConnection();
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
            Saga s = new Saga(_dbContexts, _rabbitMqConnection);
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

        private IConnection CreateRabbitMqConnection()
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost", // Cambiar por la dirección del servidor RabbitMQ
                Port = 5672, // Cambiar por el puerto del servidor RabbitMQ
                UserName = "guest", // Cambiar por el nombre de usuario del servidor RabbitMQ
                Password = "guest", // Cambiar por la contraseña del servidor RabbitMQ
                VirtualHost = "/" // Cambiar por el virtual host del servidor RabbitMQ
            };

            return factory.CreateConnection();
        }

    }
}