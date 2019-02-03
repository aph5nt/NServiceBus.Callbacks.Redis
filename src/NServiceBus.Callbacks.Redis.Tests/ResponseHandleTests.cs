using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;

namespace NServiceBus.Callbacks.Redis.Tests
{
    public class ResponseHandleTests
    {
        private const string ChannelName = "test-channel";
        
        [Fact]
        public async Task GetResponseAsync_ReceivesResponse_Unblocks()
        {
            // arrange
            const string expected = "foo";
            var subscriber = new SubscriberFake();
            var handle = new ResponseHandle<TestReply>(subscriber, ChannelName);
            
            // act
            var simulationTask = Task.Run(async () =>
            {
                // simulates the delay in the reponse from the receiver
                var jsonData = JsonConvert.SerializeObject(new TestReply {Prop = expected});
                await Task.Delay(1000);
                await subscriber.PublishAsync(ChannelName, jsonData);
            });
            var actual = await handle.GetResponseAsync().WithTimeout(TimeSpan.FromSeconds(5));

            // assert
            Assert.Equal(expected, actual.Prop);
        }

        [Fact]
        public async Task GetResponseAsync_NeverGetsAResponse_ThrowsTimeout()
        {
            // arrange
            var subscriber = new SubscriberFake();
            var handle = new ResponseHandle<TestReply>(subscriber, ChannelName);

            // act

            // assert
            await Assert.ThrowsAsync<TimeoutException>(() =>
                handle.GetResponseAsync().WithTimeout(TimeSpan.FromSeconds(2)));
        }
    }
}
