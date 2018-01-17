namespace IA.SDK
{
    public enum DiscordChannelPermission : byte
    {
        CreateInstantInvite = 0,
        ManageChannel = 4,
        ReadMessages = 10,
        SendMessages = 11,
        SendTTSMessages = 12,
        ManageMessages = 13,
        EmbedLinks = 14,
        AttachFiles = 15,
        ReadMessageHistory = 16,
        MentionEveryone = 17,
        UseExternalEmojis = 18,
        Connect = 20,
        Speak = 21,
        MuteMembers = 22,
        DeafenMembers = 23,
        MoveMembers = 24,
        UseVAD = 25,
        ManagePermissions = 28
    }
}