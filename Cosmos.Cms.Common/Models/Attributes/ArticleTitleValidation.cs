using Cosmos.Cms.Common.Data;
using Cosmos.Cms.Common.Data.Logic;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Cosmos.Cms.Common.Models.Attributes
{
    /// <summary>
    ///     Validates that a title is valid.
    /// </summary>
    /// <remarks>
    ///     <para>This validator checks for the following:</para>
    ///     <list type="bullet">
    ///         <item>That the title is not null or empty space.</item>
    ///         <item>Ensures the title must be unique.</item>
    ///         <item>Prevents titles from being named "root," which is a key word.</item>
    ///     </list>
    ///     <para>Note: This validator will return invalid if it cannot connect to the <see cref="ApplicationDbContext" />.</para>
    /// </remarks>
    public class ArticleTitleValidation : ValidationAttribute
    {
        /// <summary>
        ///     Validates the current value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null) return new ValidationResult("Title cannot be null or empty.");

            if (validationContext == null) return new ValidationResult("ValidationResult cannot be null or empty.");

            var title = value.ToString()?.ToLower().Trim();

            if (string.IsNullOrEmpty(title) || string.IsNullOrWhiteSpace(title))
                return new ValidationResult("Title cannot be an empty string.");

            if (title == "root") return new ValidationResult("Cannot name an article with the name \"root.\"");

            var dbContext = (ApplicationDbContext)validationContext
                .GetService(typeof(ApplicationDbContext));

            if (dbContext == null) throw new Exception("Validator could not connect to ApplicationDbContext.");

            var property = validationContext.ObjectType.GetProperty("ArticleNumber");
            if (property == null) throw new Exception("Validator could not connect to ArticleNumber property.");


            // ReSharper disable once PossibleNullReferenceException
            var articleNumber = (int)property.GetValue(validationContext.ObjectInstance, null);

            var result = dbContext.Articles.Where(a =>
                a.Title.ToLower() == title &&
                a.ArticleNumber != articleNumber &&
                a.StatusCode != (int)StatusCodeEnum.Deleted &&
                a.StatusCode != (int)StatusCodeEnum.Redirect).CountAsync().Result;

            if (result > 0)
                return new ValidationResult("Title is already taken.");


            return ValidationResult.Success;
        }
    }
}