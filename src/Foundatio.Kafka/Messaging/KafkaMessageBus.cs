using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Foundatio.Extensions;
using Foundatio.Serializer;
using Microsoft.Extensions.Logging;


namespace Foundatio.Messaging;

public class KafkaMessageBus : MessageBusBase<KafkaMessageBusOptions> {
    private bool _isDisposed;
    private CancellationTokenSource _messageBusDisposedCancellationTokenSource = new CancellationTokenSource();
    private Task _listeningTask;
    //private IConsumer<string, string> _consumer = null;  // We have to figure out the typing on here...
    private ClientConfig _clientConfig;
    private string topicName = "topic2";
    public KafkaMessageBus(KafkaMessageBusOptions options) : base(options) {
    }

    public KafkaMessageBus(Builder<KafkaMessageBusOptionsBuilder, KafkaMessageBusOptions> config)
        : this(config(new KafkaMessageBusOptionsBuilder()).Build()) {
    }

    protected override Task PublishImplAsync(string messageType, object message, MessageOptions options, CancellationToken cancellationToken) {
        IProducer<string, KafkaMessageEnvelope> producer = null;
        if (_clientConfig == null)
            _clientConfig = CreateClientConfig();
        try {
            var producerConfig = CreateProducerConfig(_clientConfig);
            var kafkaMessage = new KafkaMessageEnvelope {
                Type = messageType,
                Data = SerializeMessageBody(messageType, message)
            };

            producer = new ProducerBuilder<string, KafkaMessageEnvelope>(producerConfig).SetValueSerializer(new KafkaSerializer(_serializer)).Build();

            producer.Produce(topicName, new Message<string, KafkaMessageEnvelope> { Key = messageType, Value = kafkaMessage },
            (deliveryReport) => {
                if (deliveryReport.Error.Code != ErrorCode.NoError) {
                    Console.WriteLine($"Failed to deliver message: {deliveryReport.Error.Reason}");
                } else {
                    Console.WriteLine($"Produced message to: {deliveryReport.TopicPartitionOffset}");
                }
            });

            return Task.CompletedTask;

        } finally {
            var queueSize = producer.Flush(TimeSpan.FromSeconds(15));
            if (queueSize > 0) {
                Console.WriteLine("WARNING: Producer event queue has " + queueSize + " pending events on exit.");
            }
            producer.Dispose();
        }
    }
 
    protected override Task EnsureTopicSubscriptionAsync(CancellationToken cancellationToken) {
        // Check if we create topic here / check redis / rabbit implementation, we cache the result under isSubscribed and use an async lock.

        EnsureListening();
        return Task.CompletedTask;
    }
    private ClientConfig CreateClientConfig() {
        var clientConfig = new ClientConfig();
        clientConfig.BootstrapServers = _options.BootStrapServers;
        clientConfig.SecurityProtocol = _options.SslCertificateLocation;
        clientConfig.SaslMechanism = _options.SaslMechanism;
        clientConfig.SaslUsername = _options.SaslUsername;
        clientConfig.SaslPassword = _options.SaslPassword;
        clientConfig.SslCaLocation = _options.SslCaLocation;
        return clientConfig;
    }
    private ProducerConfig CreateProducerConfig(ClientConfig clientConfig = null) {
        if (clientConfig is null)
            clientConfig = CreateClientConfig();
        var config = new ProducerConfig(clientConfig);
        return config;
    }
    private ConsumerConfig CreateConsumerConfig(ClientConfig clientConfig = null) {
        if (clientConfig is null)
            clientConfig = CreateClientConfig();
        var config = new ConsumerConfig(clientConfig) {
            GroupId = "testgroup",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };
        return config;
    }

    private async Task OnMessageAsync<TKey, TValue>(ConsumeResult<TKey, TValue> consumeResult) where TValue : KafkaMessageEnvelope {
        if (_subscribers.IsEmpty)
            return;

        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace("OnMessage([{Offset}] {Partition})", consumeResult.Offset, consumeResult.TopicPartitionOffset);

        IMessage message = null;
        try {
            message = ConvertToMessage(consumeResult.Message.Value);
        } catch (Exception ex) {
            _logger.LogWarning(ex, "OnMessage({Offset}] {Partition}) Error deserializing message: {Message}", consumeResult.Offset, consumeResult.TopicPartition.Partition, ex.Message);
            return;
        }
        await SendMessageToSubscribersAsync(message).AnyContext();
    }

    protected virtual IMessage ConvertToMessage(KafkaMessageEnvelope envelope) {
        return new Message(() => DeserializeMessageBody(envelope.Type, envelope.Data)) {
            Type = envelope.Type,
            ClrType = GetMappedMessageType(envelope.Type),
            Data = envelope.Data
        };
    }

    private void EnsureListening() {
        if (_listeningTask is not null) {
            _logger.LogInformation("StartListening: Already listening");
            return;
        }
        if (_clientConfig == null)
            _clientConfig = CreateClientConfig();
        var consumerConfig = CreateConsumerConfig(_clientConfig);
        _listeningTask = Task.Run(async () => {

            using (var consumer = new ConsumerBuilder<string, KafkaMessageEnvelope>(consumerConfig).SetValueDeserializer(new KafkaSerializer(_serializer)).Build()) {
                consumer.Subscribe(topicName);

                try {
                    while (!_messageBusDisposedCancellationTokenSource.IsCancellationRequested) {
                        var consumeResult = consumer.Consume(_messageBusDisposedCancellationTokenSource.Token);
                        await OnMessageAsync(consumeResult).AnyContext();
                    }
                } catch (OperationCanceledException) {
                } catch (Exception ex) {
                    _logger.LogDebug(ex, "Error consuming message: {Message}", ex.Message);
                } finally {
                    consumer.Close();
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
    public class KafkaMessageEnvelope {
        public string Type { get; set; }
        public byte[] Data { get; set; }
    }
    public class KafkaSerializer : Confluent.Kafka.ISerializer<KafkaMessageEnvelope>, IDeserializer<KafkaMessageEnvelope> {
        private readonly ISerializer _serializer;
        public KafkaSerializer(ISerializer serializer) {
            _serializer = serializer;
        }
        public byte[] Serialize(KafkaMessageEnvelope data, SerializationContext context) {
            byte[] result = _serializer.SerializeToBytes(data);
            return result;
        }
        public KafkaMessageEnvelope Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context) {
            using (var stream = new MemoryStream(data.ToArray())) {
                return _serializer.Deserialize<KafkaMessageEnvelope>(stream);
            }
        }
    }
}