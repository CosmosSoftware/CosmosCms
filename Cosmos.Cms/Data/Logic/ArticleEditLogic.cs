using Cosmos.Cms.Common.Data;
using Cosmos.Cms.Common.Data.Logic;
using Cosmos.Cms.Common.Models;
using Cosmos.Cms.Common.Services.Configurations;
using Cosmos.Cms.Controllers;
using Cosmos.Cms.Models;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NUglify.Html;
using SendGrid.Helpers.Errors.Model;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Cosmos.Cms.Data.Logic
{
    /// <summary>
    ///     Article Editor Logic
    /// </summary>
    /// <remarks>
    ///     Is derived from base class <see cref="ArticleLogic" />, adds on content editing functionality.
    /// </remarks>
    public class ArticleEditLogic : ArticleLogic
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="memoryCache"></param>
        /// <param name="config"></param>
        public ArticleEditLogic(ApplicationDbContext dbContext,
            IMemoryCache memoryCache,
            IOptions<CosmosConfig> config) :
            base(dbContext,
                config, memoryCache, true)
        {
        }

        /// <summary>
        ///     Database Context with Synchronize Context
        /// </summary>
        public new ApplicationDbContext DbContext => base.DbContext;

        /// <summary>
        ///     Determine if this service is configured
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsConfigured()
        {
            return await DbContext.IsConfigured();
        }

        #region VALLIDATION

        /// <summary>
        ///     Validate that the title is not already taken by another article.
        /// </summary>
        /// <param name="title">Title</param>
        /// <param name="articleNumber">Current article number</param>
        /// <returns></returns>
        /// <remarks>
        /// If article number is given, this checks  all other article
        /// numbers to see if this title is already taken.
        /// If not given, this method returns true if article name already in use.
        /// </remarks>
        public async Task<bool> ValidateTitle(string title, int? articleNumber)
        {

            //
            // Make sure it doesn't conflict with the publi blob path
            //
            var reservedPaths = new string[] { "pub", "api", "GetSupportedLanguages", "GetTOC", "AccessPending", "GetMicrosoftIdentityAssociation", "Error" };

            foreach (var reservedPath in reservedPaths)
            {
                if (title.ToLower() == reservedPath.ToLower())
                {
                    return false;
                }
            }

            Article article;
            if (articleNumber.HasValue)
            {
                article = await DbContext.Articles.FirstOrDefaultAsync(a =>
                    a.ArticleNumber != articleNumber && // look only at other article numbers
                    a.Title.ToLower() == title.Trim().ToLower() && // Is the title used already
                    a.StatusCode != (int)StatusCodeEnum.Deleted); // and the page is active (active or is inactive)
            }
            else
            {
                article = await DbContext.Articles.FirstOrDefaultAsync(a =>
                    a.Title.ToLower() == title.Trim().ToLower() && // Is the title used already
                    a.StatusCode != (int)StatusCodeEnum.Deleted); // and the page is active (active or is inactive)
            }

            if (article == null) return true;

            return false;
        }

        #endregion

        #region PRIVATE METHODS

        /// <summary>
        ///     Gets a template represented as an <see cref="ArticleViewModel" />.
        /// </summary>
        /// <param name="template"></param>
        /// <returns>ArticleViewModel</returns>
        private ArticleViewModel BuildTemplateViewModel(Template template)
        {
            var articleNumber = DbContext.Articles.Max(m => m.ArticleNumber) + 1;

            return new()
            {
                Id = template.Id,
                ArticleNumber = articleNumber,
                UrlPath = HttpUtility.UrlEncode(template.Title.Trim().Replace(" ", "_")),
                VersionNumber = 1,
                Published = DateTime.Now.ToUniversalTime(),
                Title = template.Title,
                Content = template.Content,
                Updated = DateTime.Now.ToUniversalTime(),
                HeadJavaScript = string.Empty,
                FooterJavaScript = string.Empty,
                ReadWriteMode = true
            };
        }

        private async Task HandleLogEntry(Article article, string note, string userId)
        {
            DbContext.ArticleLogs.Add(new ArticleLog
            {
                ArticleId = article.Id,
                IdentityUserId = userId,
                ActivityNotes = note,
                DateTimeStamp = DateTime.Now.ToUniversalTime()
            });

            await DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Make sure all content editble DIVs have a unique C/CMS ID (attribute 'data-ccms-ceid').
        /// </summary>
        /// <param name="content"></param>
        /// <returns>string</returns>
        /// <remarks>
        /// <para>
        /// The WYSIWYG editor is designed to only edit portions of an article content that are marked 
        /// with the attribute "contenteditable='true'".
        /// </para>
        /// <para>
        /// When an article is saved by the WYSIWYG editor only those portions within the DIV tags
        /// marked editable are saved.
        /// </para>
        /// <para>
        /// This allows editing of a web page with dynamic client-side functionality (JavaScript)
        /// like a map, chart, graph, etc. to be uneditable on a page while the text around it is.
        /// </para>
        /// </remarks>
        public string Ensure_ContentEditable_IsMarked(string content)
        {
            if (string.IsNullOrEmpty(content) || string.IsNullOrWhiteSpace(content))
            {
                return content;
            }
            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(content);

            var elements = htmlDoc.DocumentNode.SelectNodes("//*[@contenteditable]|//*[@crx]|//*[@data-ccms-ceid]");

            if (elements == null)
            {
                return content;
            }

            var count = 0;
            var editables = new string[] { "div", "h1", "h2", "h3", "h4", "h5" };

            foreach (var element in elements)
            {
                if (editables.Contains(element.Name.ToLower()))
                {

                    // Remove all editable childred as this will mess up CKEditor creation
                    var children = element.Descendants().Where(w => w.Attributes.Contains("data-ccms-ceid") || w.Attributes.Contains("contenteditable"));
                    foreach (var c in children)
                    {
                        if (c.Attributes.Contains("data-ccms-ceid"))
                        {
                            c.Attributes.Remove("data-ccms-ceid");
                        }

                        if (c.Attributes.Contains("contenteditable"))
                        {
                            c.Attributes.Remove("contenteditable");
                        }
                    }

                    if (!element.Attributes.Contains("data-ccms-ceid"))
                    {
                        element.Attributes.Add("data-ccms-ceid", Guid.NewGuid().ToString());
                        count++;
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(element.Attributes["data-ccms-ceid"].Value))
                        {
                            element.Attributes["data-ccms-ceid"].Value = Guid.NewGuid().ToString();
                        }
                    }

                    if (element.Attributes.Contains("contenteditable"))
                    {
                        element.Attributes.Remove("contenteditable");
                    }
                    if (element.Attributes.Contains("crx"))
                    {
                        element.Attributes.Remove("crx");
                    }
                }
                else
                {
                    if (element.Attributes.Contains("contenteditable"))
                    {
                        element.Attributes["contenteditable"].Value = "false";
                    }
                }

            }

            // If we had to add at least one ID, then re-save the article.
            return htmlDoc.DocumentNode.OuterHtml;
        }

        /// <summary>
        /// If an OEMBED element is present, ensures the necessary JavaScript is injected.
        /// </summary>
        /// <param name="model"></param>
        private void Ensure_Oembed_Handled(ArticleViewModel model)
        {

            var htmlDoc = new HtmlAgilityPack.HtmlDocument();
            var footerDoc = new HtmlAgilityPack.HtmlDocument();

            htmlDoc.LoadHtml(string.IsNullOrEmpty(model.Content) ? "" : model.Content);
            footerDoc.LoadHtml(string.IsNullOrEmpty(model.FooterJavaScript) ? "" : model.FooterJavaScript);

            var oembed = htmlDoc.DocumentNode.SelectNodes("//oembed[@url]");
            var hasOembed = oembed != null && oembed.Any();

            var embedlyElements = footerDoc.DocumentNode.SelectNodes("//script[@id='cwps_embedly']");
            var scriptElements = footerDoc.DocumentNode.SelectNodes("//script[@id='cwps_embedly_launch']");

            //
            // Now add or remove supporting JavaScript as  needed
            //
            if (hasOembed)
            {
                //
                // There are OEmbeds, so add supporting JavaScript injects below
                //
                if (embedlyElements == null || !embedlyElements.Any())
                {
                    var embedly = footerDoc.CreateElement("script");
                    embedly.Id = "cwps_embedly";
                    embedly.Attributes.Append("async");
                    embedly.Attributes.Append("charset", "utf-8");
                    embedly.Attributes.Append("src", "//cdn.embedly.com/widgets/platform.js");

                    footerDoc.DocumentNode.AppendChild(embedly);
                }

                if (scriptElements == null || !scriptElements.Any())
                {
                    var addon = footerDoc.CreateElement("script");
                    addon.Id = "cwps_embedly_launch";
                    addon.InnerHtml = "document.querySelectorAll( 'oembed[url]' ).forEach( element => { const anchor = document.createElement( 'a' ); anchor.setAttribute( 'href', element.getAttribute( 'url' ) ); anchor.className = 'embedly-card'; element.appendChild( anchor ); });";

                    footerDoc.DocumentNode.AppendChild(addon);
                }
                model.FooterJavaScript = footerDoc.DocumentNode.OuterHtml;
            }
            else
            {
                //
                // There are NO OEmbeds, so REMOVE supporting JavaScript injects below
                //
                if (embedlyElements != null && embedlyElements.Any())
                {
                    foreach (var el in embedlyElements)
                    {
                        el.Remove();
                    }
                }
                if (scriptElements != null && scriptElements.Any())
                {
                    foreach (var el in scriptElements)
                    {
                        el.Remove();
                    }
                }
                model.FooterJavaScript = footerDoc.DocumentNode.OuterHtml;
            }

        }

        /// <summary>
        /// Resets the expiration dates, based on the last published article, saves changes to the database
        /// </summary>
        /// <param name="articleNumber"></param>
        /// <returns></returns>
        private async Task ResetVersionExpirations(int articleNumber)
        {
            var list = await DbContext.Articles.Where(a => a.ArticleNumber == articleNumber).ToListAsync();

            foreach (var item in list)
                if (item.Expires.HasValue)
                    item.Expires = null;

            var published = list.Where(a => a.ArticleNumber == articleNumber && a.Published.HasValue)
                .OrderBy(o => o.VersionNumber).TakeLast(2).ToList();

            if (published.Count == 2) published[0].Expires = published[1].Published;

            await DbContext.SaveChangesAsync();
        }

        #endregion

        #region CREATE METHODS

        /// <summary>
        ///     Creates a new article, save it to the database before returning a copy for editing.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="userId"></param>
        /// <param name="templateId"></param>
        /// <returns>Unsaved article ready to edit and save</returns>
        /// <remarks>
        ///     <para>
        ///         Creates a new article, saves it to the database, and is ready to edit.  Uses <see cref="ArticleLogic.GetDefaultLayout" /> to get the
        ///         layout,
        ///         and builds the <see cref="ArticleViewModel" /> using method
        ///         <seealso cref="ArticleLogic.BuildArticleViewModel(Article, string)" />. Creates a new article number.
        ///     </para>
        ///     <para>
        ///         If a template ID is given, the contents of this article is loaded with content from the <see cref="Template" />.
        ///     </para>
        ///     <para>
        ///         If this is the first article, it is saved as root and published immediately.
        ///     </para>
        /// </remarks>
        public async Task<ArticleViewModel> Create(string title, string userId, Guid? templateId = null)
        {

            // Is this the first article? If so, make it the root and publish it.
            var isFirstArticle = (await DbContext.Articles.CosmosAnyAsync()) == false;

            var isValidTitle = await ValidateTitle(title, null);

            if (!isValidTitle)
            {
                throw new Exception($"Title '{title}' conflicts with another article or reserved word.");
            }

            var defaultTemplate = string.Empty;

            if (templateId.HasValue)
            {
                var template = await DbContext.Templates.FindAsync(templateId.Value);

                defaultTemplate = template?.Content;
            }

            if (string.IsNullOrEmpty(defaultTemplate))
                defaultTemplate = "<div class=\"container m-y-lg\">" +
                                  "<main class=\"main-primary\">" +
                                  "<div class=\"row\">" +
                                  "<div class=\"col-md-12\"><h1>Why Lorem Ipsum</h1><p>" +
                                  LoremIpsum.WhyLoremIpsum + "</p></div>" +
                                  "</div>" +
                                  "<div class=\"row\">" +
                                  "<div class=\"col-md-6\"><h2>Column 1</h2><p>" + LoremIpsum.SubSection1 +
                                  "</p></div>" +
                                  "<div class=\"col-md-6\"><h2>Column 2</h2><p>" + LoremIpsum.SubSection2 +
                                  "</p></div>" +
                                  "</div>" +
                                  "</main>" +
                                  "</div>";

            DateTimeOffset? published = (await DbContext.Articles.CosmosAnyAsync()) ? null : DateTimeOffset.UtcNow.AddMinutes(-5);

            // Max returns the incorrect result.
            int max;
            if ((await DbContext.ArticleNumbers.CosmosAnyAsync()) == false)
            {
                max = 0;
            }
            else
            {
                max = await DbContext.ArticleNumbers.MaxAsync(m => m.LastNumber);
            }

            // Increment
            max++;

            //
            // New article
            var article = new Article()
            {
                ArticleNumber = max,
                Content = defaultTemplate,
                StatusCode = 0,
                Title = title,
                Updated = DateTimeOffset.Now,
                UrlPath = isFirstArticle ? "root" : HandleUrlEncodeTitle(title),
                VersionNumber = 1,
                Published = isFirstArticle ? DateTimeOffset.UtcNow : null
            };

            DbContext.Articles.Add(article);
            DbContext.ArticleNumbers.Add(new ArticleNumber()
            {
                LastNumber = max
            });

            await DbContext.SaveChangesAsync();

            if (isFirstArticle)
            {
                await HandlePublishing(article, userId);
            }

            // Finally update the catalog entry
            await UpdateCatalogEntry(article.ArticleNumber, (StatusCodeEnum)article.StatusCode);

            return await BuildArticleViewModel(article, "en-US");
        }

        /// <summary>
        ///     Makes an article the new home page.
        /// </summary>
        /// <param name="id">Article Id (row key)</param>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <remarks>
        ///     The old home page has its URL changed from "root" to its normal path.  Also writes to the log
        ///     using <see cref="HandleLogEntry" />.
        /// </remarks>
        public async Task NewHomePage(Guid id, string userId)
        {
            //
            // Can't make a deleted file the new home page.
            //
            var newHome = await DbContext.Articles
                .Where(w => w.Id == id && w.StatusCode != (int)StatusCodeEnum.Deleted).ToListAsync();
            if (newHome == null) throw new Exception($"Article Id {id} not found.");
            var utcDateTimeNow = DateTime.Now.ToUniversalTime();
            if (newHome.All(a => a.Published != null && a.Published.Value <= utcDateTimeNow))
                throw new Exception("Article has not been published yet.");

            var currentHome = await DbContext.Articles.Where(w => w.UrlPath.ToLower() == "root").ToListAsync();

            var newUrl = HandleUrlEncodeTitle(currentHome.FirstOrDefault()?.Title);

            foreach (var article in currentHome) article.UrlPath = newUrl;

            await DbContext.SaveChangesAsync();

            foreach (var article in newHome) article.UrlPath = "root";

            await DbContext.SaveChangesAsync();

            var newHomeArticle = newHome.OrderByDescending(o => o.Id).FirstOrDefault(w => w.Published != null);

            if (newHomeArticle != null)
                await HandleLogEntry(newHomeArticle, $"Article {newHomeArticle.ArticleNumber} is now the new home page.",
                    userId);

            // Handle publishing changes
            await HandlePublishing(currentHome.FirstOrDefault(), userId);
            await HandlePublishing(newHomeArticle, userId);
        }

        /// <summary>
        ///     This method puts an article into trash, and, all its versions.
        /// </summary>
        /// <param name="articleNumber"></param>
        /// <returns></returns>
        /// <remarks>
        ///     <para>This method puts an article into trash. Use <see cref="RetrieveFromTrash" /> to restore an article. </para>
        ///     <para>It also removes it from the page catalog and any published pages..</para>
        ///     <para>WARNING: Make sure the menu MenuController.Index does not reference deleted files.</para>
        /// </remarks>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        public async Task TrashArticle(int articleNumber)
        {
            var doomed = await DbContext.Articles.Where(w => w.ArticleNumber == articleNumber).ToListAsync();

            if (doomed == null) throw new KeyNotFoundException($"Article number {articleNumber} not found.");

            if (doomed.Any(a => a.UrlPath.ToLower() == "root"))
                throw new NotSupportedException(
                    "Cannot trash the home page.  Replace home page with another, then send to trash.");

            foreach (var article in doomed) article.StatusCode = (int)StatusCodeEnum.Deleted;

            var doomedPages = await DbContext.Pages.Where(w => w.ArticleNumber == articleNumber).ToListAsync();
            DbContext.Pages.RemoveRange(doomedPages);

            await DbContext.SaveChangesAsync();
            await DeleteCatalogEntry(articleNumber);

        }

        /// <summary>
        /// Permanently deletes an <paramref name="articleNumber"/>, does not trash item.
        /// </summary>
        /// <param name="articleNumber"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public async Task Purge(int articleNumber)
        {
            var doomed = await DbContext.Articles.Where(w => w.ArticleNumber == articleNumber).ToListAsync();

            if (doomed == null) throw new KeyNotFoundException($"Article number {articleNumber} not found.");

            DbContext.Articles.RemoveRange(doomed);

            await DbContext.SaveChangesAsync();

            await DeleteCatalogEntry(articleNumber);

        }

        /// <summary>
        ///     Retrieves and article and all its versions from trash.
        /// </summary>
        /// <param name="articleNumber"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <remarks>
        ///     <para>
        ///         Please be aware of the following:
        ///     </para>
        ///     <list type="bullet">
        ///         <item><see cref="Article.StatusCode" /> is set to <see cref="StatusCodeEnum.Active" />.</item>
        ///         <item><see cref="Article.Title" /> will be altered if a live article exists with the same title.</item>
        ///         <item>
        ///             If the title changed, the <see cref="Article.UrlPath" /> will be updated using
        ///             <see cref="ArticleLogic.HandleUrlEncodeTitle" />.
        ///         </item>
        ///         <item>The article and all its versions are set to unpublished (<see cref="Article.Published" /> set to null).</item>
        ///         <item>Article is added back to the article catalog.</item>
        ///     </list>
        /// </remarks>
        public async Task RetrieveFromTrash(int articleNumber, string userId)
        {
            var redeemed = await DbContext.Articles.Where(w => w.ArticleNumber == articleNumber).ToListAsync();

            if (redeemed == null) throw new KeyNotFoundException($"Article number {articleNumber} not found.");

            var title = redeemed.FirstOrDefault()?.Title.ToLower();

            // Avoid restoring an article that has a title that collides with a live article.
            if (await DbContext.Articles.Where(a =>
                a.Title.ToLower() == title && a.ArticleNumber != articleNumber &&
                a.StatusCode == (int)StatusCodeEnum.Deleted).CosmosAnyAsync())
            {
                var newTitle = title + " (" + await DbContext.Articles.CountAsync() + ")";
                var url = HandleUrlEncodeTitle(newTitle);
                foreach (var article in redeemed)
                {
                    article.Title = newTitle;
                    article.UrlPath = url;
                    article.StatusCode = (int)StatusCodeEnum.Active;
                    article.Published = null;
                }
            }
            else
            {
                foreach (var article in redeemed)
                {
                    article.StatusCode = (int)StatusCodeEnum.Active;
                    article.Published = null;
                }
            }

            // Add back to the catalog
            var sample = redeemed.FirstOrDefault();
            DbContext.ArticleCatalog.Add(new CatalogEntry()
            {
                ArticleNumber = sample.ArticleNumber,
                Published = null,
                Status = "Active",
                Title = sample.Title,
                Updated = DateTimeOffset.Now,
                UrlPath = sample.UrlPath
            });

            await DbContext.SaveChangesAsync();

            // Update the log
            await HandleLogEntry(redeemed.LastOrDefault(), $"Recovered '{sample.Title}' from trash.", userId);

        }

        #endregion

        #region SAVE ARTICLE METHODS

        /// <summary>
        /// Updates or inserts a 
        /// </summary>
        /// <param name="model"></param>
        /// <param name="userId"></param>
        /// <param name="updateExisting"></param>
        /// <returns></returns>
        public async Task<ArticleUpdateResult> Save(HtmlEditorViewModel model, string userId, bool updateExisting)
        {
            ArticleViewModel entity;
            if (model.ArticleNumber == 0)
            {
                entity = new ArticleViewModel()
                {
                    ArticleNumber = model.ArticleNumber,
                    Content = "",
                    Id = model.Id,
                    Published = model.Published,
                    RoleList = model.RoleList,
                    Title = model.Title,
                    VersionNumber = model.VersionNumber,
                    UrlPath = model.UrlPath
                };
            }
            else
            {
                entity = await Get(model.Id, EnumControllerName.Edit, userId);

                entity.Published = model.Published;
                entity.Title = model.Title;
                entity.RoleList = model.RoleList;
                entity.VersionNumber = model.VersionNumber;
            }

            return await Save(entity, userId, updateExisting);
        }

        /// <summary>
        ///     Updates an existing article, or inserts a new one.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="userId"></param>
        /// <param name="updateExisting"></param>
        /// <remarks>
        ///     <para>
        ///         If the article number is '0', a new article is inserted.  If a version number is '0', then
        ///         a new version is created. Recreates <see cref="ArticleViewModel" /> using method
        ///         <see cref="ArticleLogic.BuildArticleViewModel(Article, string)" />.
        ///     </para>
        ///     <list type="bullet">
        ///         <item>
        ///             Published articles will trigger the prior published article to have its Expired property set to this
        ///             article's published property.
        ///         </item>
        ///         <item>
        ///             Actions taken here by users are logged using <see cref="HandleLogEntry" />.
        ///         </item>
        ///         <item>
        ///             Title changes (and redirects) are handled by adding a new article with redirect info.
        ///         </item>
        ///         <item>
        ///             The <see cref="ArticleViewModel" /> that is returned, is rebuilt using
        ///             <see cref="ArticleLogic.BuildArticleViewModel(Article, string)" />.
        ///         </item>
        ///         <item>
        ///            <see cref="Article.Updated"/> property is automatically updated with current UTC date and time.
        ///         </item>
        ///     </list>
        /// </remarks>
        /// <returns></returns>
        public async Task<ArticleUpdateResult> Save(ArticleViewModel model, string userId, bool updateExisting)
        {
            //
            // Retrieve the article that we will be using.
            // This will either be used to create a new version (detached then added as new),
            // or updated in place.
            //
            var existing = await DbContext.Articles.FirstOrDefaultAsync(a => a.Id == model.Id);

            if (existing == null)
            {
                throw new NotFoundException($"Article ID: {model.Id} not found.");
            }

            Article article;

            if (updateExisting)
            {
                article = existing;
            }
            else
            {
                var nextVersion = (await DbContext.Articles.Where(w => w.ArticleNumber == existing.ArticleNumber).MaxAsync(m => m.VersionNumber)) + 1;

                article = new Article()
                {
                    ArticleNumber = existing.ArticleNumber,
                    // Content = model.Content, // Set content below.
                    Expires = model.Expires,
                    //FooterJavaScript = model.FooterJavaScript, // Set content below.
                    // HeaderJavaScript = model.HeadJavaScript, // Set content below.
                    Id = Guid.NewGuid(),
                    VersionNumber = nextVersion,
                    Published = model.Published,
                    RoleList = existing.RoleList,
                    StatusCode = existing.StatusCode,
                    Title = model.Title,
                    Updated = DateTimeOffset.UtcNow,
                    UrlPath = model.UrlPath
                };
            }


            // =======================================================
            // BEGIN: MAKE CONTENT CHANGES HERE
            //
            #region UPDATE ENTITY WITH NEW CONTENT FROM MODEL

            model.Content = Ensure_ContentEditable_IsMarked(model.Content);

            Ensure_Oembed_Handled(model);

            //
            // Update content
            //
            if (model.Content == null || model.Content.Trim() == "")
            {
                article.Content = "";
            }
            else
            {
                //// When we save to the database, remove content editable attribute.
                article.Content = model.Content.Replace("contenteditable=", "crx=",
                    StringComparison.CurrentCultureIgnoreCase);
            }

            //
            // Update page head and footer
            //
            article.HeaderJavaScript = model.HeadJavaScript;
            article.FooterJavaScript = model.FooterJavaScript;

            article.RoleList = model.RoleList;

            #endregion
            //
            // END: MAKE MODEL CHANGES HERE
            // =======================================================

            UpdateHeadBaseTag(article);

            // Make sure base tag is set properly.
            UpdateHeadBaseTag(model);

            //
            // We are adding a new version.
            //
            if (updateExisting)
            {
                DbContext.Entry(article).State = EntityState.Modified;
                // Make sure this saves now
                await DbContext.SaveChangesAsync();
                await HandleLogEntry(article, "Updated existing version", userId);
            }
            else
            {
                DbContext.Articles.Add(article); // Put this entry in an add state
                                                 // Make sure this saves now
                await DbContext.SaveChangesAsync();
                await HandleLogEntry(article, "New version", userId);
            }



            // IMPORTANT!
            // Handle title (and URL) changes for existing 
            await HandleTitleChange(existing, model.Title, userId);

            // HANDLE PUBLISHING OF AN ARTICLE
            // This can be a new or existing article.
            await HandlePublishing(article, userId);

            //
            // Is the role list changing?
            //
            if (!string.Equals(article.RoleList, model.RoleList, StringComparison.CurrentCultureIgnoreCase))
            {
                // get all prior article versions, changing security now.
                var oldArticles = await DbContext.Articles.Where(w => w.ArticleNumber == article.ArticleNumber)
                    .ToListAsync();

                await HandleLogEntry(article, $"Changing role access from '{article.RoleList}' to '{model.RoleList}'.",
                    userId);

                //
                // We have to change the title and paths for all versions now.
                //
                foreach (var oldArticle in oldArticles) oldArticle.RoleList = model.RoleList;

                // Save changes to database.
                await DbContext.SaveChangesAsync();
            }

            // Finally update the catalog entry
            await UpdateCatalogEntry(article.ArticleNumber, (StatusCodeEnum)article.StatusCode);

            var result = new ArticleUpdateResult
            {
                Model = await BuildArticleViewModel(article, "en-US")
            };

            return result;
        }

        /// <summary>
        /// Logic handing logic for publishing articles and saves changes to the database.
        /// </summary>
        /// <param name="article"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        /// <remarks>
        /// If article is published, it adds the correct versions to the public pages collection. If not, 
        /// the article is removed from the public pages collection.
        /// </remarks>
        private async Task HandlePublishing(Article article, string userId)
        {
            if (article.Published.HasValue)
            {
                await HandleLogEntry(article, $"Published for: {article.Published.Value}.", userId);

                try
                {
                    var others = await DbContext.Articles.Where(w => w.ArticleNumber == article.ArticleNumber && w.Published != null && w.Id != article.Id).ToListAsync();

                    var now = DateTimeOffset.Now;
                    //
                    // If published in the future, then keep the last published article
                    if (article.Published.Value > now)
                    {
                        // Keep the article pulished just before this one
                        var oneTokeep = others.Where(
                            w => w.Published <= now // other published date is before the article
                            && w.VersionNumber < article.VersionNumber).OrderByDescending(o => o.VersionNumber).FirstOrDefault();

                        if (oneTokeep != null)
                        {
                            others.Remove(oneTokeep);
                        }

                        // Also keep the other articles that are published between now and before the current article
                        var othersToKeep = others.Where(
                            w => w.Published.Value > now // Save items published after now, and...
                            && w.Published.Value < article.Published.Value // published before the current article
                            && w.VersionNumber < article.VersionNumber // and are a version number before this one.
                            ).ToList();

                        foreach (var o in othersToKeep)
                        {
                            others.Remove(o);
                        }

                    }

                    // Now remove the other ones published
                    foreach (var item in others)
                    {
                        item.Published = null;
                    }

                    await DbContext.SaveChangesAsync();

                    // Resets the expiration dates, based on the last published article
                    await ResetVersionExpirations(article.ArticleNumber);

                    // Update the published pages collection
                    await UpdatePublishedPages(article.ArticleNumber);

                }
                catch (Exception e)
                {
                    var t = e;
                }
            }
        }

        /// <summary>
        /// Updates the published pages collection by article number
        /// </summary>
        /// <param name="articleNumber"></param>
        /// <returns></returns>
        private async Task UpdatePublishedPages(int articleNumber)
        {
            // Now we are going to update the Pages table
            var itemsToPublish = await DbContext.Articles.Where(w => w.ArticleNumber == articleNumber && w.Published != null)
                .OrderByDescending(o => o.Published).AsNoTracking().ToListAsync();

            var paths = itemsToPublish.Select(s => s.UrlPath).Distinct().ToArray();

            // Get everything that is going to be removed or replaced
            var itemsToRemove = await DbContext.Pages.Where(w => w.ArticleNumber == articleNumber || paths.Contains(w.UrlPath)).ToListAsync();

            // Mark these for deletion - do this first to remove any conflicts
            DbContext.Pages.RemoveRange(itemsToRemove);
            await DbContext.SaveChangesAsync();


            // Now refresh the published pages
            foreach (var item in itemsToPublish)
            {
                var newPage = new Common.Data.PublishedPage()
                {
                    ArticleNumber = item.ArticleNumber,
                    Content = item.Content,
                    Expires = item.Expires,
                    FooterJavaScript = item.FooterJavaScript,
                    HeaderJavaScript = item.HeaderJavaScript,
                    Id = Guid.NewGuid(), // Use a new Guid
                    Published = item.Published,
                    RoleList = item.RoleList,
                    StatusCode = item.StatusCode,
                    Title = item.Title,
                    Updated = item.Updated,
                    UrlPath = item.UrlPath,
                    VersionNumber = item.VersionNumber
                };

                // Check for duplicate
                var duplicate = await DbContext.Pages.FirstOrDefaultAsync(f => f.Id == newPage.Id);
                if (duplicate == null)
                {
                    DbContext.Pages.Add(newPage);
                }
                else
                {
                    throw new Exception($"Duplicate Page Id. Existing: {duplicate.Id} New: {newPage.Id} ArticleId: {articleNumber}.");
                }

                
            }

            // Update the pages collection
            await DbContext.SaveChangesAsync();
        }


        /// <summary>
        /// If the title has changed, handle that here
        /// </summary>
        /// <param name="article"></param>
        /// <param name="newTitle"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <remarks>
        /// Upon title change:
        /// <list type="bullet">
        /// <item>Updates title for article and it's versions</item>
        /// <item>Updates the article catalog</item>
        /// <item>Updates title of all child articles</item>
        /// <item>Creates an automatic redirect</item>
        /// <item>Updates base tags for all articles changed</item>
        /// <item>Saves changes to the database</item>
        /// </list>
        /// </remarks>
        private async Task HandleTitleChange(Article article, string newTitle, string userId)
        {

            if (string.Equals(article.Title, newTitle, StringComparison.CurrentCultureIgnoreCase))
            {
                // Nothing to do
                return;
            }

            var articleNumbersToUpdate = new List<int>();

            articleNumbersToUpdate.Add(article.ArticleNumber);

            //
            //  Validate that title is not already taken.
            //
            if (!await ValidateTitle(newTitle, article.ArticleNumber))
                throw new Exception($"Title '{newTitle}' already taken");

            var oldTitle = article.Title;

            var oldUrl = HandleUrlEncodeTitle(oldTitle);
            var newUrl = HandleUrlEncodeTitle(newTitle);

            // If NOT the root, handle any child page updates and redirects
            // that need to be created.
            if (article.UrlPath != "root")
            {
                // Update sub articles.
                var subArticles = await GetAllSubArticles(oldTitle);

                foreach (var subArticle in subArticles)
                {
                    if (!subArticle.Title.Equals("redirect", StringComparison.CurrentCultureIgnoreCase))
                    {
                        subArticle.Title = UpdatePrefix(oldTitle, newTitle, subArticle.Title);
                    }
                    subArticle.UrlPath = UpdatePrefix(oldUrl, newUrl, subArticle.UrlPath);

                    // Make sure base tag is set properly.
                    UpdateHeadBaseTag(subArticle);

                    articleNumbersToUpdate.Add(article.ArticleNumber);
                }

                DbContext.Articles.UpdateRange(subArticles);

                // Remove any conflicting redirects
                var conflictingRedirects = await DbContext.Articles.Where(a => a.Content == newUrl && a.Title.ToLower().Equals("redirect")).ToListAsync();

                if (conflictingRedirects.Any())
                {
                    DbContext.Articles.RemoveRange(conflictingRedirects);
                    articleNumbersToUpdate.AddRange(conflictingRedirects.Select(s => s.ArticleNumber).ToList());
                }

                //
                // Update base href
                //
                UpdateHeadBaseTag(article);

                // Create a redirect
                var entity = new Article
                {
                    ArticleNumber = 0,
                    StatusCode = (int)StatusCodeEnum.Redirect,
                    UrlPath = oldUrl, // Old URL
                    VersionNumber = 0,
                    Published = DateTime.Now.ToUniversalTime().AddDays(-1), // Make sure this sticks!
                    Title = "Redirect",
                    Content = newUrl, // New URL
                    Updated = DateTime.Now.ToUniversalTime(),
                    HeaderJavaScript = null,
                    FooterJavaScript = null
                };

                // Add redirect here
                DbContext.Articles.Add(entity);

                await HandleLogEntry(entity, $"Redirect {oldUrl} to {newUrl}", userId);
            }

            // We have to change the title and paths for all versions now.
            var versions = await DbContext.Articles.Where(w => w.ArticleNumber == article.ArticleNumber)
                .ToListAsync();

            var catalog = await DbContext.ArticleCatalog.Where(a => a.ArticleNumber == article.ArticleNumber).ToListAsync();

            foreach (var art in versions)
            {
                //
                // Update base href (for Angular apps)
                //
                UpdateHeadBaseTag(article);

                art.Title = newTitle;
                art.Updated = DateTime.Now.ToUniversalTime();
                art.UrlPath = article.UrlPath;
            }

            foreach (var item in catalog)
            {
                item.Title = newTitle;
                item.Updated = DateTime.Now.ToUniversalTime();
                item.UrlPath = article.UrlPath;
            }

            DbContext.Articles.UpdateRange(versions);
            DbContext.ArticleCatalog.UpdateRange(catalog);

            await DbContext.SaveChangesAsync();

            // Now update the published pages
            foreach (var num in articleNumbersToUpdate)
            {
                await UpdatePublishedPages(num);
            }

        }

        private string UpdatePrefix(string oldprefix, string newPrefix, string targetString)
        {
            var updated = newPrefix + targetString.TrimStart(oldprefix.ToArray());
            return updated;
        }

        /// <summary>
        /// Update head tag to match path 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <remarks>
        /// Angular uses the BASE tag within the HEAD to set relative path to article/app.
        /// If that tag is detected, it is updated automatically to match the current <see cref="Article.UrlPath"/>.
        /// </remarks>
        public void UpdateHeadBaseTag(ArticleViewModel model)
        {
            if (!string.IsNullOrEmpty(model.HeadJavaScript) && model.HeadJavaScript.Contains("<base "))
            {
                model.HeadJavaScript = UpdateHeadBaseTag(model.HeadJavaScript, model.UrlPath);
            }
            return;
        }

        /// <summary>
        /// Update head tag to match path 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <remarks>
        /// Angular uses the BASE tag within the HEAD to set relative path to article/app.
        /// If that tag is detected, it is updated automatically to match the current <see cref="Article.UrlPath"/>.
        /// </remarks>
        public void UpdateHeadBaseTag(Article model)
        {
            if (!string.IsNullOrEmpty(model.HeaderJavaScript) && (model.HeaderJavaScript.Contains("<base ") || (model.HeaderJavaScript.ToLower().Contains("ccms:framework") && model.HeaderJavaScript.ToLower().Contains("angular"))))
            {
                model.HeaderJavaScript = UpdateHeadBaseTag(model.HeaderJavaScript, model.UrlPath);
            }
            return;
        }

        /// <summary>
        /// Updates the base tag in the head if Angular is being used.
        /// </summary>
        /// <param name="headerJavaScript"></param>
        /// <param name="urlPath"></param>
        /// <returns></returns>
        private string UpdateHeadBaseTag(string headerJavaScript, string urlPath)
        {
            if (string.IsNullOrEmpty(headerJavaScript))
            {
                return "";
            }

            var htmlDoc = new HtmlAgilityPack.HtmlDocument();

            htmlDoc.LoadHtml(headerJavaScript);

            // <meta name="ccms:framework" value="angular">
            var meta = htmlDoc.DocumentNode.SelectSingleNode("//meta[@name='ccms:framework']");

            // This only needs to be run if the framework is "Angular"
            if (meta != null && meta.Attributes["value"].Value.ToLower() != "angular")
            {
                return headerJavaScript;
            }

            var element = htmlDoc.DocumentNode.SelectSingleNode("//base");

            urlPath = $"/{HttpUtility.UrlDecode(urlPath.ToLower().Trim('/'))}/";

            if (element == null)
            {
                var metaTag = htmlDoc.CreateElement("base");
                metaTag.SetAttributeValue("href", urlPath);
                htmlDoc.DocumentNode.AppendChild(metaTag);
            }
            else
            {
                var href = element.Attributes["href"];

                if (href == null)
                {
                    element.Attributes.Add("href", urlPath);
                }
                else
                {
                    href.Value = urlPath;
                }
            }


            headerJavaScript = htmlDoc.DocumentNode.OuterHtml;

            return headerJavaScript;
        }

        /// <summary>
        ///     Changes the status of an article by marking all versions with that status.
        /// </summary>
        /// <param name="articleNumber">Article to set status for</param>
        /// <param cref="StatusCodeEnum" name="code"></param>
        /// <param name="userId"></param>
        /// <exception cref="Exception">User ID or article number not found.</exception>
        /// <returns>Returns the number of versions for the given article where status was set</returns>
        public async Task<int> SetStatus(int articleNumber, StatusCodeEnum code, string userId)
        {
            if (!await DbContext.Users.Where(a => a.Id == userId).CosmosAnyAsync())
                throw new Exception($"User ID: {userId} not found!");

            var versions =
                await DbContext.Articles.Where(a => a.ArticleNumber == articleNumber).ToListAsync();

            if (!versions.Any())
                throw new Exception($"Article number: {articleNumber} not found!");

            foreach (var version in versions)
            {
                version.StatusCode = (int)code;

                var statusText = code switch
                {
                    StatusCodeEnum.Deleted => "deleted",
                    StatusCodeEnum.Active => "active",
                    _ => "inactive"
                };

                DbContext.ArticleLogs.Add(new ArticleLog
                {
                    ActivityNotes = $"Status changed to '{statusText}'.",
                    IdentityUserId = userId,
                    DateTimeStamp = DateTime.Now.ToUniversalTime(),
                    ArticleId = version.Id
                });

            }

            var count = await DbContext.SaveChangesAsync();
            return count;
        }

        #endregion

        #region GET METHODS ONLY FOR EDITOR

        /// <summary>
        ///     Gets a copy of the article ready for edit.
        /// </summary>
        /// <param name="articleNumber">Article Number</param>
        /// <param name="versionNumber">Version to edit</param>
        /// <returns>
        ///     <see cref="ArticleViewModel" />
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         Returns <see cref="ArticleViewModel" />. For more details on what is returned, see
        ///         <see cref="ArticleLogic.BuildArticleViewModel(Article, string)" />
        ///     </para>
        ///     <para>NOTE: Cannot access articles that have been deleted.</para>
        /// </remarks>
        public async Task<ArticleViewModel> Get(int articleNumber, int versionNumber)
        {
            var article = await DbContext.Articles
                .FirstOrDefaultAsync(
                    a => a.ArticleNumber == articleNumber &&
                         a.VersionNumber == versionNumber &&
                         a.StatusCode != 2);

            if (article == null)
                throw new Exception($"Article number:{articleNumber}, Version:{versionNumber}, not found.");

            return await BuildArticleViewModel(article, "en-US");
        }


        /// <summary>
        ///     Gets an article by ID (row Key), or creates a new (unsaved) article if id is null.
        /// </summary>
        /// <param name="id">Row Id (or identity) number.  If null returns a new article.</param>
        /// <param name="controllerName"></param>
        /// <param name="userId"></param>
        /// <remarks>
        ///     <para>
        ///         For new articles, uses <see cref="Create" /> and the method
        ///         <see cref="ArticleLogic.BuildArticleViewModel(Article, string)" /> to
        ///         generate the <see cref="ArticleViewModel" /> .
        ///     </para>
        ///     <para>
        ///         Retrieves <see cref="Article" /> and builds an <see cref="ArticleViewModel" /> using the method
        ///         <see cref="ArticleLogic.BuildArticleViewModel(Article, string)" />,
        ///         or in the case of a template, uses method <see cref="BuildTemplateViewModel" />.
        ///     </para>
        /// </remarks>
        /// <returns>
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         Returns <see cref="ArticleViewModel" />. For more details on what is returned, see
        ///         <see cref="ArticleLogic.BuildArticleViewModel(Article, string)" /> or <see cref="BuildTemplateViewModel" />.
        ///     </para>
        ///     <para>NOTE: Cannot access articles that have been deleted.</para>
        /// </remarks>
        public async Task<ArticleViewModel> Get(Guid? id, EnumControllerName controllerName, string userId)
        {
            if (controllerName == EnumControllerName.Template)
            {
                if (id == null)
                    throw new Exception("Template ID:null not found.");

                var idNo = id.Value;
                var template = await DbContext.Templates.FindAsync(idNo);

                if (template == null) throw new Exception($"Template ID:{id} not found.");
                return BuildTemplateViewModel(template);
            }

            //
            // This is used to create a "blank" page just so we have something to get started with.
            //
            if (id == null)
            {
                var count = await DbContext.Articles.CountAsync();
                return await Create("Page " + count, userId);
            }

            var article = await DbContext.Articles
                .FirstOrDefaultAsync(a => a.Id == id && a.StatusCode != 2);

            article.Content = Ensure_ContentEditable_IsMarked(article.Content);

            if (article == null) throw new Exception($"Article ID:{id} not found.");
            return await BuildArticleViewModel(article, "en-US");
        }

        /// <summary>
        /// Gets the sub articles for a page
        /// </summary>
        /// <param name="urlPrefix">URL Prefix</param>
        /// <returns></returns>
        private async Task<List<Article>> GetAllSubArticles(string urlPrefix)
        {
            if (string.IsNullOrEmpty(urlPrefix) || string.IsNullOrWhiteSpace(urlPrefix) || urlPrefix.Equals("/"))
            {
                urlPrefix = "";
            }
            else
            {
                urlPrefix = System.Web.HttpUtility.UrlDecode(urlPrefix.ToLower().Replace("%20", "_").Replace(" ", "_"));
            }

            var query = DbContext.Articles.Where(a => a.UrlPath.StartsWith(urlPrefix));
            //    .Where(a => a.Published <= DateTime.UtcNow && a.UrlPath.Like());

            try
            {
                var list = await query.ToListAsync();
                return list;
            }
            catch (Exception e)
            {
                var t = e; // For debuging purposes
                throw;
            }


        }

        /// <summary>
        /// Gets a page, and allows unpublished or inactive pages to be returned.
        /// </summary>
        /// <param name="urlPath"></param>
        /// <param name="lang"></param>
        /// <param name="publishedOnly"></param>
        /// <param name="onlyActive"></param>
        /// <returns></returns>
        public async Task<ArticleViewModel> GetByUrl(string urlPath, string lang = "", bool publishedOnly = true,
            bool onlyActive = true)
        {
            if (publishedOnly && onlyActive)
            {
                return await base.GetByUrl(urlPath, lang);
            }

            var activeStatusCodes =
                onlyActive ? new[] { 0, 3 } : new[] { 0, 1, 3 }; // i.e. StatusCode.Active (DEFAULT) and StatusCode.Redirect

            if (publishedOnly)
            {
                var article = await DbContext.Pages.WithPartitionKey(urlPath)
                    .Where(a => a.Published <= DateTimeOffset.UtcNow &&
                                activeStatusCodes.Contains(a.StatusCode)) // Now filter on active status code.
                    .OrderByDescending(o => o.VersionNumber).FirstOrDefaultAsync();
                return await base.BuildArticleViewModel(article, lang, null);
            }
            else
            {
                // Will search unpublished and published articles
                var article = await DbContext.Articles
                    .Where(a => a.UrlPath == urlPath && activeStatusCodes.Contains(a.StatusCode))
                    .OrderByDescending(o => o.VersionNumber)
                    .FirstOrDefaultAsync();
                return await base.BuildArticleViewModel(article, lang);
            }

        }

        #region LISTS

        /// <summary>
        /// Get a list of article redirects
        /// </summary>
        /// <returns></returns>
        public IQueryable<RedirectItemViewModel> GetArticleRedirects()
        {
            var redirectCode = (int)StatusCodeEnum.Redirect;
            var query = DbContext.Articles.Where(w => w.StatusCode == redirectCode);

            return query.Select(s => new RedirectItemViewModel()
            {
                FromUrl = s.UrlPath,
                Id = s.Id,
                ToUrl = s.Content
            });
        }

        /// <summary>
        ///     Gets the latest versions of articles that are in the trash.
        /// </summary>
        /// <returns>Gets article number, version number, last data published (if applicable)</returns>
        /// <remarks>
        /// </remarks>
        public async Task<List<ArticleListItem>> GetArticleTrashList()
        {

            var data = await
                (from x in DbContext.Articles
                 where x.StatusCode == (int)StatusCodeEnum.Deleted
                 select new
                 {
                     x.ArticleNumber,
                     x.VersionNumber,
                     x.Published,
                     x.StatusCode,
                     x.Title
                 }).ToListAsync();

            var model =
                (from x in data
                 where x.StatusCode == (int)StatusCodeEnum.Deleted
                 group x by x.ArticleNumber
                    into g
                 select new ArticleListItem
                 {
                     ArticleNumber = g.Key,
                     Title = g.FirstOrDefault().Title,
                     VersionNumber = g.Max(i => i.VersionNumber),
                     LastPublished = g.Max(m => m.Published),
                     Status = g.Max(f => f.StatusCode) == 0 ? "Active" : "Inactive"
                 }).ToList();


            return model;
        }

        #endregion

        #endregion

        #region PAGE EXPORT

        /// <summary>
        /// Exports and article as HTML with layout elements.
        /// </summary>
        /// <param name="article"></param>
        /// <param name="blobPublicAbsoluteUrl"></param>
        /// <param name="viewRenderService"></param>
        /// <returns>web page</returns>
        public async Task<string> ExportArticle(ArticleViewModel article, Uri blobPublicAbsoluteUrl, Services.IViewRenderService viewRenderService)
        {

            var htmlUtilities = new Services.HtmlUtilities();

            article.Layout.Head = htmlUtilities.RelativeToAbsoluteUrls(article.Layout.Head, blobPublicAbsoluteUrl, false);

            // Layout body elements
            article.Layout.HtmlHeader = htmlUtilities.RelativeToAbsoluteUrls(article.Layout.HtmlHeader, blobPublicAbsoluteUrl, true);
            article.Layout.FooterHtmlContent = htmlUtilities.RelativeToAbsoluteUrls(article.Layout.FooterHtmlContent, blobPublicAbsoluteUrl, true);

            article.HeadJavaScript = htmlUtilities.RelativeToAbsoluteUrls(article.HeadJavaScript, blobPublicAbsoluteUrl, false);
            article.Content = htmlUtilities.RelativeToAbsoluteUrls(article.Content, blobPublicAbsoluteUrl, false);
            article.FooterJavaScript = htmlUtilities.RelativeToAbsoluteUrls(article.FooterJavaScript, blobPublicAbsoluteUrl, false);

            var html = await viewRenderService.RenderToStringAsync("~/Views/Editor/ExportPage.cshtml", article);

            return html;
        }

        #endregion

        /// <summary>
        /// Deletes a catalog entry
        /// </summary>
        /// <param name="articleNumber"></param>
        /// <returns></returns>
        private async Task DeleteCatalogEntry(int articleNumber)
        {
            var catalogEntry = await DbContext.ArticleCatalog.FirstOrDefaultAsync(f => f.ArticleNumber == articleNumber);
            DbContext.ArticleCatalog.Remove(catalogEntry);
            await DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Update catalog entry
        /// </summary>
        /// <param name="articleNumber"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        private async Task UpdateCatalogEntry(int articleNumber, StatusCodeEnum code)
        {
            var versions = await DbContext.Articles.Where(w => w.ArticleNumber == articleNumber).ToListAsync();

            var catalogEntry = await DbContext.ArticleCatalog.FirstOrDefaultAsync(f => f.ArticleNumber == articleNumber);

            if (catalogEntry == null)
            {
                var data = versions.FirstOrDefault();

                catalogEntry = new CatalogEntry()
                {
                    ArticleNumber = articleNumber,
                    Updated = data.Updated,
                    Status = code == StatusCodeEnum.Active ? "Active" : "Inactive",
                    Published = data.Published,
                    Title = data.Title,
                    UrlPath = data.UrlPath
                };

                DbContext.ArticleCatalog.Add(catalogEntry);
            }
            else
            {
                var data = (from v in versions
                            group v by v.Title into summary
                            select new CatalogEntry
                            {
                                Title = summary.Key,
                                ArticleNumber = articleNumber,
                                Published = summary.Max(m => m.Published),
                                Updated = summary.Max(m => m.Updated),
                                UrlPath = catalogEntry.UrlPath
                            }).FirstOrDefault();

                catalogEntry.Updated = data.Updated;
                catalogEntry.Status = code == StatusCodeEnum.Active ? "Active" : "Inactive";
                catalogEntry.Published = data.Published;
                catalogEntry.Title = data.Title;
                catalogEntry.UrlPath = data.UrlPath;
            }

            await DbContext.SaveChangesAsync();
        }
    }
}
