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
using Microsoft.AspNetCore.Mvc;

namespace RabbitMQPublisherApi.Controllers
{
    [ApiController]
    public class BackupRestoreController : ControllerBase
    {
        private readonly IRabbitMqProducer<Message> _messagePublisher;
        private readonly IConfiguration _configuration;
        private static readonly Counter metricMessageRestore = Metrics.CreateCounter("messages_restored", "Total number of messages Restored to Queue.");

        public BackupRestoreController(IRabbitMqProducer<Message> messagePublisher, IConfiguration configuration)
        {
            _messagePublisher = messagePublisher;
            _configuration = configuration;
        }

        /// <summary>
        /// Restore messages to Queue from file
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/Restore")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> RestoreMessages(string filename)
        {
            List<object> messages = JsonSerialization.ReadMultipleLinesFromJsonFile(filename);

            Console.WriteLine($" [x] Restore from file: {filename}");
            Log.Information($" [x] Restore from file: {filename}");
            foreach (object envelopeObject in messages)
            {
                string envelopeStr = JsonConvert.SerializeObject(envelopeObject);
                Log.Information($" [*] Message: {envelopeStr}");
                Envelope envelope = JsonConvert.DeserializeObject<Envelope>(envelopeStr);
                object message = envelope.messageObj;
                _messagePublisher.Publish((Message)message);
                Console.WriteLine($" [x] Restored Msg: {message}");
                Log.Information($" [x] Restored Msg: {message}");
                metricMessageRestore.Inc();
            }
            return Ok();
        }
    }
}
