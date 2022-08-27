using System;
using System.ComponentModel.DataAnnotations;

namespace Cosmos.Cms.Common.Data
{
    /// <summary>
    /// Script Catalog Entry
    /// </summary>
    public class ScriptCatalogEntry
    {
        /// <summary>
        /// Catalog Id
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Endpoint Name
        /// </summary>
        public string EndPoint { get; set; }

        /// <summary>
        /// Published date and time
        /// </summary>
        public DateTimeOffset? Published { get; set; }

        /// <summary>
        /// Date and time updated
        /// </summary>
        public DateTimeOffset Updated { get; set; }


    }
}
