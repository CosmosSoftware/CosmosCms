using Cosmos.Cms.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cosmos.Cms.Models
{
    /// <summary>
    /// Edit script post back model
    /// </summary>
    public class EditScriptPostModel : ICodeEditorViewModel
    {
        /// <summary>
        /// Article ID
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Endpoint Name
        /// </summary>
        public string EndPoint { get; set; }

        /// <summary>
        /// Roles that can execute this script (if applicable)
        /// </summary>
        public string RoleList { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Version number
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Input variables
        /// </summary>
        public string InputVars { get; set; }

        /// <summary>
        /// Input configuration
        /// </summary>
        public string Config { get; set; }

        /// <summary>
        /// Node JavaScript code
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Published date and time
        /// </summary>
        public DateTimeOffset? Published { get; set; }

        /// <summary>
        /// Editing field
        /// </summary>
        public string EditingField { get; set; }

        /// <summary>
        /// Editor title
        /// </summary>
        public string EditorTitle { get; set; }

        /// <summary>
        /// Code is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Editor fields
        /// </summary>
        public IEnumerable<EditorField> EditorFields { get; set; }

        /// <summary>
        /// Custom buttons
        /// </summary>
        public IEnumerable<string> CustomButtons { get; set; }

        /// <summary>
        /// Editor type
        /// </summary>
        public string EditorType { get; set; } = nameof(EditScriptPostModel);
    }
}
