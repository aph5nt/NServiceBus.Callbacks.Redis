using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace NServiceBus.Callbacks.Redis
{
    public static class MessageSessionEx
    {
        private const string ReplyTopicHeader = "reply-topic";
        private const string ReplyTopicFormat = "nsbreply-{0}";

        // TODO: see if it's possible to access NSB's IoC container here so we don't
        // have to require the parameter. Will that create an interface conflict with NServiceBus.Core?

        public static async Task<ResponseHandle<TReplyType>> Request<TReplyType>(
            this IMessageSession context,
            ISubscriber subscriber,
            object message,
            SendOptions options) where TReplyType : class, IMessage
        {
            var conversationId = Guid.NewGuid();
            var channelName = string.Format(ReplyTopicFormat, conversationId);
            options.SetHeader(Headers.ConversationId, conversationId.ToString());
            options.SetHeader(ReplyTopicHeader, channelName);

            await context.Send(message, options).ConfigureAwait(false);

            return new ResponseHandle<TReplyType>(subscriber, channelName);
        }

        public static Task<ResponseHandle<TReplyType>> Request<TCommandType, TReplyType>(
            this IMessageSession context, 
            ISubscriber subscriber, 
            Action<TCommandType> constructor, 
            SendOptions options = null) where TReplyType : class, IMessage
        {
            var message = Activator.CreateInstance<TCommandType>();
            constructor(message);
            return Request<TReplyType>(context, subscriber, message, options);
        }

        public static Task Reply(this IMessageHandlerContext context, ISubscriber subscriber, object message)
        {
            if(!context.MessageHeaders.TryGetValue(ReplyTopicHeader, out var replyTopic))
                throw new InvalidOperationException("No reply topic was found on message headers.");

            var json = JsonConvert.SerializeObject(message);
            return subscriber.PublishAsync(replyTopic, json);
        }

        public static Task Reply<T>(this IMessageHandlerContext context, ISubscriber subscriber, Action<T> constructor)
        {
            var message = Activator.CreateInstance<T>();
            constructor(message);
            return Reply(context, subscriber, message);
        }
    }
}
