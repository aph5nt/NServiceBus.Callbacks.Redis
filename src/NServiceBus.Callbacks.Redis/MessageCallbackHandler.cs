using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace NServiceBus.Callbacks.Redis
{
    public static class SessionEx
    {
        public static Task Send(this IMessageSession context, string destination, object conversationId, object message, SendOptions options = null)
        {
            var opt = options ?? new SendOptions();
            opt.SetDestination(destination);
            opt.SetHeader(Headers.ConversationId, conversationId.ToString());
            return context.Send(message, opt);
        }

        public static Task Send<T>(this IMessageSession context, string destination, object conversationId, Action<T> constructor, SendOptions options = null)
        {
            var message = Activator.CreateInstance<T>();
            constructor(message);
            return Send(context, destination, conversationId, message, options);
        }
    }

    public abstract class RedisCallbackSubscriber
    {
        protected static ISubscriber Subscriber; // the same instance used application-wide

        public static void UseSubscriber(ISubscriber subscriber) => Subscriber = subscriber;
    }

    public abstract class MessageCallback<T> : RedisCallbackSubscriber, IHandleMessages<T>
        where T : class
    {
        private const string KeyFormat = "nsbcallback-{0}";

        private static string ChannelKey(object conversationId) => string.Format(KeyFormat, conversationId);

        public async Task Handle(T message, IMessageHandlerContext context)
        {
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
}
