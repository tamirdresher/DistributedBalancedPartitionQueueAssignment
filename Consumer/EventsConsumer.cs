using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Consumer.Configuration;
using k8s;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Consumer
{
    public class EventsConsumer : BackgroundService
    {
        private readonly ILogger<EventsConsumer> _logger;
        private readonly IConsumerConfiguration _consumerConfiguration;
        private readonly ILoggerFactory _loggerFactory;
        private int _consumerCount;
        Dictionary<int, EventConsumerWorker> _eventConsmerWorkers = new Dictionary<int, EventConsumerWorker>();

        public EventsConsumer(ILogger<EventsConsumer> logger, 
            IConsumerConfiguration consumerConfiguration,
            ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _consumerConfiguration = consumerConfiguration;
            _loggerFactory = loggerFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {        
            try
            {
                await ExecuteInternalAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occured");
                throw;
            }
        }

        protected async Task ExecuteInternalAsync(CancellationToken stoppingToken)
        {
            using var _ = _logger.BeginScope("Consumer {ConsumerId}", _consumerConfiguration.ConsumerId);

            _logger.LogInformation($"{nameof(EventsConsumer)} started");
            var factory = new ConnectionFactory()
            {
                HostName = _consumerConfiguration.RabbitMQHost,
                UserName = _consumerConfiguration.RabbitMQUser,
                Password = _consumerConfiguration.RabbitMQPass,
                Port = _consumerConfiguration.RabbitMQPort
            };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            while (!stoppingToken.IsCancellationRequested)
            {
                var newCousumerCount =  await GetNewConsmerCount();

                if (_consumerCount != newCousumerCount)
                {
                    _logger.LogInformation($"Consumer count has changed from {_consumerCount} to {newCousumerCount}. Partition count is {_consumerConfiguration.PartitionCount}");
                    _consumerCount = newCousumerCount;
                    IEnumerable<int> newPartitions = GeneratePartitionsList(newCousumerCount);
                    var currentPartitions = _eventConsmerWorkers.Keys;

                    var partitionsToRemove = currentPartitions.Except(newPartitions);
                    var partitionsToAdd = newPartitions.Except(currentPartitions);

                    await DisconnectFromPartitionsAsync(partitionsToRemove);
                    await ConnectToPartitionsAsync(partitionsToAdd, channel);

                    await Task.Delay(1000, stoppingToken);
                }
            }

            await DisconnectFromPartitionsAsync(_eventConsmerWorkers.Keys);
        }

        private async Task<int> GetNewConsmerCount()
        {
            try
            {
                return await _consumerConfiguration.GetConsumerCountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Couldnt get the current number of consumer");
                return this._consumerCount;
            }
        }

        private async Task ConnectToPartitionsAsync(IEnumerable<int> partitions, IModel channel)
        {
            _logger.LogInformation($"Connecting to partitions: {string.Join(",", partitions)}");
            foreach (var partition in partitions)
            {
                string queue = _consumerConfiguration.PartitionQueuePrefix + partition;
                var worker = new EventConsumerWorker(
                    queue, 
                    channel,
                    _loggerFactory.CreateLogger($"Consumer {_consumerConfiguration.ConsumerId} - {queue} worker"));
                await worker.StartAsync();
                _eventConsmerWorkers[partition] = worker;
            }        
        }

       
        private async Task DisconnectFromPartitionsAsync(IEnumerable<int> partitions)
        {
            _logger.LogInformation($"Disconnecting from partitions: {string.Join(",", partitions)}");
            foreach (var partition in partitions)
            {
                var worker = _eventConsmerWorkers[partition];
                await worker.StopAsync();
                _eventConsmerWorkers.Remove(partition);
            }
        }

        private IEnumerable<int> GeneratePartitionsList(int consumerCount)
        {
            int consumerId = _consumerConfiguration.ConsumerId;
            int parititionCount = _consumerConfiguration.PartitionCount;
            int assignableConsumerCount = Math.Min(consumerCount, parititionCount); //we can't assign more consumers that partitions
            if (consumerId>=assignableConsumerCount)
            {
                return Enumerable.Empty<int>();
            }
            
            var partitionsPerConsumer = Math.DivRem(parititionCount, assignableConsumerCount, out var remainder);
            var partitionsStart = consumerId * partitionsPerConsumer;
            var amountOfPartitionsToTake = partitionsPerConsumer + (IsLastConsumer(consumerCount, consumerId) ? remainder : 0);

            var newPartitions = Enumerable.Range(partitionsStart, amountOfPartitionsToTake);
            return newPartitions;
        }

        private static bool IsLastConsumer(int consumerCount, int consumerId)
        {
            return consumerId == consumerCount - 1;
        }
    }
}
