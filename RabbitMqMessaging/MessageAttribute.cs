namespace RabbitMqMessaging;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MessageAttribute : Attribute
{
    public string Exchange { get; set; }
    
    public MessageAttribute(
        string exchange)
    {
        Exchange = exchange;
    }
}