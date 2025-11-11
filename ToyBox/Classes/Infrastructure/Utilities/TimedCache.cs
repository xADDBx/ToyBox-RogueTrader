namespace ToyBox.Infrastructure.Utilities;

public class TimedCache<T> {
    private readonly Func<T> m_GetValueFunc;
    private readonly TimeSpan m_InvalidateDelay;
    private readonly bool m_QueueOnMainThread;
    private DateTime m_LastCacheUpdate = DateTime.MinValue;
    public TimedCache(Func<T> getterFunc, int invalidateAfterSeconds = 1, bool queueOnUpdateThread = false) {
        m_GetValueFunc = getterFunc;
        m_InvalidateDelay = TimeSpan.FromSeconds(invalidateAfterSeconds);
        m_QueueOnMainThread = queueOnUpdateThread;
    }
    private void RefreshCacheIfNeeded() {
        var now = DateTime.UtcNow;
        if ((now - m_LastCacheUpdate) > m_InvalidateDelay) {
            if (m_QueueOnMainThread) {
                Main.ScheduleForMainThread(() => {
                    Value = m_GetValueFunc();
                    m_LastCacheUpdate = now;
                });
            } else {
                Value = m_GetValueFunc();
                m_LastCacheUpdate = now;
            }
        }
    }
    public void ForceRefresh() {
        m_LastCacheUpdate = DateTime.MinValue;
    }
    public T Value {
        get {
            RefreshCacheIfNeeded();
            return field;
        }
        private set;
    } = default!;
    public static implicit operator T(TimedCache<T> cache) {
        return cache.Value;
    }
}
