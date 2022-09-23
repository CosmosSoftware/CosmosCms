namespace Cosmos.Cms.Models
{
    /// <summary>
    /// Gets file type icons based on file extension
    /// </summary>
    public static class FileTypeFontIcon
    {
        public static string Get(string extension)
        {
            switch (extension)
            {
                case "xlsx":
                case "xls":
                    return "fa-solid fa-file-excel";
                case "docx":
                    return "fa-solid fa-file-word";







                default:
                    return "fa-regular fa-file";
            }
        }
    }
}
