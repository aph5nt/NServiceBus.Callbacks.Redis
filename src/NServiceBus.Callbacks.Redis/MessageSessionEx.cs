using System;
using System.Threading.Tasks;

namespace NServiceBus.Callbacks.Redis
{
    public static class MessageSessionEx
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
}
