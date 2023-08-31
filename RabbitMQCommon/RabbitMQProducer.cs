using System;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Configuration;
using Serilog;
using Prometheus;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQCommon;
using RabbitMQCommon.Models;

namespace RabbitMQCommon
{
    public interface IRabbitMqProducer<T>
    {
        void Publish(T message);
    }

    public class RabbitMqProducerBase<T> : RabbitMqClientBase, IRabbitMqProducer<T>
    {
        private string exchange = ConfigurationHelper.config.GetSection("RabbitMQ")["Exchange"] ?? "TestExchange";
        private string appId = Assembly.GetExecutingAssembly().GetName().Name ?? "RabbitMQTester";
        private Dictionary<string, object>? headers = ConfigurationHelper.config.GetSection("RabbitMQ:Headers").GetChildren().ToDictionary(x => x.Key, x => x.Value as object);

        private readonly ConnectionFactory _connectionFactory;

        public RabbitMqProducerBase(ConnectionFactory connectionFactory) : base(connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public void Publish(T message)
        {
            try
            {
                var json = JsonConvert.SerializeObject(message);
                var body = Encoding.UTF8.GetBytes(json);
                var properties = Channel.CreateBasicProperties();
                properties.AppId = appId;
                properties.ContentType = "application/json";
                properties.Persistent = true;
                properties.Headers = headers;
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                Channel.BasicPublish(
                    exchange: exchange,
                    routingKey: string.Empty,
                    body: body,
                    basicProperties: properties
                    );
            }
            catch (Exception ex)
            {
                Log.Error($" [-] Error while publishing message.\n{ex}");
            }
        }
    }
}
