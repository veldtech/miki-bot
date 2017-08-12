using System.Collections.Generic;

namespace Miki.API.Gambling
{
    internal class GamblingApi
    {
    }

    internal class GamblingSettings
    {
        public SlotsSettings slotSettings = new SlotsSettings();
    }

    internal class SlotsSettings
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

    internal class SlotsItem
    {
        public SlotsItem(string emoji, float weight)
        {
            Emoji = emoji;
            Weight = weight;
        }

        public string Emoji { get; set; }
        public float Weight { get; set; }
    }

    internal class SlotsResponse
    {
        private int MoneyBack { get; set; }
        private bool Win { get; set; }
    }
}