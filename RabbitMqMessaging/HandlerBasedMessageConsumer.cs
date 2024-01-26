using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace RabbitMqMessaging;

public class HandlerBasedMessageConsumer<THandler, TMessage> : MessageConsumerBase<TMessage>
where TMessage : class
where THandler : IMessageHandler<TMessage>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    
    public HandlerBasedMessageConsumer(
        IServiceScopeFactory serviceScopeFactory,
        IModel model, 
        bool isTransient) 
        : base(model, isTransient)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task<bool> HandleMessageAsync(TMessage message)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();

        var handler = ActivatorUtilities.CreateInstance<THandler>(scope.ServiceProvider);
        await handler.HandleMessageAsync(message);
        
        return true;
    }
}

public interface IMessageHandler<in TMessage>
where TMessage : class
{
    Task<bool> HandleMessageAsync(TMessage message);
}

public interface IDeadLetterMessageHandler<in TMessage>
    where TMessage : class
{
    Task<bool> HandleMessageAsync(TMessage message);
}