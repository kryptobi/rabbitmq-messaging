using System.Reflection;
using System.Text.Json;
using CommunityToolkit.HighPerformance;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMqMessaging;

public abstract class MessageConsumerBase<TMessage> : IAsyncBasicConsumer, IBasicConsumer
where TMessage : class
{
    private readonly bool _isTransient;
    void IBasicConsumer.HandleModelShutdown(object model, ShutdownEventArgs reason)
    {
        throw new NotImplementedException();
    }

    public IModel Model { get; }
    event EventHandler<ConsumerEventArgs>? IBasicConsumer.ConsumerCancelled
    {
        add => throw new NotImplementedException();
        remove => throw new NotImplementedException();
    }

    public event AsyncEventHandler<ConsumerEventArgs>? ConsumerCancelled;
    private readonly HashSet<string> _consumerTags = new();
    
    protected MessageConsumerBase(
        IModel model, 
        bool isTransient)
    {
        Model = model;
        _isTransient = isTransient;
    }
    public Task HandleBasicCancel(string consumerTag)
    {
        OnCancel(consumerTag);
        return Task.CompletedTask;
    }
    

    public Task HandleBasicCancelOk(string consumerTag)
    {
        OnCancel(consumerTag);
        return Task.CompletedTask;
    }

    public Task HandleBasicConsumeOk(string consumerTag)
    {
        _consumerTags.Add(consumerTag);
        return Task.CompletedTask;
    }

    public async Task HandleBasicDeliver(
        string consumerTag,
        ulong deliveryTag,
        bool redelivered,
        string exchange,
        string routingKey,
        IBasicProperties properties,
        ReadOnlyMemory<byte> body)
    { 
        TMessage? message;
        try
        { 
            message = await JsonSerializer.DeserializeAsync<TMessage>(body.AsStream());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Model.BasicReject(deliveryTag, false);

            return;
        }
        
        bool successful;
        
        try
        {
            if (message is null)
            {
                Console.WriteLine("Message is null");
                successful = true;
            }
            else
            {
                successful = await HandleMessageAsync(message);    
            }
        }
        catch (Exception e)
        {
            Console.Write(e);
            successful = false;
        }
        
        if (successful || _isTransient)
        {
            Model.BasicAck(deliveryTag, false);
        }
        else
        {
            Model.BasicReject(deliveryTag, !redelivered);
        }
    }

    public Task HandleModelShutdown(object model, ShutdownEventArgs reason)
    {
        OnCancel(_consumerTags.ToArray());
        return Task.CompletedTask;
    }
    
    private void OnCancel(params string[] consumerTags)
    {
        foreach (EventHandler<ConsumerEventArgs> h in ConsumerCancelled?.GetInvocationList() ?? Array.Empty<Delegate>())
        {
            h(this, new ConsumerEventArgs(consumerTags));
        }

        foreach (var consumerTag in consumerTags)
        {
            _consumerTags.Remove(consumerTag);
        }
    }
    
    protected static bool GetIsTransient<THandler>()
    {
        return typeof(THandler).GetCustomAttribute<MessageHandlerAttribute>()?.IsTransient 
               ?? throw new InvalidOperationException($"MessageHandler {typeof(THandler).Name} is missing {typeof(THandler).Name}");
    }

    protected abstract Task<bool> HandleMessageAsync(TMessage message);
    
    void IBasicConsumer.HandleBasicCancelOk(string consumerTag)
    {
        throw new InvalidOperationException("Should never be called. Enable 'DispatchConsumersAsync'.");
    }

    void IBasicConsumer.HandleBasicConsumeOk(string consumerTag)
    {
        throw new InvalidOperationException("Should never be called. Enable 'DispatchConsumersAsync'.");
    }

    void IBasicConsumer.HandleBasicDeliver(string consumerTag, ulong deliveryTag, bool redelivered, string exchange, string routingKey,
        IBasicProperties properties, ReadOnlyMemory<byte> body)
    {
        throw new InvalidOperationException("Should never be called. Enable 'DispatchConsumersAsync'.");
    }

    void IBasicConsumer.HandleBasicCancel(string consumerTag)
    {
        throw new InvalidOperationException("Should never be called. Enable 'DispatchConsumersAsync'.");
    }
}