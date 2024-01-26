using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace RabbitMqMessaging;

public class RabbitMqConnector
{
    public readonly IConnection _connection;
    public readonly Guid _InstanceGuid;
    public readonly IServiceProvider _rootServiceProvider;
    private const string DeadLetterSuffix = "-dead-letter";

    public RabbitMqConnector(IConnection connection, IServiceProvider rootServiceProvider)
    {
        _connection = connection;
        _rootServiceProvider = rootServiceProvider;
    }
    
    public static RabbitMqConnector CreateConnector(IServiceProvider rootServiceProvider)
    {
        var connectionFactory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672,  
            UserName = "username",
            Password = "strong_secret",
            AutomaticRecoveryEnabled = true,
            DispatchConsumersAsync = true
        };

        var connection = connectionFactory.CreateConnection();
        return new RabbitMqConnector(connection, rootServiceProvider);
    }

    public void CreateConsumer(
        MessageAttribute messageAttribute, 
        MessageHandlerAttribute messageHandlerAttribute,
        Type messageHandlerType)
    {
        var channel = _connection.CreateModel();
        channel.ExchangeDeclare(
            messageAttribute.Exchange, 
            ExchangeType.Fanout, 
            true);
        
        var queueName = messageHandlerAttribute.IsTransient
            ? $"{messageHandlerAttribute.QueueName}-{Guid.NewGuid():N}"
            : messageHandlerAttribute.QueueName;
        
        var args = new Dictionary<string, object>();
        if (!messageHandlerAttribute.IsTransient)
        {
            var deadLetterExchangeName = $"{messageAttribute.Exchange}{DeadLetterSuffix}";
            
            channel.ExchangeDeclare(
                deadLetterExchangeName, 
                ExchangeType.Fanout, 
                true);
            
            var deadLetterQueueName = $"{queueName}{DeadLetterSuffix}";
            
            channel.QueueDeclare(
                deadLetterQueueName,
                true,
                false,
                false
            ); 
            
            channel.QueueBind(deadLetterQueueName, deadLetterExchangeName, string.Empty);

            args.Add("x-dead-letter-exchange", deadLetterExchangeName);
        }

        channel.QueueDeclare(
            queueName,
            !messageHandlerAttribute.IsTransient,
            false,
            messageHandlerAttribute.IsTransient, 
            args
            );
     
        channel.QueueBind(queueName, messageAttribute.Exchange, string.Empty);
        
        var messageType = messageHandlerType.GetInterfaces().Single(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IMessageHandler<>)).GenericTypeArguments[0];
        
        var handlerInstance = (IBasicConsumer)ActivatorUtilities.CreateInstance(_rootServiceProvider, typeof(HandlerBasedMessageConsumer<,>).MakeGenericType(messageHandlerType, messageType), channel, messageHandlerAttribute.IsTransient);
        channel.BasicConsume(queueName, false, handlerInstance);
    }
}