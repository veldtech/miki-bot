using Discord;
using IA.SDK.Interfaces;

namespace IA.SDK
{
    internal class RuntimeAttachment : IDiscordAttachment
    {
        private IAttachment attachment;

        public RuntimeAttachment(IAttachment attachment)
        {
            this.attachment = attachment;
        }

        public string FileName => attachment.Filename;

        public string Url => attachment.Url;

        public string ProxyUrl => attachment.ProxyUrl;

        public int? Width => attachment.Width;

        public int? Height => attachment.Height;

        public int Size => attachment.Size;

        public ulong Id => attachment.Id;
    }
}