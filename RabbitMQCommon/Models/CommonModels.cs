using Newtonsoft.Json;

namespace RabbitMQCommon.Models
{
    public class Message
    {
        public string? message { get; set; }
        public DateTime dateTime { get { return DateTime.Now; } }
    }

    public class Envelope
    {
        public Message messageObj { get; set; }
        public Dictionary<string, object>? headers { get; set; }
        public string? appId { get; set; }
        public string? routingTag { get; set; }
        public RabbitMQ.Client.AmqpTimestamp? timestamp { get; set; }
    }
}
