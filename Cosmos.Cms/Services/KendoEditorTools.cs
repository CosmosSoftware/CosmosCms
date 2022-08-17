using Kendo.Mvc.UI.Fluent;
using System;

namespace Cosmos.Cms.Services
{
    public static class KendoEditorTools
    {
        public static Action<EditorToolFactory> GetCommonTools()
        {
            return tools => tools
                .Clear()
                .Bold().Italic().Underline().Strikethrough()
                .JustifyLeft().JustifyCenter().JustifyRight().JustifyFull()
                .InsertUnorderedList().InsertOrderedList()
                .Outdent().Indent()
                .CreateLink().Unlink()
                .SubScript()
                .SuperScript()
                .TableEditing()
                .ViewHtml()
                .Formatting()
                .CleanFormatting()
                .FormatPainter()
                .FontName()
                .FontSize()
                .ForeColor().BackColor()
                .Print();
        }
    }
}