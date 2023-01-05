namespace Cosmos.Cms.Models
{
    /// <summary>
    ///     JSON model returned have Live editor saves content
    /// </summary>
    public class SaveResultJsonModel : SaveCodeResultJsonModel
    {
        /// <summary>
        ///     Content model as saved
        /// </summary>
        public new HtmlEditorViewModel Model { get; set; }
    }
}