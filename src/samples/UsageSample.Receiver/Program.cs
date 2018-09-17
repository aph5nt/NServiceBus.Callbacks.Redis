using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.Callbacks.Redis;
using StackExchange.Redis;
using UsageSample.Messaging;

namespace UsageSample.Receiver
{
    class Program
    {
        static void Main(string[] args)
        {
            var services = new ServiceCollection();
            services.AddSingleton(ConnectionMultiplexer.Connect("localhost:6379").GetSubscriber());

            var config = new EndpointConfiguration("TestDestination");
            config.UseTransport<LearningTransport>();
            //config.UseTransport<RabbitMQTransport>()
            //    .ConnectionString("host=rabbitmq;virtualhost=/;usetls=false;username=user;password=bitnami")
            //    .UseConventionalRoutingTopology();
            //config.EnableInstallers();
            config.UsePersistence<LearningPersistence>();
            config.UseSerialization<NewtonsoftSerializer>();
            config.UseContainer<ServicesBuilder>(c => c.ExistingServices(services));

            var session = Endpoint.Start(config);
            Task.Delay(Timeout.Infinite).GetAwaiter().GetResult();
        }
    }

    public class TestCommandHandler : IHandleMessages<TestCommand>
    {
        private readonly ISubscriber _subscriber;

        public TestCommandHandler(ISubscriber subscriber)
        {
            _subscriber = subscriber;
        }

        public async Task Handle(TestCommand message, IMessageHandlerContext context)
        {
            await context.Reply<TestReply>(
                _subscriber, r =>
                {
                    r.ReplyValue = $"Hello from receiver! Your values were {message.Property1} and {message.Property2}!";
                });
        }
    }
}
