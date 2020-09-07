using Microsoft.Extensions.Configuration;

namespace Producer.Configuration
{
    public interface IProducerConfiguration
    {
        public int PartitionCount { get; }
        public string PartitionQueuePrefix { get; }
        public string RabbitMQHost { get; }
        public int RabbitMQPort { get; }
        public string RabbitMQUser { get; }
        public string RabbitMQPass { get; }
    }
    public class ProducerConfiguration : IProducerConfiguration
    {
        public ProducerConfiguration(IConfiguration configuration)
        {        
            PartitionCount = configuration.GetValue<int>("PartitionCount");
            PartitionQueuePrefix = configuration.GetValue<string>("PartitionQueuePrefix");
            RabbitMQHost = configuration.GetValue<string>("RMQHost");
            RabbitMQHost = configuration.GetValue<string>("RMQHost");
            RabbitMQPort = configuration.GetValue<int>("RMQPort");
            RabbitMQUser = configuration.GetValue<string>("RMQUser");
            RabbitMQPass = configuration.GetValue<string>("RMQPass");
        }

        
        public int PartitionCount { get; }
        public string PartitionQueuePrefix { get; }   
        public string RabbitMQHost { get; }
        public int RabbitMQPort { get; }
        public string RabbitMQUser { get; }
        public string RabbitMQPass { get; }
    }
}
