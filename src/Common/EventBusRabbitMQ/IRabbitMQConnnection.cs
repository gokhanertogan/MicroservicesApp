using RabbitMQ.Client;
using System;

namespace EventBusRabbitMQ
{
    public interface IRabbitMQConnnection :IDisposable
    {
        bool IsConnected { get; }
        bool TryConnect();
        IModel CreateModel();
    }
}
