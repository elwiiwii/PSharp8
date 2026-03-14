using FluentAssertions;
using PSharp8.Graphics;
using Xunit;

namespace PSharp8.Tests.Graphics;

public class LruCacheTests
{
    private sealed class Sentinel : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    [Fact]
    public void Get_ReturnsNull_ForMissingKey()
    {
        var cache = new LruCache<string, Sentinel>(staleTtlFrames: 10);

        cache.Get("missing").Should().BeNull();
    }

    [Fact]
    public void Get_ReturnsValue_ForInsertedKey()
    {
        var cache = new LruCache<string, Sentinel>(staleTtlFrames: 10);
        var sentinel = new Sentinel();
        cache.Put("a", sentinel);

        cache.Get("a").Should().BeSameAs(sentinel);
    }

    [Fact]
    public void Put_DisposesOldValue_WhenReplacingExistingKey()
    {
        var cache = new LruCache<string, Sentinel>(staleTtlFrames: 10);
        var old = new Sentinel();
        var replacement = new Sentinel();
        cache.Put("a", old);
        cache.Put("a", replacement);

        old.IsDisposed.Should().BeTrue();
        cache.Get("a").Should().BeSameAs(replacement);
    }

    [Fact]
    public void Tick_DoesNotEvict_EntriesWithinTtl()
    {
        var cache = new LruCache<string, Sentinel>(staleTtlFrames: 2);
        var sentinel = new Sentinel();
        cache.Put("a", sentinel);
        cache.Tick(); // frame 1: (1-0)=1 not > 2
        cache.Tick(); // frame 2: (2-0)=2 not > 2

        sentinel.IsDisposed.Should().BeFalse();
        cache.Count.Should().Be(1);
    }

    [Fact]
    public void Tick_EvictsAndDisposes_EntriesPastTtl()
    {
        var cache = new LruCache<string, Sentinel>(staleTtlFrames: 2);
        var sentinel = new Sentinel();
        cache.Put("a", sentinel);
        cache.Tick(); // frame 1
        cache.Tick(); // frame 2
        cache.Tick(); // frame 3: (3-0)=3 > 2 → evict

        sentinel.IsDisposed.Should().BeTrue();
        cache.Count.Should().Be(0);
    }

    [Fact]
    public void Tick_DoesNotEvict_EntryReaccessedBeforeTtlExpires()
    {
        var cache = new LruCache<string, Sentinel>(staleTtlFrames: 2);
        var sentinel = new Sentinel();
        cache.Put("a", sentinel);
        cache.Tick(); // frame 1
        cache.Tick(); // frame 2
        cache.Get("a"); // resets lastAccessed to frame 2
        cache.Tick(); // frame 3: (3-2)=1 not > 2 → alive

        sentinel.IsDisposed.Should().BeFalse();
        cache.Count.Should().Be(1);
    }

    [Fact]
    public void Clear_DisposesAllEntries()
    {
        var cache = new LruCache<string, Sentinel>(staleTtlFrames: 10);
        var s1 = new Sentinel();
        var s2 = new Sentinel();
        var s3 = new Sentinel();
        cache.Put("a", s1);
        cache.Put("b", s2);
        cache.Put("c", s3);
        cache.Clear();

        s1.IsDisposed.Should().BeTrue();
        s2.IsDisposed.Should().BeTrue();
        s3.IsDisposed.Should().BeTrue();
        cache.Count.Should().Be(0);
    }

    [Fact]
    public void Count_ReflectsLiveEntries()
    {
        var cache = new LruCache<string, Sentinel>(staleTtlFrames: 2);
        cache.Count.Should().Be(0);
        cache.Put("a", new Sentinel());
        cache.Put("b", new Sentinel());
        cache.Count.Should().Be(2);
        cache.Tick();
        cache.Tick();
        cache.Tick(); // evicts both

        cache.Count.Should().Be(0);
    }
}
