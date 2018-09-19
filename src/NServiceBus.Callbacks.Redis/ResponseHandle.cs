namespace NServiceBus.Callbacks.Redis
{
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using StackExchange.Redis;

    /// <summary>
    /// Wraps a preconfigured instance of <see cref="ISubscriber"/> that will be used to received a reply from a downstream service.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ResponseHandle<T> where T : class, IMessage
    {
        private readonly ISubscriber _subscriber;
        private readonly string _channelName;

        public ResponseHandle(ISubscriber subscriber, string channelName)
        {
            _subscriber = subscriber;
            _channelName = channelName;
        }

        /// <summary>
        /// Blocks until a response is received or timeout occurs.
        /// </summary>
        /// <returns></returns>
        public async Task<T> GetResponseAsync()
        {
            var result = string.Empty;
            await _subscriber.SubscribeAsync(_channelName, (ch, val) => result = val).ConfigureAwait(false);
            
            // block until we get a result or timeout
            await TaskEx.WaitWhile(() => string.IsNullOrEmpty(result));

            // TODO: would be better if we could somehow align this with the serializer currently being used by NSB
            var response = JsonConvert.DeserializeObject<T>(result); 
            await _subscriber.UnsubscribeAsync(_channelName).ConfigureAwait(false);
            return response;
        }
    }
}