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
    public abstract class RabbitMqClientBase : IDisposable
    {
        protected readonly string queue = ConfigurationHelper.config.GetSection("RabbitMQ")["Queue"] ?? "TestQueue";
        protected readonly string exchange = ConfigurationHelper.config.GetSection("RabbitMQ")["Exchange"] ?? "TestExchange";
        protected readonly Dictionary<string, object> queueArgs = new Dictionary<string, object> {
            { "x-queue-type", "quorum" },
            //{ "x-message-ttl", int.Parse(ConfigurationHelper.config.GetSection("RabbitMQ")["TTL"]) }
        };

        protected IModel? Channel { get; private set; }
        private IConnection? _connection;
        private readonly ConnectionFactory _connectionFactory;

        protected RabbitMqClientBase(ConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            ConnectToRabbitMq();
        }

        private void ConnectToRabbitMq()
        {
            if (_connection == null || _connection.IsOpen == false)
            {
                _connection = _connectionFactory.CreateConnection();
                Log.Information($" [*] Opened connection to RabbitMQ server.");
            }

            if (Channel == null || Channel.IsOpen == false)
            {
                Channel = _connection.CreateModel();
                Channel.ExchangeDeclare(
                    exchange: exchange,
                    type: "direct",
                    durable: true,
                    autoDelete: false);

                Channel.QueueDeclare(
                    queue: queue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: queueArgs);

                Channel.QueueBind(
                    queue: queue,
                    exchange: exchange,
                    routingKey: string.Empty);

                Log.Information($" [*] Created necessary RabbitMQ Exchanges/Queues and bindings.");
            }
        }
        
        public void Dispose()
        {
            try
            {
                Channel?.Close();
                Channel?.Dispose();
                Channel = null;
                _connection?.Close();
                _connection?.Dispose();
                _connection = null;
                Log.Information($" [*] Closed all RabbitMQ connections and channels.");
            }
            catch (Exception ex)
            {
                Log.Error($" [-] ERROR: Cannot dispose RabbitMQ channel or connection.\n{ex}");
            }
        }
    }
}
