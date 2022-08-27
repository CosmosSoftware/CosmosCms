﻿using Cosmos.Cms.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cosmos.Cms.Models
{
    /// <summary>
    /// Template code editor view model
    /// </summary>
    public class TemplateCodeEditorViewModel : ICodeEditorViewModel
    {
        /// <summary>
        /// Content
        /// </summary>
        [DataType(DataType.Html)] 
        public string Content { get; set; }
        /// <summary>
        /// Version
        /// </summary>
        public int Version { get; set; }
        /// <summary>
        /// Unique ID of template
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// Editing field name
        /// </summary>
        public string EditingField { get; set; }
        /// <summary>
        /// Editor title
        /// </summary>
        public string EditorTitle { get; set; }
        /// <summary>
        /// List of editor fields
        /// </summary>
        public IEnumerable<EditorField> EditorFields { get; set; }
        /// <summary>
        /// Custom buttons used by editor
        /// </summary>
        public IEnumerable<string> CustomButtons { get; set; }
        /// <summary>
        /// Content is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Editor type
        /// </summary>
        public string EditorType { get; set; } = nameof(TemplateCodeEditorViewModel);
    }
}