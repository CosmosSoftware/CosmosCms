namespace Cosmos.Cms.Models
{
    /// <summary>
    /// Kendo editor tool set configuration.
    /// </summary>
    public enum ToolSetEnum
    {
        /// <summary>
        /// All tools
        /// </summary>
        Full = 0,
        /// <summary>
        /// Tools for a title on a page
        /// </summary>
        Title = 1,
        /// <summary>
        /// Minimum set of tools
        /// </summary>
        Limited = 2,
        /// <summary>
        /// Tools for content in a card
        /// </summary>
        Card = 3,
        /// <summary>
        /// Tools for a map
        /// </summary>
        Map
    }
}