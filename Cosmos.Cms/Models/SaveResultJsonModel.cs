using Cosmos.Cms.Common.Models;

namespace Cosmos.Cms.Models
{
    /// <summary>
    ///     JSON model returned have HTML editor saves content
    /// </summary>
    public class SaveResultJsonModel : SaveCodeResultJsonModel
    {
        /// <summary>
        ///     Content model as saved
        /// </summary>
        public ArticleViewModel Model { get; set; }
    }
}