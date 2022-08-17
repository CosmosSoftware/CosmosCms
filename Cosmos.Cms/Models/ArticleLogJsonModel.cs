using System;
using System.ComponentModel.DataAnnotations;

namespace Cosmos.Cms.Models
{
    public class ArticleLogJsonModel
    {
        [Key] public Guid Id { get; set; }

        public string ActivityNotes { get; set; }

        /// <summary>
        ///     Date and Time (UTC by default)
        /// </summary>
        public DateTimeOffset DateTimeStamp { get; set; }
        /// <summary>
        /// Identity User Id
        /// </summary>
        public string IdentityUserId { get; set; }
    }
}