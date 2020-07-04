using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Miki.Cache;
using MiScript;
using MiScript.Attributes;
using MiScript.Exceptions;
using MiScript.Utils;
using MiScript.Values;
using MiScript.Values.Literals;
using Newtonsoft.Json;

namespace Miki.Modules.CustomCommands.Values
{
    public class ScriptStorage : ScriptValue
    {
        private const string CacheKey = "customcommandsstorage";

        private readonly ConcurrentDictionary<string, IScriptValue> values;
        private readonly int keyLimit;
        private readonly int valueLimit;
        private readonly long guildId;
        private readonly IExtendedCacheClient cache;
        private readonly IList<string> updatedKeys;
        private IList<string> keys;

        public ScriptStorage(IExtendedCacheClient cache, long guildId, int keyLimit, int valueLimit)
        {
            this.cache = cache;
            this.guildId = guildId;
            this.keyLimit = keyLimit;
            this.valueLimit = valueLimit;
            values = new ConcurrentDictionary<string, IScriptValue>();
            updatedKeys = new List<string>();
        }

        public override ScriptValueType Type => ScriptValueType.Object;

        public override bool KeepReference => false;

        /// <inheritdoc />
        public override IScriptValue GetPrototype(Context context)
        {
            return context.Global.GetTypePrototype(typeof(ScriptStorage));
        }

        [Function("get_keys")]
        public async Task<IScriptValue> GetKeysAsync()
        {
            keys ??= (await cache.HashKeysAsync(GetCacheKey(guildId))).ToList();
            return new ScriptArray(keys.Select(ScriptString.From));
        }

        [Function("clear")]
        public async Task ClearAsync()
        {
            keys ??= (await cache.HashKeysAsync(GetCacheKey(guildId))).ToList();

            foreach (var key in keys.Where(k => !updatedKeys.Contains(k)))
            {
                updatedKeys.Add(key);
            }
            
            values.Clear();
            keys.Clear();
        }

        [Function("get")]
        public async Task<IScriptValue> GetAsync(string key)
        {
            if (values.TryGetValue(key, out var value))
            {
                return value;
            }
            
            var cacheKey = GetCacheKey(guildId);
            var json = await cache.HashGetAsync<string>(cacheKey, key);

            if (string.IsNullOrEmpty(json))
            {
                return Null;
            }

            try
            {
                value = await JsonUtils.ReadJsonAsync(json);
            }
            catch
            {
                value = Null;
            }
            
            values.AddOrUpdate(key, value, (a1, a2) => value);
            return value;
        }

        [Function("set")]
        public void Set(string key, IScriptValue value)
        {
            if (keys != null)
            {
                var keyStr = key;
                
                if (value.IsNull && keys.Contains(keyStr))
                {
                    keys.Remove(keyStr);
                }
                else if (!value.IsNull && !keys.Contains(keyStr))
                {
                    keys.Add(keyStr);
                }
            }

            if (!updatedKeys.Contains(key))
            {
                updatedKeys.Add(key);
            }
            
            values.AddOrUpdate(key, value, (a1, a2) => value);
        }

        [Function("del")]
        public void Delete(string key)
        {
            Set(key, Null);
        }

        /// <summary>
        /// Update the values in Redis.
        /// </summary>
        public async ValueTask UpdateAsync(Context context, CancellationToken token = default)
        {
            if (updatedKeys.Count == 0)
            {
                return;
            }
            
            var cacheKey = GetCacheKey(guildId);
            var cacheKeys = (await cache.HashKeysAsync(cacheKey)).ToList();
            var deletedKeys = updatedKeys
                .Where(k => !values.TryGetValue(k, out var value) || value.IsNull)
                .ToList();

            foreach (var key in deletedKeys.Where(cacheKeys.Contains))
            {
                await cache.HashDeleteAsync(cacheKey, key);
            }

            foreach (var key in updatedKeys)
            {
                if (deletedKeys.Contains(key))
                {
                    continue;
                }
                
                var value = values[key];
            
                if (cacheKeys.Count >= keyLimit && !cacheKeys.Contains(key))
                {
                    throw new MiScriptException($"You can store up to {keyLimit} keys in the storage");
                }

                await using var stream = new MemoryStream();
                var writer = new StreamWriter(stream);
                var jsonWriter = new JsonTextWriter(writer);

                await value.WriteJsonAsync(context, jsonWriter);
                await jsonWriter.FlushAsync(token);
            
                var json = Encoding.UTF8.GetString(stream.ToArray());

                if (json.Length > valueLimit)
                {
                    throw new MiScriptException($"The value limit is {valueLimit} bytes, tried to store {json.Length} bytes.");
                }
            
                await cache.HashUpsertAsync(cacheKey, key, json);
            }
        }

        public override object ToObject(Context context)
        {
            return this;
        }

        public override bool Equals(IScriptValue other)
        {
            return other is ScriptStorage;
        }

        public override async Task WriteJsonAsync(Context context, JsonWriter writer)
        {
            await writer.WriteStartObjectAsync();
            
            foreach (var (key, value) in await cache.HashGetAllAsync<string>(GetCacheKey(guildId)))
            {
                await writer.WritePropertyNameAsync(key);
                await writer.WriteRawAsync(value);
            }
            
            await writer.WriteEndObjectAsync();
        }

        public static string GetCacheKey(long guildId)
        {
            return CacheKey + ":" + guildId;
        }

        public override async Task<IScriptValue> GetAsync(Context context, IScriptValue key, CancellationToken token = new CancellationToken())
        {
            var value = await base.GetAsync(context, key, token);

            if (value.IsNull)
            {
                throw new MiScriptException("Cannot get from storage through indexes, use storage.get instead");
            }
            
            return value;
        }

        public override void SetRaw(IScriptValue key, IScriptValue value)
        {
            throw new MiScriptException("Cannot set in storage through indexes, use storage.set instead");
        }
    }
}