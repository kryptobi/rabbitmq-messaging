namespace RabbitMqMessaging;

[Message("MyMessageExchange")]
public class MyMessage
{
    public int Count { get; set; }
}