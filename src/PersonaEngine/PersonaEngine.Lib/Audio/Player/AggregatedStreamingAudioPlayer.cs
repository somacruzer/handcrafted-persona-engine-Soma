using System.Runtime.CompilerServices;
using System.Threading.Channels;

using PersonaEngine.Lib.TTS.Synthesis;

namespace PersonaEngine.Lib.Audio.Player;

public class AggregatedStreamingAudioPlayer : IAggregatedStreamingAudioPlayer
{
    private readonly List<IStreamingAudioPlayer> _players = new();

    private bool _disposed = false;

    /// <summary>
    ///     Creates a new instance of AggregatedStreamingAudioPlayer.
    /// </summary>
    public AggregatedStreamingAudioPlayer() { }

    /// <summary>
    ///     Creates a new instance of AggregatedStreamingAudioPlayer with initial players.
    /// </summary>
    /// <param name="players">The initial collection of players to add.</param>
    public AggregatedStreamingAudioPlayer(IEnumerable<IStreamingAudioPlayer> players)
    {
        ArgumentNullException.ThrowIfNull(players);

        foreach ( var player in players )
        {
            AddPlayer(player);
        }
    }

    /// <summary>
    ///     Gets the collection of streaming audio players in the aggregation.
    /// </summary>
    public IReadOnlyCollection<IStreamingAudioPlayer> Players => _players.AsReadOnly();

    /// <summary>
    ///     Adds a streaming audio player to the aggregation.
    /// </summary>
    /// <param name="player">The player to add.</param>
    public void AddPlayer(IStreamingAudioPlayer player)
    {
        ThrowIfDisposed();

        if ( player == null )
        {
            throw new ArgumentNullException(nameof(player));
        }

        if ( !_players.Contains(player) )
        {
            _players.Add(player);
        }
    }

    /// <summary>
    ///     Removes a streaming audio player from the aggregation.
    /// </summary>
    /// <param name="player">The player to remove.</param>
    /// <returns>True if the player was removed; otherwise, false.</returns>
    public bool RemovePlayer(IStreamingAudioPlayer player)
    {
        ThrowIfDisposed();

        if ( player == null )
        {
            throw new ArgumentNullException(nameof(player));
        }

        return _players.Remove(player);
    }

    public async Task StartPlaybackAsync(IAsyncEnumerable<AudioSegment> audioSegments, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if ( audioSegments == null )
        {
            throw new ArgumentNullException(nameof(audioSegments));
        }

        if ( _players.Count == 0 )
        {
            return; // No players to stream to
        }

        // For a single player, just forward the stream directly
        if ( _players.Count == 1 )
        {
            await _players[0].StartPlaybackAsync(audioSegments, cancellationToken);

            return;
        }

        // For multiple players, use the multicast approach
        await MulticastPlaybackAsync(audioSegments, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if ( _disposed )
        {
            return;
        }

        // Create a copy of the players to avoid modification during enumeration
        var players = _players.ToList();
        _players.Clear();

        // Dispose all players
        foreach ( var player in players )
        {
            await player.DisposeAsync();
        }

        _disposed = true;
    }

    private async Task MulticastPlaybackAsync(
        IAsyncEnumerable<AudioSegment> audioSegments,
        CancellationToken              cancellationToken)
    {
        // Create channels for each player
        var channels = _players.Select(_ => Channel.CreateUnbounded<AudioSegment>()).ToList();

        // Start playback tasks for each player
        var playbackTasks = new List<Task>();
        for ( var i = 0; i < _players.Count; i++ )
        {
            var player  = _players[i];
            var channel = channels[i];

            // Create an async enumerable from this channel
            var playerStream = ChannelToAsyncEnumerable(channel.Reader, cancellationToken);

            // Start playback for this player
            playbackTasks.Add(player.StartPlaybackAsync(playerStream, cancellationToken));
        }

        // Fan-out task - read from the source and write to all channels
        playbackTasks.Add(FanOutAudioSegmentsAsync(audioSegments, channels, cancellationToken));

        // Wait for all tasks to complete
        await Task.WhenAll(playbackTasks);
    }

    private async Task FanOutAudioSegmentsAsync(
        IAsyncEnumerable<AudioSegment> source,
        List<Channel<AudioSegment>>    channels,
        CancellationToken              cancellationToken)
    {
        try
        {
            await foreach ( var segment in source.WithCancellation(cancellationToken) )
            {
                // Write the segment to all channels
                foreach ( var channel in channels )
                {
                    await channel.Writer.WriteAsync(segment, cancellationToken);
                }
            }

            // Complete all channels
            foreach ( var channel in channels )
            {
                channel.Writer.Complete();
            }
        }
        catch (Exception ex)
        {
            // Complete all channels with error
            foreach ( var channel in channels )
            {
                channel.Writer.Complete(ex);
            }

            throw; // Re-throw the exception
        }
    }

    private static async IAsyncEnumerable<AudioSegment> ChannelToAsyncEnumerable(
        ChannelReader<AudioSegment>                reader,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while ( await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false) )
        {
            while ( reader.TryRead(out var item) )
            {
                yield return item;
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if ( _disposed )
        {
            throw new ObjectDisposedException(nameof(AggregatedStreamingAudioPlayer));
        }
    }
}