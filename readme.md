[![Nuget](https://img.shields.io/badge/nuget-v0.1.1-green.svg)](https://www.nuget.org/packages/NServiceBus.Callbacks.Redis/0.1.1)

# NServiceBus.Callbacks.Redis
This is an unofficial package that provides an implementation of a callback pattern. This essentially mimicks the behavior of a request-response call, effectively blocking until a response is received from the downstream service. The problem I was trying to solve is that with the existing callbacks package provided by Particular, it requires that the endpoint be uniquely identifiable. This is to ensure that the message is delivered to the correct instance that made the request. This can be problematic in environments such as Kubernetes where instances are spinning up and down and any means of uniquely identifying the instance is unpredictable (e.g. machine name, pod id, etc.) -- the transport would eventually get loaded up with dead queues. 

Rather than implementing a complex set of scripts that aim to make the queue names identifiable or delete them once they are consumed, or some other convoluted infrastructure maintenance nightmare, instead, NServiceBus.Callbacks.Redis simply creates a channel on Redis to receive the specific reply on the instance that made the request, and then kills it when it's done.

### Pros
- Keeps with the async messaging paradigm
- Follows a common paradigm of "response topics"
- Doesn't require uniquely identifiable endpoints, but messages are still delivered to the original threads on the instances of the endpoint that were waiting on them.
- Utilizes pub sub with redis
- Channels are gone once the message is received.
- It is an async block, so it won't use up any more resources than a typical synchronous call would.

### Cons
- If the blocking call times out before a reply is received, the reply is lost. Just like a synchronous system. This may be solvable with a little extra work. For example, maybe the MessageCallback handler doesn't consume the message until it receives an ack. This would result in the message remanining in the queue. However, this makes no sense since in most use cases, the client that was waiting on this response would have timed out. We would have to provide them with the conversation ID on timeout. It still needs some thought...

## Use Case
This isn't intended to promote synchronous usage of an asynchronous message bus. This is just to offer a some flexibility. For example if we must integrate with a system that cannot manage requests asynchronously (e.g. websockets aren't an option to receive a response; eventual consistency isn't an option) and it needs a reply with data right now, then this could help. Use this model sparingly and only where absolutely necessary. In other words, if you have the means to keep the client asynchronous, then avoid this and do so.

## Setup
There is no real setup involved. You simply need an instance of ISubscriber available at the time of request and in any handler that will reply. So be sure to register that with DI.

## Usage

Prerequisites:
- The sender and reciver must agree on a reply message type
- The receiver must call `context.Reply(subscriber, replymessage);` at the end of its task

### Receiver

Just make sure to reply with the correct response type

``` csharp
public class MyCommandHandler : IHandleMessages<MyCommand>
{
    private readonly _subscriber;

    public MyCommandHandler(ISubscriber subscriber)
    {
        _subscriber = subscriber;
    }

    public async Task Handle(MyCommand message, IMessageHandlerContext context)
    {
        // normal handling stuff...

        // reply
        await context.Reply<MyReply>(_subscriber, msg => { msg.Value = "Hello World!" });
    }
}
```

### Sender

Send the request which will give you back a handle. Await it to get your response.

``` csharp
var options = new SendOptions();
options.SetDestination("DestinationEndpoint")
var handle = await session.Request<MyCommand, MyReply>(cmd => {
    cmd.Prop1 = "foo";
    cmd.Prop2 = "bar";
}, options);

var result = await handle.GetResponseAsync();
// result.Value is "Hello World!"
```

**See samples for working demo**

## TODO
- Use some sort of configuration magic to allow `Request<>()` and `Reply()` to get an instance of `ISubscriber` from DI so that we don't have to be responsible for it. This might involve overriding default behaviors for thos in NServiceBus.Core, which I'm not sure is possible.
