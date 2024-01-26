namespace RabbitMqMessaging;

[MessageHandler("MyNotificationQueue", isTransient: true)]
public class MyNotificationHandler2 : IMessageHandler<MyNotification>
{
    public Task<bool> HandleMessageAsync(MyNotification message)
    {
        Console.WriteLine("2");
        return Task.FromResult(true);
    }
}