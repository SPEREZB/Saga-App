using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
namespace Saga_App
{
   

    // PRIMERO DEFINIMOS LAS ENTIDADES PARA NUESTRO DBCONTEXT 
    //EN ESTE CASO SE DEFINEN 2 ORDER Y PAYMENT
    public class Order
    {
        public int Id { get; set; } 
        public string Status { get; set; }
 
    }

    public class Payment
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } 
    }

    // DEFINIMOS NUESTRO DbContext
    public class AppDbContext : DbContext
    {
        public DbSet<Order> Orders { get; set; }
        public DbSet<Payment> Payments { get; set; }

        public AppDbContext()
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            optionsBuilder.UseSqlServer(connectionString);
        }
    }

 

    // SE IMPLEMENTA LA SAGA  
    public class OrderPaymentSaga
    {
        private readonly AppDbContext _dbContext = new AppDbContext();
        private readonly IModel _rabbitMQChannel;
       
        public OrderPaymentSaga()
        {
             
        }
        public OrderPaymentSaga(AppDbContext dbContext, IModel rabbitMQChannel)
        {
            _dbContext = dbContext;
            _rabbitMQChannel = rabbitMQChannel; 
        }

        
        //EMPEZAR NUESTRA SAGA
        public void StartSaga(Order order, Payment pay)
        {
             
            // SE INICIA LA TRANSACCION
            _dbContext.Database.BeginTransaction();

            try
            {
                // ACTUALIZAMOS EL ESTADO DE LA ORDEN EN NUESTRO DBCONTEXT  
                order.Status = "InProcess";
                _dbContext.SaveChanges();

                // SE ENVIA UN MENSAJE A RABBITMQ DEL PROCESO DEL PAGO  
                string message = $"Payment for OrderId: {order.Id}, Amount: {pay.Amount}";
                byte[] body = Encoding.UTF8.GetBytes(message); 
                 

                // Crear la conexión a RabbitMQ y asignar el canal a la variable _rabbitMQChannel
                var factory = new ConnectionFactory() { HostName = "localhost" };
                using (var connection = factory.CreateConnection())
                {
                    using (var channel = connection.CreateModel())
                    {
                        channel.ExchangeDeclare(exchange: "logs", type: ExchangeType.Fanout);
                        var queuName = channel.QueueDeclare().QueueName;
                        channel.QueueBind(queue: queuName, exchange: "logs", routingKey: "");

                        var consumer = new EventingBasicConsumer(channel);
                        consumer.Received += (model, ea) =>
                        {
                            var body = ea.Body.ToArray();
                            var message = Encoding.UTF8.GetString(body);
                        }; 
                        channel.BasicPublish(exchange: "", routingKey: "", basicProperties: null, body: body);

                    }
                }

            }
            catch (Exception ex)
            { 
                FailSaga(order,pay); 
                Console.WriteLine($"Error occurred: {ex.Message}");
            }
        }
        //COMPLETAR NUESTRA SAGA
        public void CompleteSaga(Order order, Payment payment)
        {
            // Actualizar el estado de la orden y el pago en el DbContext
            order.Status = "Completed";
            payment.Status = "Completed";
            _dbContext.SaveChanges();

            // Confirmar la transacción del DbContext
            _dbContext.Database.CommitTransaction();
        }
        //ERROR EN LA SAGA
        public void FailSaga(Order order, Payment payment)
        {
            // Actualizar el estado de la orden y el pago en el DbContext
            order.Status = "Failed";
            payment.Status = "Failed";
            _dbContext.SaveChanges();

            // Realizar rollback de la transacción del DbContext
            _dbContext.Database.RollbackTransaction();
        }
    }

    //NUESTRA SAGA
    public class Saga
    {
        private readonly AppDbContext _dbContext;
        private readonly IModel _rabbitMQChannel;
        private readonly OrderPaymentSaga _saga = new OrderPaymentSaga();

        public Saga()
        { 
        }
        public Saga(AppDbContext dbContext, IModel rabbitMQChannel)
        {
            _dbContext = dbContext;
            _rabbitMQChannel = rabbitMQChannel; 
        }

        public string ProcessOrder(Order order, Payment pay)
        {
            try
            {

                // INICIAR SAGA
                _saga.StartSaga(order, pay);

              
                var payment = new Payment
                {
                    OrderId = order.Id,
                    Amount = pay.Amount,
                    Status = "Completed"
                };

                // COMPLETAR SAGA
                _saga.CompleteSaga(order, payment);

                return "bienn";
            }
            catch (Exception ex)
            { 
                Console.WriteLine($"Error occurred: {ex.Message}"); 
                _saga.FailSaga(order, null);
                return "mall";
            }
        }
    }
}
