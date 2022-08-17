using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cosmos.Cms.Models
{
    public static class AllowedFileExtensions
    {
        public enum ExtensionCollectionType
        {
            FileUploads = 0,
            ImageUploads = 1,
            BothTypes = 2
        }

        public static string FileUploadsFilter =>
            "*.txt, *.doc, *.docx, *.xls, *.xlsx, *.ppt, *.pptx, *.zip, *.rar, *.pdf";

        public static string ImageUploadsFilter => "*.png, *.gif, *.jpg, *.jpeg, *.svg";

        public static string DeveloperUploadsFilter => "*.js, *.css, *.json, *.ts, *.md";

        public static List<string> GetFilterForBlobs(ExtensionCollectionType type)
        {
            // "*.txt, *.doc, *.docx, *.xls, *.xlsx, *.ppt, *.pptx, *.zip, *.rar, .pdf"
            switch (type)
            {
                case ExtensionCollectionType.FileUploads:
                    return BuildArray(FileUploadsFilter);
                case ExtensionCollectionType.ImageUploads:
                    return BuildArray(ImageUploadsFilter);
                default:
                    return BuildArray($"{FileUploadsFilter},{ImageUploadsFilter},{DeveloperUploadsFilter}");
            }
        }

        public static List<string> GetFilterForBlobs(string extensions)
        {
            // "*.txt, *.doc, *.docx, *.xls, *.xlsx, *.ppt, *.pptx, *.zip, *.rar, .pdf"
            return BuildArray(extensions);
        }


        public static string GetFilterForViews(ExtensionCollectionType type)
        {
            string filter;
            switch (type)
            {
                case ExtensionCollectionType.FileUploads:
                    filter = FileUploadsFilter;
                    break;
                case ExtensionCollectionType.ImageUploads:
                    filter = ImageUploadsFilter;
                    break;
                default:
                    filter = $"{FileUploadsFilter},{ImageUploadsFilter},{DeveloperUploadsFilter}";
                    break;
            }

            return filter.Replace(".", "").Replace("*", "").Replace(" ", "");
        }

        public static bool IsFileValid(string fileName, ExtensionCollectionType type)
        {
            return !string.IsNullOrEmpty(fileName) &&
                   GetFilterForBlobs(type).Contains(Path.GetExtension(fileName).ToLower());
        }

        public static bool IsFileValid(string fileName, string allowedFileTypes)
        {
            return !string.IsNullOrEmpty(fileName) &&
                   GetFilterForBlobs(allowedFileTypes).Contains(Path.GetExtension(fileName).ToLower());
        }

        private static List<string> BuildArray(string filter)
        {
            return filter.Replace("*", "").Replace(" ", "").Split(",").ToList();
        }
    }
}