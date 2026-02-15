namespace SessionAndState.Internal;

internal static class SessionKeyLogHelper
{
    public static string Format(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return string.Empty;
        }

        return key.Length > 8 ? key[..8] + "..." : key;
    }
}
