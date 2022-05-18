using System;
using System.Collections.Generic;
using Confluent.Kafka;

namespace Foundatio.Messaging;

public class KafkaMessageBusOptions : SharedMessageBusOptions {
    /// <summary>
    /// bootstrap.servers
    /// </summary>
    /// <value>
    /// The boot strap servers.
    /// </value>
    public string BootStrapServers { get; set; }

    /// <summary>
    /// group.id
    /// </summary>
    /// <value>
    /// The group identifier.
    /// </value>
    public string GroupId { get; set; }
    public string ConsumerGroupId { get; set; }

    /// <summary>
    /// { "auto.commit.interval.ms", 5000 },
    /// </summary>
    /// <value>
    /// The automatic commit interval ms.
    /// </value>
    public int AutoCommitIntervalMs { get; set; } = 5000;

    /// <summary>
    /// { "auto.offset.reset", "earliest" }
    /// </summary>
    /// <value>
    /// The automatic off set reset.
    /// </value>
    public string AutoOffSetReset { get; set; }

    public IDictionary<string, object> Arguments { get; set; }

    public SecurityProtocol SslCertificateLocation { get; set; }
    public SaslMechanism? SaslMechanism { get; set; }

    public string SaslUsername { get; set; }
    public string SaslPassword { get; set; }
    public string SslCaLocation { get; set; }
    public SecurityProtocol SecurityProtocol { get; set; }
}

public class KafkaMessageBusOptionsBuilder : SharedMessageBusOptionsBuilder<KafkaMessageBusOptions, KafkaMessageBusOptionsBuilder> {
    public KafkaMessageBusOptionsBuilder BootStrapServers(string bootstrapServers) {
        Target.BootStrapServers = bootstrapServers ?? throw new ArgumentNullException(nameof(bootstrapServers));
        return this;
    }

    public KafkaMessageBusOptionsBuilder GroupId(string groupId) {
        Target.GroupId = groupId ?? throw new ArgumentNullException(nameof(groupId));
        return this;
    }

    public KafkaMessageBusOptionsBuilder ConsumerGroupId(string consumerGroupId) {
        Target.ConsumerGroupId = consumerGroupId ?? throw new ArgumentNullException(nameof(consumerGroupId));
        return this;
    }

    public KafkaMessageBusOptionsBuilder AutoCommitIntervalMs(int autoCommitIntervalMs) {
        Target.AutoCommitIntervalMs = autoCommitIntervalMs;
        return this;
    }

    public KafkaMessageBusOptionsBuilder AutoOffSetReset(string autoOffSetReset) {
        Target.AutoOffSetReset = autoOffSetReset ?? throw new ArgumentNullException(nameof(autoOffSetReset));
        return this;
    }

    public KafkaMessageBusOptionsBuilder Arguments(IDictionary<string, object> arguments) {
        Target.Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        return this;
    }
    public KafkaMessageBusOptionsBuilder BootStrapServers2(string bootstrapServers) {
        Target.BootStrapServers = bootstrapServers ?? throw new ArgumentNullException(nameof(bootstrapServers));
        return this;
    }
    public KafkaMessageBusOptionsBuilder SslCertificateLocation(SecurityProtocol? securityProtocol) {
        Target.SslCertificateLocation = securityProtocol ?? throw new ArgumentNullException(nameof(securityProtocol));
        return this;
    }

    public KafkaMessageBusOptionsBuilder SaslMechanism(SaslMechanism? saslMechanism) {
        Target.SaslMechanism = saslMechanism ?? throw new ArgumentNullException(nameof(saslMechanism));
        return this;
    }

    public KafkaMessageBusOptionsBuilder SaslUsername(string saslUsername) {
        Target.SaslUsername = saslUsername ?? throw new ArgumentNullException(nameof(saslUsername));
        return this;
    }
    public KafkaMessageBusOptionsBuilder SaslPassword(string saslPassword) {
        Target.SaslPassword = saslPassword ?? throw new ArgumentNullException(nameof(saslPassword));
        return this;
    }
    public KafkaMessageBusOptionsBuilder SslCaLocation(string sslCaLocation) {
        Target.SslCaLocation = sslCaLocation ?? throw new ArgumentNullException(nameof(sslCaLocation));
        return this;
    }
    public KafkaMessageBusOptionsBuilder SecurityProtocol(SecurityProtocol? securityProtocol) {
        Target.SecurityProtocol = securityProtocol ?? throw new ArgumentNullException(nameof(securityProtocol));
        return this;
    }
    public KafkaMessageBusOptionsBuilder AutoCommitIntervalMs(int? autoCommitIntervalMs) {
        Target.AutoCommitIntervalMs = autoCommitIntervalMs ?? throw new ArgumentNullException(nameof(autoCommitIntervalMs));
        return this;
    }
}
