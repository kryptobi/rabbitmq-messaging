namespace RabbitMqMessaging;

[MessageHandler("MyNotificationQueue", isTransient: true)]
public class MyNotificationHandler : IMessageHandler<MyNotification>
{
    public Task<bool> HandleMessageAsync(MyNotification message)
    {
        Console.WriteLine("Fire and Forget Message Handler");
        return Task.FromResult(true);
    }
}