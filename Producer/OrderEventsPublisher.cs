using Microsoft.Extensions.Configuration;
using Producer.Configuration;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Producer
{
    public class OrderEventsPublisher : IOrderEventsPublisher, IDisposable
    {
        private bool disposedValue;
        private readonly Lazy<IConnection> _connectionLazy;
        private readonly Lazy<IModel> _channelLazy;
        private readonly MD5 _hasher;
        private readonly IProducerConfiguration _configuration;

        public OrderEventsPublisher(IProducerConfiguration configuration)
        {
            _connectionLazy = new Lazy<IConnection>(() => CreateRabbitMqConnection(configuration));
            _channelLazy = new Lazy<IModel>(() => _connectionLazy.Value.CreateModel());
            _hasher = MD5.Create();
            _configuration = configuration;
        }

        private static IConnection CreateRabbitMqConnection(IProducerConfiguration configuration)
        {
            var factory = new ConnectionFactory()
            {
                HostName = configuration.RabbitMQHost,
                Port = configuration.RabbitMQPort,
                UserName = configuration.RabbitMQUser,
                Password = configuration.RabbitMQPass,
            };
            return factory.CreateConnection();
        }

        public void Publish(int orderId, string eventDescription)
        {
            var hashed = _hasher.ComputeHash(Encoding.UTF8.GetBytes(orderId.ToString()));
            var partition = BitConverter.ToUInt32(hashed, 0) % _configuration.PartitionCount;

            string queue = _configuration.PartitionQueuePrefix + partition;
            _channelLazy.Value.QueueDeclare(queue: queue,            
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: new Dictionary<string, object> { { "x-single-active-consumer", true } });


            string message = $"OrderId: {orderId}, Description: {eventDescription}";
            var body = Encoding.UTF8.GetBytes(message);

            _channelLazy.Value.BasicPublish(exchange: "",
                                 routingKey: queue,
                                 basicProperties: null,
                                 body: body);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if(_channelLazy.IsValueCreated) _channelLazy.Value.Dispose();                    
                    if(_connectionLazy.IsValueCreated) _connectionLazy.Value.Dispose();
                }
                               
                disposedValue = true;
            }
        }

      
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
