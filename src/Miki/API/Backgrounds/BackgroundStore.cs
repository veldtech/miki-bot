namespace Miki.API.Backgrounds
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class BackgroundStore
    {
        public IReadOnlyList<Background> Backgrounds { get; }

        public BackgroundStore(IReadOnlyList<Background> backgrounds)
        {
            if(backgrounds == null)
            {
                throw new ArgumentNullException(nameof(backgrounds));
            }
            Backgrounds = backgrounds;
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