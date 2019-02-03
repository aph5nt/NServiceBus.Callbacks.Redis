namespace NServiceBus.Callbacks.Redis.Tests
{
    public class TestReply : IMessage
    {
        public string Prop { get; set; }
    }
}