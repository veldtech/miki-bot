using System;
using System.IO;
using System.Threading.Tasks;
using MiScript.Values;
using MiScript.Values.Literals;
using MiScript.Values.Numbers;
using Newtonsoft.Json;

namespace Miki.Modules.CustomCommands
{
    public class JsonUtils
    {
        public static Task<IScriptValue> ReadJsonAsync(TextReader stream)
        {
            using var reader = new JsonTextReader(stream);

            return ReadJsonAsync(reader);
        }

        public static Task<IScriptValue> ReadJsonAsync(string json)
        {
            using var stream = new StringReader(json);
            using var reader = new JsonTextReader(stream);

            return ReadJsonAsync(reader);
        }

        public static async Task<IScriptValue> ReadJsonAsync(JsonReader reader)
        {
            Parse:
            if (!await reader.ReadAsync())
            {
                return ScriptValue.Null;
            }

            switch (reader.TokenType)
            {
                case JsonToken.None:
                case JsonToken.Null:
                case JsonToken.Undefined:
                case JsonToken.Raw:
                    return ScriptValue.Null;
                case JsonToken.StartObject:
                    return await ReadObject(reader);
                case JsonToken.StartArray:
                    return await ReadList(reader);
                case JsonToken.Integer:
                    switch (reader.Value)
                    {
                        case int i:
                            return ScriptNumber.From(i);
                        case long l:
                            return ScriptNumber.From(l);
                        case ulong l:
                            return ScriptNumber.From(l);
                        default:
                            throw new NotSupportedException();
                    }
                case JsonToken.Float:
                    return ScriptNumber.From((float)reader.Value);
                case JsonToken.String:
                    return ScriptString.From((string)reader.Value);
                case JsonToken.Boolean:
                    return (bool)reader.Value ? ScriptValue.True : ScriptValue.False;
                case JsonToken.EndArray:
                    return ScriptValue.Null;
                case JsonToken.EndObject:
                case JsonToken.EndConstructor:
                case JsonToken.PropertyName:
                    throw new InvalidOperationException();
                case JsonToken.Date:
                case JsonToken.Bytes:
                case JsonToken.StartConstructor:
                    throw new NotSupportedException();
                case JsonToken.Comment:
                    goto Parse;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static async Task<ScriptObject> ReadObject(JsonReader reader)
        {
            var value = new ScriptObject();

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.PropertyName:
                        var propertyName = reader.Value.ToString();

                        value[propertyName] = await ReadJsonAsync(reader);
                        break;
                    case JsonToken.Comment:
                        break;
                    case JsonToken.EndObject:
                        return value;
                }
            }

            throw new InvalidOperationException("Unexpected end when reading object.");
        }

        private static async Task<ScriptArray> ReadList(JsonReader reader)
        {
            var value = new ScriptArray();

            while (true)
            {
                var item = await ReadJsonAsync(reader);

                if (reader.TokenType == JsonToken.EndArray)
                {
                    return value;
                }

                value.Add(item);
            }
        }
    }
}