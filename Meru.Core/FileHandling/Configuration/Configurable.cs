using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IA.FileHandling.Configuration
{
    public class Configurable<T>
    {
        public T Value { get; internal set; }

        public void TryLoad(string input)
        {
            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter != null)
                {
                    Value = (T)converter.ConvertFromString(input);
                }
                Value = default(T);
            }
            catch (NotSupportedException)
            {
                Log.Warning("Configurable {0} was not loaded correctly.");
                Value = default(T);
            }
        }
    }
}
