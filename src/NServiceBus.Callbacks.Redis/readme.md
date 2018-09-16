
## Setup
Setup mostly only requires that you register a factory that returns an instance of the Redis ISubscriber. Choose your own injection pattern, but at some point before the application is "ready", the following needs to be called:
``` csharp
CallbackSubscriber.UseSubscriberFactory()
```
For example:

``` csharp
var multiplexer = ConnectionMultiplexer.Connect("localhost:6379");
CallbackSubscriber.UseSubscriberFactory(multiplexer.GetSubscriber);
```

## Usage

Prerequisites:
- The sender and reciver must agree on a reply message type
- The receiver must call `context.Reply(replymessage);` at the end of its task
- You need to subclass `MessageCallback<T>` but there is nothing to implement. You just need to declare the subclass so that NSB picks it up as a handler during assembly scan on startup.

### Receiver

Just make sure to reply with the correct response type

``` csharp
public class MyCommandHandler : IHandleMessages<MyCommand>
{
    public async Task Handle(MyCommand message, IMessageHandlerContext context)
    {
        // normal handling stuff...

        // reply
        await context.Reply<MyReply>(msg => { msg.Value = "Hello World!" });
    }
}
```

### Sender

Declare the handler
``` csharp
// This is literally all you do. Just an empty subclass 
// where T is the type of the reply you are expecting.
public class MyReplyHandler : MessageCallback<MyReply> { }
```

Send the command and then await the response. I added an extension overload to allow you to pass the destination and conversation ID in without having to add it to options.

``` csharp
var conversationId = Guid.NewGuid();
await session.Send<MyCommand>("DestinationEndpoint", conversationId, cmd => {
    cmd.Prop1 = "foo";
    cmd.Prop2 = "bar";
});

var result = await MyReplyHandler.GetResponseAsync(conversationId);
// result.Value is "Hello World!"
```

See samples for working demo