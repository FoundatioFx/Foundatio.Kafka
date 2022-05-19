using System;
using System.IO;
using System.Linq;
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
    private readonly AdminClientConfig _adminClientConfig;
    private readonly ProducerConfig _producerConfig;
    private readonly ConsumerConfig _consumerConfig;
    private readonly IProducer<string, KafkaMessageEnvelope> _producer;

    public KafkaMessageBus(KafkaMessageBusOptions options) : base(options) {
        _adminClientConfig = CreateAdminConfig();
        _consumerConfig = CreateConsumerConfig();
        _producerConfig = CreateProducerConfig();
        _producer = new ProducerBuilder<string, KafkaMessageEnvelope>(_producerConfig).SetValueSerializer(new KafkaSerializer(_serializer)).Build();
    }

    public KafkaMessageBus(Builder<KafkaMessageBusOptionsBuilder, KafkaMessageBusOptions> config)
        : this(config(new KafkaMessageBusOptionsBuilder()).Build()) {
    }

    protected override Task PublishImplAsync(string messageType, object message, MessageOptions options, CancellationToken cancellationToken) {
        ///Todo: review logging
        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace("PublishImplAsync([{messageType}])", messageType);
        ///Get rid of envelope (short type and header)
        var kafkaMessage = new KafkaMessageEnvelope {
            Type = messageType,
            Data = SerializeMessageBody(messageType, message)
        };

        _producer.Produce(_options.Topic, new Message<string, KafkaMessageEnvelope> { Key = messageType, Value = kafkaMessage },
        (deliveryReport) => {
            ///rethrow error and logging ? rabbitmq
            if (deliveryReport.Error.Code != ErrorCode.NoError) {
                _logger.LogTrace("Failed to deliver message: {Reason}", deliveryReport.Error.Reason);
            } else {
                _logger.LogTrace("Produced message to: {TopicPartitionOffset}", deliveryReport.TopicPartitionOffset);
            }
        });
        return Task.CompletedTask;
    }

    private async Task OnMessageAsync<TKey, TValue>(ConsumeResult<TKey, TValue> consumeResult) where TValue : KafkaMessageEnvelope {
        if (_subscribers.IsEmpty)
            return;
        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace("OnMessage( topic: {Topic},groupId: {GroupId} partition: [{Partition}] offset: [{Offset}] partitionOffset; [{TopicPartitionOffset}])", consumeResult.Topic,_consumerConfig.GroupId, consumeResult.Partition, consumeResult.Offset, consumeResult.TopicPartitionOffset);

        IMessage message = null;
        try {
            message = ConvertToMessage(consumeResult.Message.Value);
        } catch (Exception ex) {
            _logger.LogWarning(ex, "OnMessage({Offset}] {Partition}) Error deserializing message: {Message}", consumeResult.Offset, consumeResult.TopicPartition.Partition, ex.Message);
            return;
        }

        await SendMessageToSubscribersAsync(message).AnyContext();
    }

    protected override async Task EnsureTopicSubscriptionAsync(CancellationToken cancellationToken) {
        // Check if we create topic here / check redis / rabbit implementation, we cache the result under isSubscribed and use an async lock.
        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace("EnsureTopicSubscriptionAsync");
        await EnsureTopicCreatedAsync();
        EnsureListening();
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
            _logger.LogDebug("StartListening: Already listening");
            return;
        }

        _listeningTask = Task.Run(async () => {
            using (var consumer = new ConsumerBuilder<string, KafkaMessageEnvelope>(_consumerConfig).SetValueDeserializer(new KafkaSerializer(_serializer)).Build()) {
                consumer.Subscribe(_options.Topic);
                
                _logger.LogInformation("EnsureListening consumer {Name} subscribed on {Topic}", consumer.Name, _options.Topic);

                try {
                    if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTrace("MessageBus {MessageBusId} dispose", MessageBusId);

                    while (!_messageBusDisposedCancellationTokenSource.IsCancellationRequested) {
                        var consumeResult = consumer.Consume(_messageBusDisposedCancellationTokenSource.Token);

                        if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTrace($"Consumed topic: {consumeResult.Topic} by consumer : {consumer.Name} partition {consumeResult.TopicPartition}");
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

        if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTrace("MessageBus {MessageBusId} dispose", MessageBusId);

        var queueSize = _producer?.Flush(TimeSpan.FromSeconds(15));
        if (queueSize > 0) {
            if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTrace("Flushing producer {queueSize}", queueSize);
        }
        _producer?.Dispose();

        _messageBusDisposedCancellationTokenSource.Cancel();
        _messageBusDisposedCancellationTokenSource.Dispose();
        base.Dispose();
    }

    private async Task EnsureTopicCreatedAsync() {
        if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTrace("EnsureTopicCreatedAsync {Topic}", _options.Topic);
        var adminConfig = CreateAdminConfig();
        using var adminClient = new AdminClientBuilder(adminConfig).Build() ;
        try {
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(2));
            bool isTopicExist = metadata.Topics.Any(t => t.Topic == _options.Topic);
            if (!isTopicExist)
                await adminClient.CreateTopicsAsync(new TopicSpecification[] { new TopicSpecification { Name = _options.Topic, ReplicationFactor = _options.TopicReplicationFactor, NumPartitions = _options.TopicNumberOfPartitions, Configs = _options.TopicConfigs, ReplicasAssignments = _options.TopicReplicasAssignments } });
            ///Logging and rethrow
        } catch (CreateTopicsException e) {
            if (e.Results[0].Error.Code != ErrorCode.TopicAlreadyExists) {
                if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTrace("An error occured creating topic {Topic}: {Reason}", _options.Topic, e.Results[0].Error.Reason);

            } else {
                if (_logger.IsEnabled(LogLevel.Trace)) _logger.LogTrace("Topic {Topic} already exists", _options.Topic);
            }
        } 
    }

    /// <summary>
    /// check if disposable, pass all options
    /// </summary>
    /// <returns></returns>
    private ClientConfig CreateClientConfig() {
        return new ClientConfig {
            BootstrapServers = _options.BootStrapServers,
            SecurityProtocol = _options.SslCertificateLocation,
            SaslMechanism = _options.SaslMechanism,
            SaslUsername = _options.SaslUsername,
            SaslPassword = _options.SaslPassword,
            SslCaLocation = _options.SslCaLocation,
        };
    }

    private AdminClientConfig CreateAdminConfig() {
        var _clientConfig = CreateClientConfig();
        var config = new AdminClientConfig(_clientConfig);
        return config;
    }
    /// <summary>
    /// check default values
    /// </summary>
    /// <returns></returns>
    private ProducerConfig CreateProducerConfig() {
        var _clientConfig = CreateClientConfig();
        var config = new ProducerConfig(_clientConfig) { EnableDeliveryReports = true };
        return config;
    }
    //set from options
    private ConsumerConfig CreateConsumerConfig() {
        var _clientConfig = CreateClientConfig();
        var config = new ConsumerConfig(_clientConfig) {
            GroupId = _options.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = _options.EnableAutoCommit,
            EnableAutoOffsetStore = _options.EnableAutoOffsetStore,
        };
        return config;
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
            return _serializer.SerializeToBytes(data);
        }

        public KafkaMessageEnvelope Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context) {
            using (var stream = new MemoryStream(data.ToArray())) {
                return _serializer.Deserialize<KafkaMessageEnvelope>(stream);
            }
        }
    }
}