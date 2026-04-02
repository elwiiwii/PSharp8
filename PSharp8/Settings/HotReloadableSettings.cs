using System.Text.Json;

namespace PSharp8.Settings;

public sealed class HotReloadableSettings<T> : IDisposable where T : class, new()
{
    private const int DebounceMs = 300;
    private const int LoadRetryCount = 3;
    private const int LoadRetryDelayMs = 50;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _filePath;
    private readonly FileSystemWatcher _watcher;

    // UTC ticks of the most recent file-change event; 0 = no pending change.
    // Written from the FSW background thread; read from the game thread.
    private long _pendingChangeTicks;

    public T Current { get; private set; }

    public event Action<T>? Changed;

    public HotReloadableSettings(string filePath, T? defaultValue = null)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));

        if (!File.Exists(filePath))
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var defaults = defaultValue ?? new T();
            File.WriteAllText(filePath, JsonSerializer.Serialize(defaults, JsonOptions));
            Current = defaults;
        }
        else
        {
            Current = Load();
        }

        var watchDir = Path.GetDirectoryName(Path.GetFullPath(filePath))!;
        var watchFile = Path.GetFileName(filePath);
        _watcher = new FileSystemWatcher(watchDir, watchFile)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };
        _watcher.Changed += OnFileChanged;
    }

    public void FlushPending()
    {
        var changeTicks = Interlocked.Read(ref _pendingChangeTicks);
        if (changeTicks == 0)
            return;

        var elapsed = DateTime.UtcNow - new DateTime(changeTicks, DateTimeKind.Utc);
        if (elapsed.TotalMilliseconds < DebounceMs)
            return;

        // CAS: only one FlushPending call wins ownership of this change event.
        if (Interlocked.CompareExchange(ref _pendingChangeTicks, 0L, changeTicks) != changeTicks)
            return;

        var newValue = Load();
        Current = newValue;
        Changed?.Invoke(newValue);
    }

    public void Dispose()
    {
        _watcher.Changed -= OnFileChanged;
        _watcher.Dispose();
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        Interlocked.Exchange(ref _pendingChangeTicks, DateTime.UtcNow.Ticks);
    }

    private T Load()
    {
        for (int attempt = 0; attempt < LoadRetryCount; attempt++)
        {
            try
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? new T();
            }
            catch (IOException) when (attempt < LoadRetryCount - 1)
            {
                Thread.Sleep(LoadRetryDelayMs);
            }
        }

        return Current; // fallback: keep last known good value
    }
}
