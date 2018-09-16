using NServiceBus.Callbacks.Redis;
using UsageSample.Messaging;

namespace UsageSample.Sender2
{
    // We only declare these at all so that NSB will register them as handlers during assembly scan 

    public class TestReplyHandler : MessageCallback<TestReply> { }
}