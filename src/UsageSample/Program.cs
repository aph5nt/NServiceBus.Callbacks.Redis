using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.Callbacks.Redis;
using StackExchange.Redis;
using UsageSample.Messaging;
using UsageSample.Sender1;
using XidNet;

namespace UsageSample.Sender
{
    class Program
    {
        private string _instanceId;

        public async Task RunSample(IServiceProvider provider, IMessageSession bus, string barValue)
        {
            var conversationId = Xid.NewXid();
            await bus.Send<TestCommand>("TestDestination", conversationId, cmd =>
            {
                cmd.Property1 = "foo";
                cmd.Property2 = barValue;
            });

            var reply = await TestReplyHandler.GetResponseAsync(conversationId);
            Console.WriteLine($"Instance {_instanceId} received reply \"{reply.ReplyValue}\"");
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
            var config = new EndpointConfiguration("Sender");
            config.UseTransport<LearningTransport>();
            //config.UseTransport<RabbitMQTransport>()
            //    .ConnectionString("host=rabbitmq;virtualhost=/;usetls=false;username=user;password=bitnami")
            //    .UseConventionalRoutingTopology();
            //config.EnableInstallers();

            config.UsePersistence<LearningPersistence>();
            config.UseContainer<ServicesBuilder>(c => c.ExistingServices(services));
            config.UseSerialization<NewtonsoftSerializer>();

            var provider = services.BuildServiceProvider();
            CallbackSubscriber.UseSubscriber(provider.GetRequiredService<ISubscriber>());

            return Endpoint.Start(config).Result;
        }

        static void Main(string[] args)
        {
            var s1 = new ServiceCollection();
            AddRedis(s1);
            var session1 = ConfigureSession(s1);

            var s2 = new ServiceCollection();
            AddRedis(s2);
            var session2 = ConfigureSession(s2);

            var consumer1 = new Program{ _instanceId = Xid.NewXid().ToString()};
            var consumer2 = new Program{_instanceId = Xid.NewXid().ToString()};

            Console.WriteLine("Press Enter to run simulation");
            Console.ReadKey();
            Task.WaitAll(
                consumer1.RunSample(s1.BuildServiceProvider(), session1, "abcd"),
                consumer2.RunSample(s2.BuildServiceProvider(), session2, "1234"));

            Console.WriteLine("Sample finished. Press enter to close.");
            Console.ReadKey();
        }
    }
}
