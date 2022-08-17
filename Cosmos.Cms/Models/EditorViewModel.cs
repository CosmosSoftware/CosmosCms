namespace Cosmos.Cms.Models
{
    public class EditorViewModel
    {
        public string FieldName { get; set; }
        public string Html { get; set; }
        public bool EditModeOn { get; set; }
    }

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