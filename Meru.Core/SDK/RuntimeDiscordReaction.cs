using Discord;

namespace IA.SDK
{
    internal class RuntimeDiscordReaction
    {
        private IReaction sourceReaction;

        public RuntimeDiscordReaction(IReaction reaction)
        {
            sourceReaction = reaction;
        }
    }
}