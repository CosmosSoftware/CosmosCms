using System;
using System.Collections.Generic;

namespace Cosmos.Cms.Models
{
    /// <summary>
    /// CKEditor post model
    /// </summary>
    public class HtmlEditorPost
    {
        /// <summary>
        /// Article ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Update existing article (don't create a new version)
        /// </summary>
        public bool? UpdateExisting { get; set; }

        /// <summary>
        /// Regions
        /// </summary>
        public List<HtmlEditorRegion> Regions { get; set; }
    }

    /// <summary>
    /// CK Editor HTML Editable Region
    /// </summary>
    public class HtmlEditorRegion
    {
        /// <summary>
        /// Region ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// HTML Content
        /// </summary>
        public string Html { get; set; }
    }
}
