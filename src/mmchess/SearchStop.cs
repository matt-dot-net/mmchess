using System.Threading;

namespace mmchess;

public sealed class SearchStop
{
    int requested;

    public bool IsRequested => Volatile.Read(ref requested) != 0;

    public void Request()
    {
        Interlocked.Exchange(ref requested, 1);
    }

    public void Reset()
    {
        Interlocked.Exchange(ref requested, 0);
    }
}
