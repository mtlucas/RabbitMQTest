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
using Microsoft.AspNetCore.Mvc;
using RabbitMQCommon;
using RabbitMQCommon.Models;

namespace RabbitMQPublisherApi.Controllers
{
    [ApiController]
    public class PublisherController : ControllerBase
    {
        private readonly IRabbitMqProducer<Message> _messagePublisher;
        private readonly IConfiguration _configuration;
        private static readonly Counter metricMessageTx = Metrics.CreateCounter("messages_sent_total", "Total number of messages sent and processed.");

        public PublisherController(IRabbitMqProducer<Message> messagePublisher, IConfiguration configuration)
        {
            _messagePublisher = messagePublisher;
            _configuration = configuration;
        }

        /// <summary>
        /// Publishes a single message to queue
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/Publish")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> PublishMessage(string message)
        {
            Message messageJson = new()
            {
                message = message,
            };

            _messagePublisher.Publish(messageJson);
            Console.WriteLine($" [x] Publish Sent: {message}");
            Log.Information($" [x] Publish Sent: {message}");
            metricMessageTx.Inc();
            return Ok();
        }

        /// <summary>
        /// Publishes multiple messages to queue based on number of iterations
        /// </summary>
        /// <param name="message"></param>
        /// <param name="iterations"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/PublishMany")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> PublishManyMessages(string message, int iterations)
        {
            Message messageJson = new()
            {
                message = message,
            };

            for (int i = 0; i < iterations; i++)
            {
                _messagePublisher.Publish(messageJson);
                Console.WriteLine($" [x] PublishMany Sent #{i}: {message}");
                Log.Information($" [x] PublishMany Sent #{i}: {message}");
                metricMessageTx.Inc();
            }
            return Ok();
        }
    }
}