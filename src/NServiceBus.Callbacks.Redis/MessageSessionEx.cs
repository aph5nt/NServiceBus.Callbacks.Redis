namespace NServiceBus.Callbacks.Redis
{
    using System;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using StackExchange.Redis;

    public static class MessageSessionEx
    {
        internal const string ReplyTopicHeader = "reply-topic"; // since we're modifying how replies work, would it be prudent to just use the reply to address header?
        internal const string ReplyTopicFormat = "nsbreply-{0}";

        // REFACTOR?: see if it's possible to access NSB's IoC container here so we don't
        // have to require the parameter? Will that create an interface conflict with NServiceBus.Core?

        /// <summary>
        /// Sends a request to the endpoint and then returns a handle that can be awaited for response.
        /// </summary>
        /// <typeparam name="TReplyType"></typeparam>
        /// <param name="context">The context</param>
        /// <param name="subscriber">Instance of <see cref="ISubscriber"/></param>
        /// <param name="message">The command message.</param>
        /// <param name="options">Options</param>
        /// <returns></returns>
        public static async Task<TReplyType> Request<TReplyType>(
            this IMessageSession context,
            ISubscriber subscriber,
            object message,
            SendOptions options) where TReplyType : class, IMessage
        {
            // allows caller to set their own ID
            if (!options.GetHeaders().TryGetValue(Headers.ConversationId, out var conversationId))
            {
                conversationId = Guid.NewGuid().ToString();
                options.SetHeader(Headers.ConversationId, Guid.NewGuid().ToString());
            }

            // set the channel name for consistency   
            var channelName = string.Format(ReplyTopicFormat, conversationId);
            options.SetHeader(ReplyTopicHeader, channelName);

            await context.Send(message, options).ConfigureAwait(false);

            var handle = new ResponseHandle<TReplyType>(subscriber, channelName);
            return await handle.GetResponseAsync();
        }

        /// <summary>
        /// Sends a request to the endpoint and then returns a handle that can be awaited for response.
        /// </summary>
        /// <typeparam name="TReplyType">The type of the expected reply.</typeparam>
        /// <typeparam name="TCommandType">The command type.</typeparam>
        /// <param name="context">The context</param>
        /// <param name="subscriber">Instance of <see cref="ISubscriber"/></param>
        /// <param name="constructor">Action that will be executed against an empty instance of <see cref="{TCommandType}"/></param>
        /// <param name="options">Options</param>
        /// <returns></returns>
        public static Task<TReplyType> Request<TCommandType, TReplyType>(
            this IMessageSession context, 
            ISubscriber subscriber, 
            Action<TCommandType> constructor, 
            SendOptions options = null) where TReplyType : class, IMessage
        {
            var message = Activator.CreateInstance<TCommandType>();
            constructor(message);
            return Request<TReplyType>(context, subscriber, message, options);
        }

        /// <summary>
        /// Replies to the original caller over a redis channel.
        /// </summary>
        /// <param name="context">The message context.</param>
        /// <param name="subscriber">Instance of <see cref="ISubscriber"/></param>
        /// <param name="message">The reply message</param>
        /// <returns></returns>
        public static Task Reply(this IMessageHandlerContext context, ISubscriber subscriber, object message)
        {
            if(!context.MessageHeaders.TryGetValue(ReplyTopicHeader, out var replyTopic))
                throw new InvalidOperationException("No reply topic was found on message headers.");

            var json = JsonConvert.SerializeObject(message);
            return subscriber.PublishAsync(replyTopic, json);
        }

        /// <summary>
        /// Replies to the original caller over a redis channel.
        /// </summary>
        /// <param name="context">The message context.</param>
        /// <param name="subscriber">Instance of <see cref="ISubscriber"/></param>
        /// <param name="constructor">Action that will be executed against an empty instance of <see cref="{T}"/></param>
        /// <returns></returns>
        public static Task Reply<T>(this IMessageHandlerContext context, ISubscriber subscriber, Action<T> constructor)
        {
            var message = Activator.CreateInstance<T>();
            constructor(message);
            return Reply(context, subscriber, message);
        }
    }
}
