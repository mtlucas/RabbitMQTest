using System;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Configuration;
using System.Threading.Tasks;
using MediatR;
using Serilog;
using Prometheus;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQCommon.Models;

namespace RabbitMQCommon
{
    public class RabbitMqEventConsumerBase : RabbitMqClientBase
    {
        private readonly IMediator _mediator;
        private static readonly Counter metricMessageRx = Metrics.CreateCounter("messages_received_total", "Total number of messages received and processed.");

        public RabbitMqEventConsumerBase(
            IMediator mediator,
            ConnectionFactory connectionFactory) :
            base(connectionFactory)
        {
            _mediator = mediator;
        }

        protected virtual async Task OnEventReceived<T>(object sender, BasicDeliverEventArgs @event)
        {
            try
            {
                var body = Encoding.UTF8.GetString(@event.Body.ToArray());
                var message = JsonConvert.DeserializeObject<T>(body);

                await _mediator.Send(message);
            }
            catch (Exception ex)
            {
                Log.Error($" [-] ERROR while retrieving message from queue.\n{ex}");
            }
            finally
            {
                Channel.BasicAck(@event.DeliveryTag, false);
            }
        }
    }
}
