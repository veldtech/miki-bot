using System;
using System.Collections.Generic;

namespace IA
{
    public delegate void QueryOutput(SqlQueryResponse result);

    public class SqlQueryResponse
    {
        public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();

        public SqlQueryResponse(Dictionary<string, object> objects)
        {
            Values = objects;
        }

        public string GetString(string id) => Values[id].ToString();

        public bool GetBool(string id) => bool.Parse(GetString(id));

        public DateTime GetDate(string id)
        {
            if (!IsValid(id))
            {
                return DateTime.MinValue;
            }

            return DateTime.Parse(GetString(id));
        }

        public int GetInt(string id)
        {
            return int.Parse(GetString(id));
        }

        public long GetLong(string id)
        {
            return long.Parse(GetString(id));
        }

        public float GetFloat(string v)
        {
            return float.Parse(GetString(v));
        }

        public uint GetUint(string id)
        {
            return uint.Parse(GetString(id));
        }

        public ulong GetUlong(string id)
        {
            return ulong.Parse(GetString(id));
        }

        public bool HasValue(string id)
        {
            return Values.ContainsKey(id);
        }

        public bool IsValid(string id)
        {
            return !string.IsNullOrWhiteSpace(GetString(id));
        }
    }
}