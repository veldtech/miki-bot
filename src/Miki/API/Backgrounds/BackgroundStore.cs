#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Miki.Exceptions;

namespace Miki.API.Backgrounds
{
    public interface IBackgroundStore
    {
        ValueTask<Background> GetBackgroundAsync(int id);
    }
    
    public class BackgroundStore : IBackgroundStore
    {
        private IReadOnlyList<Background> Backgrounds { get; }

        public BackgroundStore(IReadOnlyList<Background> backgrounds)
        {
            Backgrounds = backgrounds ?? throw new ArgumentNullException(nameof(backgrounds));
        }

        public ValueTask<Background> GetBackgroundAsync(int id)
        {
            if (id >= Backgrounds.Count || id < 0)
            {
                throw new EntityNotFoundException<Background>();
            }
            return new ValueTask<Background>(Backgrounds[id]);
        }
        
        public static async ValueTask<BackgroundStore> LoadFromFileAsync(string path)
        {
            if(!File.Exists(path))
            {
                throw new FileNotFoundException("Couldn't find file", path);
            }

            await using var stream = new MemoryStream(await File.ReadAllBytesAsync(path));
            var backgrounds = await JsonSerializer.DeserializeAsync<List<Background>>(stream);
            return new BackgroundStore(backgrounds);
        }
    }
}