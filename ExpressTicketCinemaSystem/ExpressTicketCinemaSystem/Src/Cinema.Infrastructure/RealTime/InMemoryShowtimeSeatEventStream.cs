using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ExpressTicketCinemaSystem.Src.Cinema.Infrastructure.Realtime
{
    public class InMemoryShowtimeSeatEventStream : IShowtimeSeatEventStream
    {
        private class Hub
        {
            public ConcurrentDictionary<int, Channel<SeatEvent>> Channels { get; } =
                new ConcurrentDictionary<int, Channel<SeatEvent>>();
            public int NextId() => System.Threading.Interlocked.Increment(ref _id);
            private int _id = 0;
        }

        private readonly ConcurrentDictionary<int, Hub> _hubs =
            new ConcurrentDictionary<int, Hub>();

        public async Task PublishAsync(SeatEvent ev, CancellationToken ct = default)
        {
            if (!_hubs.TryGetValue(ev.ShowtimeId, out var hub)) return;

            foreach (var kv in hub.Channels.ToArray())
            {
                // best-effort; nếu channel đóng thì loại bỏ
                if (!kv.Value.Writer.TryWrite(ev))
                {
                    hub.Channels.TryRemove(kv.Key, out _);
                }
            }

            await Task.CompletedTask;
        }

        public async IAsyncEnumerable<SeatEvent> SubscribeAsync(
            int showtimeId,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var hub = _hubs.GetOrAdd(showtimeId, _ => new Hub());
            var id = hub.NextId();
            var channel = Channel.CreateUnbounded<SeatEvent>(
                new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

            hub.Channels[id] = channel;

            try
            {
                while (await channel.Reader.WaitToReadAsync(ct).ConfigureAwait(false))
                {
                    while (channel.Reader.TryRead(out var item))
                    {
                        yield return item;
                    }
                }
            }
            finally
            {
                hub.Channels.TryRemove(id, out _);
                channel.Writer.TryComplete();
            }
        }
    }
}
