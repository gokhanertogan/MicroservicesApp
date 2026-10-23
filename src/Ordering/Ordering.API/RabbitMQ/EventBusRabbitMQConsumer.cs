using AutoMapper;
using EventBusRabbitMQ;
using EventBusRabbitMQ.Common;
using EventBusRabbitMQ.Events;
using MediatR;
using Newtonsoft.Json;
using Ordering.Application.Commands;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

namespace Ordering.API.RabbitMQ
{
    public class EventBusRabbitMQConsumer
    {
        private readonly IRabbitMQConnnection _connection;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMapper _mapper;

        public EventBusRabbitMQConsumer(IRabbitMQConnnection connection, IServiceScopeFactory scopeFactory, IMapper mapper)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public void Consume()
        {
            var channel = _connection.CreateModel();
            channel.QueueDeclare(queue:EventBusConstants.BasketCheckoutQueue, durable:false, exclusive:false, autoDelete:false,arguments:null);

            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += ReceivedEvent;

            channel.BasicConsume(queue: EventBusConstants.BasketCheckoutQueue, autoAck: true, consumer: consumer,noLocal:false,exclusive:false,arguments:null);
        }

        private async void ReceivedEvent(object sender, BasicDeliverEventArgs e)
        {
            if (e.RoutingKey==EventBusConstants.BasketCheckoutQueue)
            {
                var message = Encoding.UTF8.GetString(e.Body.Span);
                var basketCheckoutEvent = JsonConvert.DeserializeObject<BasketCheckoutEvent>(message);

                var command = _mapper.Map<CheckoutOrderCommand>(basketCheckoutEvent);

                // The consumer is a singleton; resolve scoped services (DbContext, repositories) per message.
                using (var scope = _scopeFactory.CreateScope())
                {
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    var result = await mediator.Send(command);
                }
            }
        }

        public void Disconnect()
        {
            _connection.Dispose();
        }
    }
}
