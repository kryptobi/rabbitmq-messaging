namespace RabbitMqMessaging;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MessageHandlerAttribute : Attribute
{
    public string QueueName { get; set; }
    public bool IsTransient { get; set; } //Transient = Fire and Forget
    
    public MessageHandlerAttribute(
        string queueName,
        bool isTransient = false)
    {
        IsTransient = isTransient;
        QueueName = queueName;
    }
}