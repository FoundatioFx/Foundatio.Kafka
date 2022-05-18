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

    /// <summary>
    ///  Path to client's public key (PEM) used for authentication. default: '' importance:low
    /// </summary>
    public SecurityProtocol SslCertificateLocation { get; set; }
    /// <summary>
    ///SASL mechanism to use for authentication. Supported: GSSAPI, PLAIN, SCRAM-SHA-256,
    ///SCRAM-SHA-512. **NOTE**: Despite the name, you may not configure more than one
    ///mechanism.
    /// </summary>
    public SaslMechanism? SaslMechanism { get; set; }
    /// <summary>
    /// SASL username for use with the PLAIN and SASL-SCRAM-.. mechanisms default: importance: high
    /// </summary>
    public string SaslUsername { get; set; }
    /// <summary>
    /// SASL password for use with the PLAIN and SASL-SCRAM-
    /// </summary>
    public string SaslPassword { get; set; }
    /// <summary>
    /// File or directory path to CA certificate(s) for verifying the broker's key. Defaults:
    ///     On Windows the system's CA certificates are automatically looked up in the Windows
    ///     Root certificate store. On Mac OSX this configuration defaults to `probe`. It
    ///     is recommended to install openssl using Homebrew, to provide CA certificates.
    ///     On Linux install the distribution's ca-certificates package. If OpenSSL is statically
    ///     linked or `ssl.ca.location` is set to `probe` a list of standard paths will be
    ///     probed and the first one found will be used as the default CA certificate location
    ///     path. If OpenSSL is dynamically linked the OpenSSL library's default path will
    ///     be used (see `OPENSSLDIR` in `openssl version -a`). default: '' importance: low
    /// </summary>
    public string SslCaLocation { get; set; }
    /// <summary>
    /// Protocol used to communicate with brokers. default: plaintext importance: high
    /// </summary>
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
