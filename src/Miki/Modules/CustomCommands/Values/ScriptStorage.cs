using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Miki.Cache;
using MiScript;
using MiScript.Exceptions;
using MiScript.Utils;
using MiScript.Values;
using Newtonsoft.Json;

namespace Miki.Modules.CustomCommands.Values
{
    public class ScriptStorage : ScriptObject
    {
        private const string CacheKey = "customcommandsstorage";

        private readonly int keyLimit;
        private readonly int valueLimit;
        private readonly long guildId;
        private readonly IExtendedCacheClient cache;
        private readonly IList<IScriptValue> updatedKeys = new List<IScriptValue>();

        public ScriptStorage(IExtendedCacheClient cache, long guildId, int keyLimit, int valueLimit)
        {
            this.cache = cache;
            this.guildId = guildId;
            this.keyLimit = keyLimit;
            this.valueLimit = valueLimit;
        }

        /// <inheritdoc />
        public override async Task<IScriptValue> GetAsync(Context context, IScriptValue key, CancellationToken token = new CancellationToken())
        {
            var value = await base.GetAsync(context, key, token);
            if (!value.IsNull)
            {
                return value;
            }
            
            var cacheKey = GetCacheKey(guildId);
            var json = await cache.HashGetAsync<string>(cacheKey, key.ToString());
            if (string.IsNullOrEmpty(json))
            {
                return value;
            }
            
            value = await JsonUtils.ReadJsonAsync(json);
            SetRaw(key, value);

            return value;
        }

        /// <inheritdoc />
        public override async Task SetAsync(Context context, IScriptValue key, IScriptValue value, CancellationToken token = new CancellationToken())
        {
            await base.SetAsync(context, key, value, token);

            if (!updatedKeys.Contains(key))
            {
                updatedKeys.Add(key);
            }
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
            
            foreach (var key in updatedKeys)
            {
                var value = GetRaw(key);
                var keyStr = key.ToString();
            
                if (value.IsNull)
                {
                    if (cacheKeys.Contains(keyStr))
                    {
                        await cache.HashDeleteAsync(cacheKey, keyStr);
                    }
                    
                    continue;
                }
            
                if (cacheKeys.Count >= keyLimit && !cacheKeys.Contains(keyStr))
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
            
                await cache.HashUpsertAsync(cacheKey, keyStr, json);
            }
        }

        public static string GetCacheKey(long guildId)
        {
            return CacheKey + ":" + guildId;
        }
    }
}