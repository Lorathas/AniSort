using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Protobuf.WellKnownTypes;

namespace AniSort.Core.Extensions;

public static class ProtobufExtensions
{
    public static ListValue ToListValue(this object[] parameters)
    {
        var paramList = new ListValue();

        foreach (var param in parameters)
        {
            paramList.Values.Add(param switch {
                string s => Value.ForString(s),
                int i => Value.ForNumber(i),
                long l => Value.ForNumber(l),
                double d => Value.ForNumber(d),
                float f => Value.ForNumber(f),
                bool b => Value.ForBool(b),
                _ => Value.ForStruct(Struct.Parser.ParseJson(JsonSerializer.Serialize(param)))
            });
        }

        return paramList;
    }
}
