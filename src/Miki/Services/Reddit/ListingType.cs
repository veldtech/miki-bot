namespace Miki.Services.Reddit
{
    public enum ListingType
    {
        /// <summary>
        /// Top posts of the subreddit.
        /// </summary>
        Top,
        /// <summary>
        /// Current highly voted posts of the set subreddit.
        /// </summary>
        Hot,
        /// <summary>
        /// Posts ordered by creation time.
        /// </summary>
        New,
        /// <summary>
        /// Posts that have gained traction in little time.
        /// </summary>
        Rising
    }
}