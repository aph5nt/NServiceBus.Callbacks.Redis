using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.Callbacks.Redis;
using StackExchange.Redis;
using UsageSample.Messaging;
using XidNet;

namespace UsageSample.Sender
{
    class Program
    {
        private string _instanceId;

        public async Task RunSample(IServiceProvider provider, IMessageSession bus, string barValue)
        {
            var subscriber = provider.GetRequiredService<ISubscriber>();
            
            var options = new SendOptions();
            options.SetDestination("TestDestination");
            
            var reply = await bus.Request<TestCommand, TestReply>(
                subscriber, cmd =>
                {
                    cmd.Property1 = "foo";
                    cmd.Property2 = barValue;
                }, options);

            Console.WriteLine($"Instance {_instanceId} received reply \"{reply.ReplyValue}\"");
        }

        private static void AddRedis(IServiceCollection services)
        {
            services.AddSingleton(ConnectionMultiplexer.Connect("localhost:6379").GetSubscriber());
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
            
            return Endpoint.Start(config).Result;
        }

        static void Main(string[] args)
        {
            // simulate two competing consumers

            var s1 = new ServiceCollection();
            AddRedis(s1);
            var session1 = ConfigureSession(s1);
            var provider1 = s1.BuildServiceProvider();
            
            var s2 = new ServiceCollection();
            AddRedis(s2);
            var session2 = ConfigureSession(s2);
            var provider2 = s2.BuildServiceProvider();

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
