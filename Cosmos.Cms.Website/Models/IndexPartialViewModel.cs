namespace Cosmos.Cms.Website.Models
{
    /// <summary>
    /// Model used by the index partial view model
    /// </summary>
    public class HomeIndexViewModel
    {
        /// <summary>
        /// ID route value
        /// </summary>
        public string UrlPath { get; internal set; }
        /// <summary>
        /// Language route or query value
        /// </summary>
        public string Lang { get; internal set; }
    }
}
