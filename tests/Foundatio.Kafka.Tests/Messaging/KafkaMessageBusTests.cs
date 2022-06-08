﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Foundatio.AsyncEx;
using Foundatio.Messaging;
using Foundatio.Tests.Extensions;
using Foundatio.Tests.Messaging;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Foundatio.Kafka.Tests.Messaging;

public class KafkaMessageBusTests : KafkaMessageBusTestBase {
    public KafkaMessageBusTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public override Task CanSendMessageAsync() {
        return base.CanSendMessageAsync();
    }

    [Fact]
    public override Task CanHandleNullMessageAsync() {
        return base.CanHandleNullMessageAsync();
    }

    [Fact]
    public override Task CanSendDerivedMessageAsync() {
        return base.CanSendDerivedMessageAsync();
    }

    [Fact]
    public override Task CanSendDelayedMessageAsync() {
        return base.CanSendDelayedMessageAsync();
    }

    [Fact]
    public override Task WillReceiveDerivedMessageTypesAsync() {
        return base.WillReceiveDerivedMessageTypesAsync();
    }

    [Fact]
    public override Task CanSubscribeToAllMessageTypesAsync() {
        return base.CanSubscribeToAllMessageTypesAsync();
    }

    [Fact]
    public override Task CanSubscribeToRawMessagesAsync() {
        return base.CanSubscribeToRawMessagesAsync();
    }

    [Fact]
    public override Task WontKeepMessagesWithNoSubscribersAsync() {
        return base.WontKeepMessagesWithNoSubscribersAsync();
    }

    [Fact]
    public override void CanDisposeWithNoSubscribersOrPublishers() {
        base.CanDisposeWithNoSubscribersOrPublishers();
    }

    [Fact]
    public async Task CanPersistAndNotLoseMessages() {
        var messageBus1 = GetMessageBus();
        try {
            var countdownEvent = new AsyncCountdownEvent(1);
            var cts = new CancellationTokenSource();
            await messageBus1.SubscribeAsync<SimpleMessageA>(msg => {
                _logger.LogInformation("[Subscriber1] Got message: {Message}", msg.Data);
                countdownEvent.Signal();
            }, cts.Token);

            await messageBus1.PublishAsync(new SimpleMessageA { Data = "Audit message 1" });
            await countdownEvent.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(0, countdownEvent.CurrentCount);
            cts.Cancel();

            await messageBus1.PublishAsync(new SimpleMessageA { Data = "Audit message 2" });

            cts = new CancellationTokenSource();
            countdownEvent.AddCount(1);
            await messageBus1.SubscribeAsync<SimpleMessageA>(msg => {
                _logger.LogInformation("[Subscriber2] Got message: {Message}", msg.Data);
                countdownEvent.Signal();
            }, cts.Token);
            await countdownEvent.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(0, countdownEvent.CurrentCount);
            cts.Cancel();

            await messageBus1.PublishAsync(new SimpleMessageA { Data = "Audit offline message 1" });
            await messageBus1.PublishAsync(new SimpleMessageA { Data = "Audit offline message 2" });
            await messageBus1.PublishAsync(new SimpleMessageA { Data = "Audit offline message 3" });

            messageBus1.Dispose();

            using var messageBus2 = GetMessageBus();

            cts = new CancellationTokenSource();
            countdownEvent.AddCount(4);
            await messageBus2.SubscribeAsync<SimpleMessageA>(msg => {
                _logger.LogInformation("[Subscriber3] Got message: {Message}", msg.Data);
                countdownEvent.Signal();
            }, cts.Token);
            await messageBus2.PublishAsync(new SimpleMessageA { Data = "Another audit message 4" });
            await countdownEvent.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(0, countdownEvent.CurrentCount);
        } finally {
            await CleanupMessageBusAsync(messageBus1);
        }
    }
}