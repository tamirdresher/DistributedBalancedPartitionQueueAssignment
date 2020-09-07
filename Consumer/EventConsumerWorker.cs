using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Consumer
{
    internal class EventConsumerWorker
    {
        private readonly string _queue;
        private readonly IModel _channel;
        private readonly ILogger _logger;
        private EventingBasicConsumer _consumer;
        private Task _executingTask;

        public EventConsumerWorker(string queue, IModel channel, ILogger logger)
        {
            _queue = queue;
            _channel = channel;
            _logger = logger;
        }

        public Task StartAsync()
        {
            _executingTask =
                Task.Run(() =>
                {
                    try
                    {
                        _channel.QueueDeclare(queue: _queue,
                                       durable: true,
                                       exclusive: false,
                                       autoDelete: false,
                                       arguments: new Dictionary<string, object> { { "x-single-active-consumer", true } });


                        _consumer = new EventingBasicConsumer(_channel);
                        _consumer.Received += (model, ea) =>
                        {
                            var body = ea.Body.ToArray();
                            var message = Encoding.UTF8.GetString(body);
                            _logger.LogInformation(" [x] Received {0}", message);
                            _channel.BasicAck(ea.DeliveryTag, false);
                        };
                        _channel.BasicConsume(queue: _queue,
                                             autoAck: false,
                                             consumer: _consumer);
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError(ex, $"Couldnt subscribe to queue {_queue}");
                    }
                  
                });
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            if (_consumer!=null)
            {
                _channel.BasicCancel(_consumer.ConsumerTags[0]);
                _consumer = null;                
                _executingTask = null;
            }
            return Task.CompletedTask;
        }
    }
}