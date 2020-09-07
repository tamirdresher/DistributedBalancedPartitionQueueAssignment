using k8s;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Consumer.Configuration
{
    public interface IConsumerConfiguration
    {
        public Task<int> GetConsumerCountAsync();
        public int ConsumerId { get; }
        public int PartitionCount { get; }
        public string PartitionQueuePrefix { get; }
        public string RabbitMQHost { get;  }
        public int RabbitMQPort { get; }
        public string RabbitMQUser { get; }
        public string RabbitMQPass { get; }
    }
    public class ConsumerConfiguration : IConsumerConfiguration
    {
        private readonly string _statefulSetName;
        private readonly string _statefulSetNamespace;
        private readonly IConfiguration _configuration;

        public ConsumerConfiguration(IConfiguration configuration)
        {
            _statefulSetName = configuration.GetValue<string>("STATEFULSET_NAME");
            _statefulSetNamespace = configuration.GetValue<string>("STATEFULSET_NAMESPACE");
            PartitionCount = configuration.GetValue<int>("PartitionCount");
            PartitionQueuePrefix = configuration.GetValue<string>("PartitionQueuePrefix");
            RabbitMQHost = configuration.GetValue<string>("RMQHost");
            RabbitMQPort = configuration.GetValue<int>("RMQPort");
            RabbitMQUser = configuration.GetValue<string>("RMQUser");
            RabbitMQPass = configuration.GetValue<string>("RMQPass");
            _configuration = configuration;
        }

        public int ConsumerId
        {
            get
            {
                try
                {
                    int.TryParse(Environment.MachineName.Replace($"{_statefulSetName}-", ""), out var consumerId);
                    return consumerId;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Couldnt extract the {nameof(ConsumerId)} from the hostname {Environment.MachineName} and StatefulSet name '{_statefulSetName}'", ex);
                }

            }
        }

        public int PartitionCount { get; }
        public string PartitionQueuePrefix { get; }
        public bool IsHostedInKubernetes => !string.IsNullOrEmpty(_configuration.GetValue<string>("KUBERNETES_PORT"));

        public string RabbitMQHost { get; }
        public int RabbitMQPort { get; }
        public string RabbitMQUser { get; }
        public string RabbitMQPass { get; }

        public async Task<int> GetConsumerCountAsync()
        {
            try
            {

                KubernetesClientConfiguration config = null;
                if (IsHostedInKubernetes)
                {
                    config = KubernetesClientConfiguration.InClusterConfig();
                }
                else
                {
                    // don't forget to run 'kubectl proxy'
                    config = new KubernetesClientConfiguration { Host = "http://127.0.0.1:8001" };
                }

                using (var client = new Kubernetes(config))
                {
                    var scale = await client.ReadNamespacedStatefulSetScaleAsync(_statefulSetName, _statefulSetNamespace);
                    return scale.Spec.Replicas ?? 1;
                }

            }
            catch (Exception ex)
            {

                throw;
            }
        }
    }
}
