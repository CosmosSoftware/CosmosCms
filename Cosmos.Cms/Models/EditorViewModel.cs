namespace Cosmos.Cms.Models
{
    /// <summary>
    /// Editor view model
    /// </summary>
    public class EditorViewModel
    {
        public string FieldName { get; set; }
        public string Html { get; set; }
        public bool EditModeOn { get; set; }
    }

    /// <summary>
    /// Editor view model builder
    /// </summary>
    public static class EditorViewModelBuilder
    {
        public static EditorViewModel Build(string fieldName, bool editModeOn, string html)
        {
            return new()
            {
                FieldName = fieldName,
                Html = html,
                EditModeOn = editModeOn
            };
        }
    }
}