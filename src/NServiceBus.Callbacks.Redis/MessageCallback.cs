using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NServiceBus.Callbacks.Redis
{
    public abstract class MessageCallback<T> : CallbackSubscriber, IHandleMessages<T>
        where T : class
    {
        private const string KeyFormat = "nsbcallback-{0}";

        private static string ChannelKey(object conversationId) => string.Format(KeyFormat, conversationId);

        public async Task Handle(T message, IMessageHandlerContext context)
        {
            Validate();
            if (!context.MessageHeaders.TryGetValue(Headers.ConversationId, out var conversationId))
                return; // nothingtodohere.gif

            await Subscriber.PublishAsync(
                ChannelKey(conversationId),
                JsonConvert.SerializeObject(message));
        }

        /// <summary>
        /// Blocks until a response is received or time out.
        /// </summary>
        /// <param name="conversationId">The conversation id that was set on the original message.</param>
        /// <param name="timeout">The timeout in milliseconds</param>
        /// <param name="updateFrequency">Topic polling frequency.</param>
        /// <returns></returns>
        public static async Task<T> GetResponseAsync(object conversationId, int timeout = -1, int updateFrequency = 25)
        {
            Validate();
            var channelKey = ChannelKey(conversationId);
            var result = string.Empty;
            await Subscriber.SubscribeAsync(channelKey, (ch, val) => result = val).ConfigureAwait(false);

            // block until we get a result or timeout
            await TaskEx.WaitWhile(() => string.IsNullOrEmpty(result), updateFrequency, timeout);

            if (string.IsNullOrEmpty(result))
                return null;

            var response = JsonConvert.DeserializeObject<T>(result); // would be better if we could somehow align this with the serializer currently being used by NSB
            await Subscriber.UnsubscribeAsync(channelKey).ConfigureAwait(false);
            return response;
        }
    }

    public class NoSubscriberSetException : Exception { }
}