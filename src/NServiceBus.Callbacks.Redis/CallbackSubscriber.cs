using StackExchange.Redis;

namespace NServiceBus.Callbacks.Redis
{
    public abstract class CallbackSubscriber
    {
        // Locator anti-pattern but I'm not sorry. This is used to reduce 
        // the ceremony around instantiation of a new handler. It's not
        // our implementation anyway, so it doesn't need unit testing.

        protected static ISubscriber Subscriber; 

        public static void UseSubscriber(ISubscriber subscriber) => Subscriber = subscriber;

        /// <summary>
        /// Ensures that subscriber is set.
        /// </summary>
        public static void Validate()
        {
            if (Subscriber == null)
                throw new NoSubscriberSetException();
        }
    }
}