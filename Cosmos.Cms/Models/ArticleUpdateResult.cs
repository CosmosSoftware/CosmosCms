using Cosmos.Cms.Common.Models;
using Cosmos.Cms.Data.Logic;
using System.Collections.Generic;

namespace Cosmos.Cms.Models
{
    /// <summary>
    ///     <see cref="ArticleEditLogic.UpdateOrInsert(HtmlEditorViewModel, string, bool)" /> result.
    /// </summary>
    public class ArticleUpdateResult
    {
        /// <summary>
        ///     Updated or Inserted model
        /// </summary>
        public ArticleViewModel Model { get; set; }

        /// <summary>
        ///     Urls that need to be flushed
        /// </summary>
        public List<string> Urls { get; set; } = new List<string>();
    }
}