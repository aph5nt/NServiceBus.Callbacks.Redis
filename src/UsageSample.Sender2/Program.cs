using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.Callbacks.Redis;
using StackExchange.Redis;
using UsageSample.Messaging;

namespace UsageSample.Sender2
{
    class Program
    {
        public async Task RunSample(IServiceProvider provider, IMessageSession bus)
        {
            var conversationId = Guid.NewGuid();
            await bus.Send<TestCommand>("TestDestination", conversationId, cmd =>
            {
                cmd.Property1 = "baz";
                cmd.Property2 = "bat";
            });

            var reply = await TestReplyHandler.GetResponseAsync(conversationId);
            Console.WriteLine($"Received reply \"{reply.ReplyValue}\"");
        }

        private static void AddRedis(IServiceCollection services)
        {
            var mp = ConnectionMultiplexer.Connect("localhost:6379");
            services.AddSingleton<IConnectionMultiplexer>(mp);
            services.AddSingleton(p =>
            {
                var subscriber = p.GetRequiredService<IConnectionMultiplexer>().GetSubscriber();
                return subscriber;
            });
        }

        private static IMessageSession ConfigureSession(IServiceCollection services)
        {
            var config = new EndpointConfiguration("Sender2");
            config.UseTransport<LearningTransport>();
            config.UsePersistence<LearningPersistence>();
            config.UseContainer<ServicesBuilder>(c => c.ExistingServices(services));
            config.UseSerialization<NewtonsoftSerializer>();

            var provider = services.BuildServiceProvider();
            CallbackSubscriber.UseSubscriber(provider.GetRequiredService<ISubscriber>());

            return Endpoint.Start(config).Result;
        }

        static void Main(string[] args)
        {
            var services = new ServiceCollection();
            AddRedis(services);
            var session = ConfigureSession(services);
            var provider = services.BuildServiceProvider();

            Console.WriteLine("Press Enter to Run Test for sender 2");
            Console.ReadKey();
            var p = new Program();
            p.RunSample(provider, session).GetAwaiter().GetResult();
            Console.WriteLine("Sample finished. Press enter to close.");
            Console.ReadKey();
        }
    }
}
