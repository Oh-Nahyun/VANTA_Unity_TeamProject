using UnityEngine;

public static class RetryStatus
{
    public static bool IsRetry { get; private set; }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        IsRetry = false;
    }

    public static void SetRetry()
    {
        IsRetry = true;
    }

    public static void Reset()
    {
        IsRetry = false;
    }
}
