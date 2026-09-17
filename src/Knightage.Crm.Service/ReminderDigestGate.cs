namespace Knightage.Crm.Service;

// Singleton -- purely a cost optimization (avoid a DB round-trip on every request),
// NOT the sole correctness guarantee. Real double-send protection is enforced at the
// DB level by Tasks.ReminderSentAtUtc / Meetings.ReminderSentAtUtc IS NULL -- but that
// only protects across separate, non-concurrent runs (tomorrow's run won't re-include
// today's already-marked rows). Within one burst of near-simultaneous requests (a
// single page load commonly fires several parallel API calls, each independently
// invoking TenantResolutionMiddleware), an unreminded row's SELECT can return the same
// results to more than one of them before any has committed its UPDATE -- confirmed
// happening in practice (a Dashboard load landed the digest 6 times in one burst)
// before this was made atomic.
//
// TryClaim replaces the previous separate ShouldRun()+MarkRan() pair specifically to
// close that gap. A plain lock (not a ConcurrentDictionary CAS) is used deliberately --
// ConcurrentDictionary.AddOrUpdate's factory delegates can be invoked speculatively more
// than once under contention (only the dictionary's final stored value is guaranteed
// consistent, not how many times a factory runs), which would let more than one caller's
// closure believe it won the claim. This method runs at most once per HTTP request and
// contention is rare (only within one page load's handful of near-simultaneous calls),
// so a global lock's cost is irrelevant -- correctness here matters far more than
// lock-free throughput. An app-pool recycle wiping this dictionary only costs one extra
// cheap re-check per org, never a duplicate send.
public class ReminderDigestGate
{
    private readonly Dictionary<Guid, DateOnly> _lastRun = new();
    private readonly object _lock = new();

    public bool TryClaim(Guid organizationId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        lock (_lock)
        {
            if (_lastRun.TryGetValue(organizationId, out var last) && last == today)
            {
                return false;
            }
            _lastRun[organizationId] = today;
            return true;
        }
    }
}
