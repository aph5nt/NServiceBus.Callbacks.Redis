using NServiceBus;

namespace UsageSample.Messaging
{
    public class TestReply : IMessage
    {
        public string ReplyValue { get; set; }
    }
}