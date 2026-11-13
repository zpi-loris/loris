namespace Loris.MQTT;

public static class Utils
{
    public static ReadOnlySpan<char> GetLastTopicSegmentSpan(ReadOnlySpan<char> topic)
    {
        int lastSlashIndex = topic.LastIndexOf('/');
        return lastSlashIndex >= 0 ? topic[(lastSlashIndex + 1)..] : topic;
    }
}
