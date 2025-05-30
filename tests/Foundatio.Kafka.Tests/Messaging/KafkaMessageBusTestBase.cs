using System;
using System.Threading.Tasks;
using Foundatio.Messaging;
using Foundatio.Tests.Messaging;
using Xunit.Abstractions;

namespace Foundatio.Kafka.Tests.Messaging;

public class KafkaMessageBusTestBase : MessageBusTestBase
{
    protected readonly string Topic = $"test_{Guid.NewGuid().ToString("N")[..10]}";
    protected readonly string GroupId = $"group_{Guid.NewGuid().ToString("N")[..10]}";

    public KafkaMessageBusTestBase(ITestOutputHelper output) : base(output)
    {
        EnableTopicDeletion = true;
    }

    protected override IMessageBus GetMessageBus(Func<SharedMessageBusOptions, SharedMessageBusOptions> config = null)
    {
        return new KafkaMessageBus(o => o
            .BootstrapServers("127.0.0.1:9092")
            .Topic(Topic)
            .TopicReplicationFactor(1)
            .TopicNumberOfPartitions(1)
            .GroupId(GroupId)
            .AllowAutoCreateTopics(true)
            //.Debug("consumer,cgrp,topic,fetch")
            .LoggerFactory(Log)
        );
    }

    protected override async Task CleanupMessageBusAsync(IMessageBus messageBus)
    {
        await base.CleanupMessageBusAsync(messageBus);

        if (EnableTopicDeletion && messageBus is IKafkaMessageBus kafkaMessageBus)
            await kafkaMessageBus.DeleteTopicAsync();
    }

    public bool EnableTopicDeletion { get; set; }
}
