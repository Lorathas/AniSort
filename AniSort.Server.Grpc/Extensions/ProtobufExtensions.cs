using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace AniSort.Server.Extensions;

public static class ProtobufExtensions
{
    public static RepeatedField<T> ToRepeatedField<T>(this IEnumerable<T> enumerable)
    {
        var repeatedField = new RepeatedField<T>();
        repeatedField.AddRange(enumerable);
        return repeatedField;
    }
}