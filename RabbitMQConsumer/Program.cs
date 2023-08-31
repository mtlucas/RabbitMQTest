using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using Serilog;
using Prometheus;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQCommon;
using RabbitMQCommon.Models;

/* Install Nuget packages:

    dotnet add package RabbitMQ.Client
    dotnet add package prometheus-net
    dotnet add package serilog.expressions
    dotnet add package serilog.Settings.Configuration
    dotnet add package serilog.Settings.AppSettings
    dotnet add package serilog.sinks.Seq
    dotnet add package serilog.sinks.Debug
    dotnet add package serilog.sinks.Elasticsearch
    dotnet add package serilog.Exceptions
    dotnet add package serilog.Enrichers.Environment
    dotnet add package Microsoft.Extensions.Configuration.Json

    nuke :setup
*/

namespace RabbitMQConsumer
{
    class Program
    {
        static void Main(string[] args)
        {
            var configBuilder = new ConfigurationBuilder().AddJsonFile($"appsettings.json", true, true);
            var config = configBuilder.Build();
            string RabbitMQHostName = config["RabbitMQ:Server"] ?? "localhost";
            string RabbitMQPort = config["RabbitMQ:Port"] ?? "5672";
            string RabbitMQUserName = config["RabbitMQ:Username"] ?? "guest";
            string RabbitMQPassword = config["RabbitMQ:Password"] ?? "guest";
            string queue = config["RabbitMQ:Queue"];
            Dictionary<string, object> queueArgs = new Dictionary<string, object> {
                { "x-queue-type", "quorum" },
                //{ "x-message-ttl", int.Parse(config["RabbitMQ:TTL"]) }
            };

            using var metricServer = new MetricServer(port: 9090);
            metricServer.Start();
            ConfigureLogging();

            /*
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(Services =>
                {
                    Services.AddSingleton(serviceProvider =>
                    {
                        var uri = new Uri($"amqp://{RabbitMQUserName}:{RabbitMQPassword}@{RabbitMQHostName}:{RabbitMQPort}/");
                        return new ConnectionFactory
                        {
                            Uri = uri
                        };
                    })
                    .BuildServiceProvider();
                    Services.AddSingleton<IRabbitMQProducer<Message>, RabbitMQProducerBase<Message>>()
                    .BuildServiceProvider();
                });
            */
                        
            // RabbitMQ connection and channel setup
            var connFactory = new ConnectionFactory { HostName = RabbitMQHostName, UserName = RabbitMQUserName, Password = RabbitMQPassword };
            using var connection = connFactory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(queue: queue,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: queueArgs);

            channel.BasicQos(prefetchSize: 0,
                             prefetchCount: 1,
                             global: false);

            RabbitMqConsumerBase messageReceiver = new RabbitMqConsumerBase(channel);

            Console.WriteLine(" [*] Waiting for messages.");
            Log.Information(" [*] Waiting for messages.");

            channel.BasicConsume(queue: queue,
                                 autoAck: false,
                                 consumer: messageReceiver);

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
            Log.Information("Exiting application.");

            // Configure Serilog service
            void ConfigureLogging()
            {
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
                    .Build();

                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(configuration)
                    .CreateLogger();

                Log.Debug("Starting logging....");
            }
        }
    }
}
