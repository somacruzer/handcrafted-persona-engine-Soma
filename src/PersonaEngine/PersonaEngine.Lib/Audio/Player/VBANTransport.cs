using System.Net.Sockets;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PersonaEngine.Lib.Audio.Player;

/// <summary>
///     Implementation of IAudioTransport for VBAN protocol over UDP.
/// </summary>
public class VBANTransport : IAudioTransport
{
    private readonly string _destHost;

    private readonly int _destPort;

    private readonly ILogger _logger;

    private readonly IVBANPacketBuilder _packetBuilder;

    private bool _disposed;

    private UdpClient? _udpClient;

    public VBANTransport(
        string                  destHost,
        int                     destPort,
        IVBANPacketBuilder      packetBuilder,
        ILogger<VBANTransport>? logger = null)
    {
        _destHost      = destHost ?? throw new ArgumentNullException(nameof(destHost));
        _destPort      = destPort > 0 ? destPort : throw new ArgumentException("Port must be positive", nameof(destPort));
        _packetBuilder = packetBuilder ?? throw new ArgumentNullException(nameof(packetBuilder));
        _logger        = logger ?? NullLogger<VBANTransport>.Instance;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _udpClient                       = new UdpClient(_destHost, _destPort);
            _udpClient.Client.SendBufferSize = 65536; // Set a larger buffer for better performance
            await Task.CompletedTask;                 // For async consistency
        }
        catch (Exception ex)
        {
            throw new AudioException($"Failed to initialize UDP client to {_destHost}:{_destPort}", ex);
        }
    }

    public async Task SendAudioPacketAsync(
        ReadOnlyMemory<float> audioData,
        int                   sampleRate,
        int                   samplesPerChannel,
        int                   channels,
        CancellationToken     cancellationToken)
    {
        if ( _udpClient == null )
        {
            throw new InvalidOperationException("Transport not initialized");
        }

        try
        {
            var packet = _packetBuilder.BuildPacket(audioData, sampleRate, samplesPerChannel, channels);
            await _udpClient.SendAsync(packet, packet.Length);
        }
        catch (SocketException ex)
        {
            _logger.LogDebug(ex, "UDP send error");
        }
        catch (OperationCanceledException)
        {
            // Just rethrow cancellation
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending VBAN packet");

            throw new AudioException("Error sending audio packet", ex);
        }
    }

    public Task FlushAsync(CancellationToken cancellationToken) { return Task.CompletedTask; }

    public async ValueTask DisposeAsync()
    {
        if ( _disposed )
        {
            return;
        }

        _disposed = true;

        try
        {
            _udpClient?.Dispose();
            _udpClient = null;
            await Task.CompletedTask; // For async consistency
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing VBAN transport");
        }
    }
}