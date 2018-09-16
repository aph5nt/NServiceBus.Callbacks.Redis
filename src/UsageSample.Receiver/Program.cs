using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using UsageSample.Messaging;

namespace UsageSample.Receiver
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new EndpointConfiguration("TestDestination");
            config.UseTransport<LearningTransport>();
            //config.UseTransport<RabbitMQTransport>()
            //    .ConnectionString("host=rabbitmq;virtualhost=/;usetls=false;username=user;password=bitnami")
            //    .UseConventionalRoutingTopology();
            //config.EnableInstallers();
            config.UsePersistence<LearningPersistence>();
            config.UseSerialization<NewtonsoftSerializer>();

            var session = Endpoint.Start(config);
            Task.Delay(Timeout.Infinite).GetAwaiter().GetResult();
        }
    }

    public class TestCommandHandler : IHandleMessages<TestCommand>
    {
        public async Task Handle(TestCommand message, IMessageHandlerContext context)
        {
            await context.Reply<TestReply>(r => { r.ReplyValue = $"Hello from receiver! Your values were {message.Property1} and {message.Property2}!"; });
        }
    }
}
