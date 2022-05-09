using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Foundatio.Extensions;
using Microsoft.Extensions.Logging;

namespace Foundatio.Messaging; 

public class KafkaMessageBus : MessageBusBase<KafkaMessageBusOptions>
{
    private bool _isDisposed;
    private readonly CancellationTokenSource _messageBusDisposedCancellationTokenSource = new();
    private Task _listeningTask;
    private IConsumer<object> _consumer = null;  // We have to figure out the typing on here...
    
    public KafkaMessageBus(KafkaMessageBusOptions options) : base(options)
    {
    }

    public KafkaMessageBus(Builder<KafkaMessageBusOptionsBuilder, KafkaMessageBusOptions> config)
        : this(config(new KafkaMessageBusOptionsBuilder()).Build())
    {
    }

    protected override Task PublishImplAsync(string messageType, object message, MessageOptions options, CancellationToken cancellationToken) {
        throw new NotImplementedException();
    }

    protected override Task EnsureTopicSubscriptionAsync(CancellationToken cancellationToken) {
        // Check if we create topic here / check redis / rabbit implementation, we cache the result under isSubscribed and use an async lock.
        EnsureListening();
        return Task.CompletedTask;
    }
    
    private async Task OnMessageAsync<TKey, TValue>(ConsumeResult<TKey, TValue> consumeResult) {
        if (_subscribers.IsEmpty)
            return;

        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace("OnMessage([{Offset}] {Partition})", consumeResult.Offset, consumeResult.TopicPartition.Partition);
            
        IMessage message = null;
        try {
            /*
            var envelope = _serializer.Deserialize<RedisMessageEnvelope>((byte[])consumeResult.Message.Value);
            message = new Message(() => DeserializeMessageBody(envelope.Type, envelope.Data)) {
                Type = envelope.Type,
                Data = envelope.Data,
                ClrType = GetMappedMessageType(envelope.Type)
            };
            */
        } catch (Exception ex) {
            _logger.LogWarning(ex, "OnMessage({Offset}] {Partition}) Error deserializing message: {Message}", consumeResult.Offset, consumeResult.TopicPartition.Partition, ex.Message);
            return;
        }

        await SendMessageToSubscribersAsync(message).AnyContext();
    }

    private void EnsureListening() {
        if (_listeningTask is not null) {
            _logger.LogInformation("StartListening: Already listening");
            return;
        }

        _listeningTask = Task.Run(async () => {
            while (!_messageBusDisposedCancellationTokenSource.IsCancellationRequested) {
                try {
                    var consumeResult = _consumer.Consume(_messageBusDisposedCancellationTokenSource.Token);
                    await OnMessageAsync(consumeResult).AnyContext();
                } catch (OperationCanceledException) {
                } catch (Exception ex) {
                    _logger.LogDebug(ex, "Error consuming message: {Message}", ex.Message);
                }
            }
        }, _messageBusDisposedCancellationTokenSource.Token);
    }

    public override void Dispose() {
        if (_isDisposed) {
            _logger.LogTrace("MessageBus {MessageBusId} dispose was already called", MessageBusId);
            return;
        }
            
        _isDisposed = true;
            
        _logger.LogTrace("MessageBus {MessageBusId} dispose", MessageBusId);
        _messageBusDisposedCancellationTokenSource.Cancel();
        _messageBusDisposedCancellationTokenSource.Dispose();
    }
}