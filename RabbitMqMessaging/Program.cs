// See https://aka.ms/new-console-template for more information

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using RabbitMqMessaging;

var services = new ServiceCollection();
var serviceProvider = services.BuildServiceProvider();

var connector = RabbitMqConnector.CreateConnector(serviceProvider);

var messageConfigurations = Assembly.GetExecutingAssembly()
    .GetTypes()
    .Where(type => !type.IsAbstract 
                   && type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageHandler<>)))
    .Select(handler =>
    {
        return new
        {
            MessageHandlerType = handler,
            MessageHandlerAttribute = handler.GetCustomAttribute<MessageHandlerAttribute>() ?? throw new Exception(),
            MessageAttribute = handler.GetInterfaces().Single(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IMessageHandler<>)).GenericTypeArguments[0]
                    .GetCustomAttribute<MessageAttribute>() ?? throw new Exception()
        };

    })
        .ToList();

foreach (var messageConfiguration in messageConfigurations)
{
    connector.CreateConsumer(
        messageConfiguration.MessageAttribute, 
        messageConfiguration.MessageHandlerAttribute, 
        messageConfiguration.MessageHandlerType);
}

await Task.Delay(999999);