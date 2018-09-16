using System;
using StackExchange.Redis;

namespace NServiceBus.Callbacks.Redis
{
    public abstract class CallbackSubscriber
    {
        private static Func<ISubscriber> _subscriberFactory;

        protected static ISubscriber Subscriber => _subscriberFactory(); 
        
        /// <summary>
        /// Sets the subscriber factory.
        /// </summary>
        /// <param name="subscriberFactory"></param>
        public static void UseSubscriberFactory(Func<ISubscriber> subscriberFactory) => _subscriberFactory = subscriberFactory;

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