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

namespace RabbitMQConsumer
{
    public class RabbitMqConsumerBase : DefaultBasicConsumer
    {
        private static readonly Counter metricMessageRx = Metrics.CreateCounter("messages_received_total", "Total number of messages received and processed.");
        private readonly IModel _channel;

        public RabbitMqConsumerBase(IModel channel)
        {
            _channel = channel;
        }
        public override void HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey, IBasicProperties properties, ReadOnlyMemory<byte> body)
        {
            Dictionary<string, object> headersDict = new Dictionary<string, object>();
            var message = Encoding.UTF8.GetString(body.ToArray());
            metricMessageRx.Inc();
            Console.WriteLine($" [x] Received: {message}");
            Log.Information($" [x] Received: {message}");
            Console.WriteLine(string.Concat("     Exchange:     ", exchange));
            if (properties.Headers != null)
            {
                string headersString = string.Empty;
                foreach (var header in properties.Headers)
                {
                    if (headersString != string.Empty) { headersString += ", "; }
                    string headerValue = string.Empty;
                    switch ((header.Value.GetType().Name))
                    {
                        case "Byte[]": headerValue = Encoding.UTF8.GetString((byte[])header.Value);
                            headersDict.Add(header.Key, headerValue);
                            break;
                        default: headerValue = header.Value.ToString();
                            headersDict.Add(header.Key, header.Value); 
                            break;
                    }
                    headersString += "[ " + header.Key + ": " + headerValue + " ]";
                    
                }
                Console.WriteLine(string.Concat("     Headers:      ", headersString));
            }
            else
            {
                Console.WriteLine(string.Concat("     Headers:"));  // null headers
            }
            Console.WriteLine(string.Concat("     AppId tag:    ", properties.AppId));
            Console.WriteLine(string.Concat("     Routing tag:  ", routingKey));
            Console.WriteLine(string.Concat("     Consumer tag: ", consumerTag));
            Console.WriteLine(string.Concat("     Delivery tag: ", deliveryTag));

            // Simulate processing a message by sleeping the amount of chars in message
            int dots = message.Split('.').Length - 1;
            Thread.Sleep(dots * 10);

            // Save messsage to disk
            Message messageJson = JsonConvert.DeserializeObject<Message>(message);
            Envelope envelope = new Envelope() { messageObj = messageJson, headers = headersDict, appId = properties.AppId, routingTag = routingKey, timestamp = properties.Timestamp };
            JsonSerialization.WriteToJsonFile<Envelope>("BackupMsgs.json", envelope, true);

            Console.WriteLine($" [x] Processed: {message}");
            Log.Information($" [x] Processed: {message}");

            _channel.BasicAck(deliveryTag, false);
        }
    }
}
