namespace Miki.API.Backgrounds
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public class BackgroundStore
    {
        public IReadOnlyList<Background> Backgrounds { get; }

        public BackgroundStore(IReadOnlyList<Background> backgrounds)
        {
            Backgrounds = backgrounds;
        }

        public static async ValueTask<BackgroundStore> LoadFromFileAsync(string path)
        {
            if(!File.Exists(path))
            {
                throw new FileNotFoundException("Couldn't find file", path);
            }
            string json = await File.ReadAllTextAsync(path);

            var backgrounds = JsonConvert.DeserializeObject<List<Background>>(json);
            if(backgrounds == null || !backgrounds.Any())
            {
                throw new IndexOutOfRangeException("Resource did not contain any elements");
            }
            return new BackgroundStore(backgrounds);
        }
    }
}