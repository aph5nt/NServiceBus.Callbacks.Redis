# NServiceBus.Callbacks.Redis
This is an unofficial package that provides an implementation of a callback pattern. This essentially mimicks the behavior of a request-response call, effectively blocking until a response is received from the downstream service. The problem I was trying to solve is that with the existing callbacks package provided by Particular, it requires that the endpoint be uniquely identifiable. This is to ensure that the message is delivered to the correct instance that made the request. This can be problematic in environments such as Kubernetes where instances are spinning up and down and any means of uniquely identifying the instance is unpredictable (e.g. machine name, pod id, etc.) -- the transport would eventually get loaded up with dead queues. Rather than implementing a complex set of scripts that aim to make the queue names identifiable or delete them once they are consumed, instead, NServiceBus.Callbacks.Redis simply creates a channel on Redis to receive the specific reply on the instance that made the request.

### Pros:
- Keeps with the async messaging paradigm
- Follows a common paradigm of "response topics"
- Doesn't require uniquely identifiable endpoints
- Utilizes pub sub with redis
- Channels are gone once the message is received.
- It is an async block, so it won't use up any more resources than a typical synchronous call would.

### Cons:
- If the blocking call times out before a reply is received, the reply is lost. Just like a synchronous system. This may be solvable with a little extra work. For example, maybe the MessageCallback handler doesn't consume the message until it receives an ack. This would result in the message remanining in the queue. However, this makes no sense since in most use cases, the client that was waiting on this response would have timed out. We would have to provide them with the conversation ID on timeout. It still needs some thought...

## Use Case:
This isn't intended to promote synchronous usage of an asynchronous message bus. This is just to offer a some flexibility. For example if we must integrate with a system that cannot manage requests asynchronously (e.g. websockets aren't an option to receive a response; eventual consistency isn't an option) and it needs a reply with data right now, then this could help. Use this model sparingly and only where absolutely necessary. In other words, if you have the means to keep the client asynchronous, then avoid this and do so.

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

Declare the handler. This is so that NSB will register it as a handler during assembly scan at startup.
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