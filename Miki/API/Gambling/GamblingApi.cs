using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miki.API.Gambling
{
    class GamblingApi
    {

        
    }

    class GamblingSettings
    {
        public SlotsSettings slotSettings = new SlotsSettings();
    }

    class SlotsSettings
    {
        private List<SlotsItem> items = new List<SlotsItem>();

        public SlotsSettings AddItem(SlotsItem item)
        {
            items.Add(item);
            return this;
        }
        public SlotsSettings AddItem(string emoji, float weight)
        {
            return AddItem(new SlotsItem(emoji, weight));
        }
    }

    class SlotsItem
    {
        public SlotsItem(string emoji, float weight)
        {
            Emoji = emoji;
            Weight = weight;
        }

        public string Emoji { get; set; }
        public float Weight { get; set; }
    }

    class SlotsResponse
    {
        int MoneyBack { get; set; }
        bool Win { get; set; }
    }
}
