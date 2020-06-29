using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Miki.Cache;
using MiScript;
using MiScript.Exceptions;
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

        public ScriptStorage(IExtendedCacheClient cache, long guildId, int keyLimit, int valueLimit)
        {
            this.cache = cache;
            this.guildId = guildId;
            this.keyLimit = keyLimit;
            this.valueLimit = valueLimit;
        }

        public override async Task<IScriptValue> GetAsync(Context context, IScriptValue key, CancellationToken token = new CancellationToken())
        {
            var value = await base.GetAsync(context, key, token);
            if (!value.IsNull)
            {
                return value;
            }
            
            var json = await cache.HashGetAsync<string>(CacheKey + ":" + guildId, key.ToString());
            if (string.IsNullOrEmpty(json))
            {
                return value;
            }
            
            value = await JsonUtils.ReadJsonAsync(json);
            SetRaw(key, value);

            return value;
        }

        public override async Task SetAsync(Context context, IScriptValue key, IScriptValue value, CancellationToken token = new CancellationToken())
        {
            await base.SetAsync(context, key, value, token);
            
            var cacheKey = CacheKey + ":" + guildId;
            var keyStr = key.ToString();

            if (value.IsNull)
            {
                await cache.HashDeleteAsync(cacheKey, keyStr);
                return;
            }
            
            var keyCount = await cache.HashLengthAsync(cacheKey);

            if (keyCount >= keyLimit && !await cache.HashExistsAsync(cacheKey, keyStr))
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
                throw new MiScriptException($"You can store up {valueLimit} bytes storage");
            }
            
            await cache.HashUpsertAsync(cacheKey, keyStr, json);
        }
    }
}