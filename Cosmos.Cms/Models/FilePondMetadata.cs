using Newtonsoft.Json;

namespace Cosmos.Cms.Models
{
    /// <summary>
    /// Filepond upload metadata
    /// </summary>
    public class FilePondMetadata
    {
        /// <summary>
        /// Upload path or folder
        /// </summary>
        [JsonProperty("path")]
        public string Path { get; set; }
    }
}
