namespace RabbitMqMessaging;

[MessageHandler("MyMessageQueue")]
public class MyMessageHandler : IMessageHandler<MyMessage>
{
    public Task<bool> HandleMessageAsync(MyMessage message)
    {
        /*
         * Deadletter Test!
         */ 
        
        //throw new Exception("machs kaputt");
        
        Console.WriteLine("Message Handler");
        return Task.FromResult(true);
    }
}