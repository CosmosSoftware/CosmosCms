using Cosmos.Cms.Common.Models;
using Cosmos.Cms.Common.Services;
using Cosmos.Cms.Common.Services.Configurations;
using Google.Cloud.Translate.V3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static IdentityModel.ClaimComparer;

namespace Cosmos.Cms.Common.Data.Logic
{
    /// <summary>
    ///     Main logic behind getting and maintaining web site articles.
    /// </summary>
    /// <remarks>An article is the "content" behind a web page.</remarks>
    public class ArticleLogic
    {
        private readonly bool _isEditor;
        private readonly TranslationServices _translationServices;
        /// <summary>
        ///     Publisher Constructor
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="distributedCache"></param>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        /// <param name="memoryCache">Memory cache used only by Publishers</param>
        /// <param name="isEditor">Is in edit mode or not (by passess redis if set to true)</param>
        /// <param name="memoryCacheMaxSeconds">Maximum seconds to store item in memory cache.</param>
        public ArticleLogic(ApplicationDbContext dbContext,
            IOptions<CosmosConfig> config,
            bool isEditor = false)
        {
            DbContext = dbContext;
            CosmosOptions = config;
            if (config.Value.GoogleCloudAuthConfig != null &&
                string.IsNullOrEmpty(config.Value.GoogleCloudAuthConfig.ClientId) == false)
                _translationServices = new TranslationServices(config);
            else
                _translationServices = null;

            _isEditor = isEditor;
        }

        //private readonly bool _editorMode;

        /// <summary>
        ///     Database Content
        /// </summary>
        protected ApplicationDbContext DbContext { get; }

        /// <summary>
        ///     Site customization config
        /// </summary>
        protected IOptions<CosmosConfig> CosmosOptions { get; }

        /// <summary>
        /// Provides cache hit information
        /// </summary>
        public string[] CacheResult { get; internal set; }

        /// <summary>
        /// Gets the list of child pages for a given page URL
        /// </summary>
        /// <param name="prefix">Page url</param>
        /// <param name="pageNo">Zero based index (page 1 is index 0)</param>
        /// <param name="pageSize">Number of records in a page.</param>
        /// <param name="orderByPublishedDate">Order by when was published (most recent on top)</param>
        /// <returns></returns>
        public async Task<TableOfContents> GetTOC(string prefix, int pageNo = 0, int pageSize = 10, bool orderByPublishedDate = false)
        {
            if (string.IsNullOrEmpty(prefix) || string.IsNullOrWhiteSpace(prefix) || prefix.Equals("/"))
            {
                prefix = "";
            }
            else
            {
                prefix = System.Web.HttpUtility.UrlDecode(prefix.ToLower().Replace("%20", "_").Replace(" ", "_")) + "/";
            }
            var skip = pageNo * pageSize;

            //var query = DbContext.Articles.Select(s =>
            //new TOCItem { UrlPath = s.UrlPath, Title = s.Title, Published = s.Published.Value, Updated = s.Updated })
            //    .Where(a => a.Published <= DateTime.UtcNow &&
            //            EF.Functions.Like(a.Title, prefix + "%") &&
            //            (EF.Functions.Like(a.Title, prefix + "%/%") == false)).Distinct();

            var query = (from t in DbContext.Articles
                    where t.Published <= DateTime.UtcNow &&
                    t.StatusCode == 0 &&
                    EF.Functions.Like(t.Title, prefix + "%") &&
                    (EF.Functions.Like(t.Title, prefix + "%/%") == false)
                    group t by new { t.Title, t.UrlPath }
                    into g
                    select new TableOfContentsItem
                    {
                        UrlPath = g.Key.UrlPath,
                        Title = g.Key.Title,
                        Published = g.Max(a => a.Published.Value),
                        Updated = g.Max(a => a.Updated)
                    }).Distinct();
                    

            var model = new TableOfContents();

            List<TableOfContentsItem> results;

            if (orderByPublishedDate)
            {
                results = await query.OrderByDescending(o => o.Published).ToListAsync();
            }
            else
            {
                results = await query.OrderBy(o => o.Title).ToListAsync();
            }

            model.TotalCount = results.Count;
            model.PageNo = pageNo;
            model.PageSize = pageSize;
            model.Items = results.Skip(skip).Take(pageSize).ToList();

            return model;
        }

        #region GET ARTICLE METHODS

        /// <summary>
        ///     Gets the current *published* version of an article.  Gets the home page if ID is null.
        /// </summary>
        /// <param name="urlPath">URL Encoded path</param>
        /// <param name="lang">Language to return content as.</param>
        /// <param name="publishedOnly">Only retrieve latest published version</param>
        /// <param name="onlyActive">Only retrieve active status</param>
        /// <param name="forceUseRedis">Force use of distributed cache</param>
        /// <returns>
        ///     <see cref="ArticleViewModel" />
        /// </returns>
        /// <remarks>
        ///     <para>
        ///     Retrieves an article from the following sources in order:
        ///     </para>
        ///    <list type="number">
        ///       <item>Short term (5 second) Entity Framework Cache</item>
        ///       <item>SQL Database</item>
        ///     </list>
        ///     <para>
        ///         Returns <see cref="ArticleViewModel" />. For more details on what is returned, see <see cref="GetArticle" />
        ///         and <see cref="BuildArticleViewModel" />.
        ///     </para>
        ///     <para>NOTE: Cannot access articles that have been deleted.</para>
        /// </remarks>
        public async Task<ArticleViewModel> GetByUrl(string urlPath, string lang = "", bool publishedOnly = true,
            bool onlyActive = true)
        {
            return await GetArticle(urlPath, publishedOnly, onlyActive, lang);
        }

        /// <summary>
        ///     Private method used return an article view model.
        /// </summary>
        /// <param name="urlPath"></param>
        /// <param name="publishedOnly"></param>
        /// <param name="onlyActive"></param>
        /// <param name="lang">Language to translate the en-US into.</param>
        /// <returns>
        /// <para>Returns an <see cref="ArticleViewModel" /> created with <see cref="BuildArticleViewModel" />.</para>
        /// <para>This method utilizes a memory cache to briefly cache queries to the database.</para>  
        /// <para>NOTE: Cannot access articles that have been deleted.</para>
        /// </returns>
        private async Task<ArticleViewModel> GetArticle(string urlPath, bool publishedOnly, bool onlyActive,
            string lang)
        {

            urlPath = urlPath?.ToLower().Trim(new char[] { ' ', '/' });
            if (string.IsNullOrEmpty(urlPath) || urlPath.Trim() == "/")
                urlPath = "root";

            //var options = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5) };

            var layout = DbContext.Layouts.FirstOrDefault(f => f.IsDefault == true);

            //urlPath = urlPath?.ToLower().Trim('/');
            Article article;
            // Get time zone info
            //var pst = TimeZoneUtility.ConvertUtcDateTimeToPst(DateTime.UtcNow);

            var activeStatusCodes =
                onlyActive ? new[] { 0, 3 } : new[] { 0, 1, 3 }; // i.e. StatusCode.Active (DEFAULT) and StatusCode.Redirect


            if (publishedOnly)
                article = await DbContext.Articles.WithPartitionKey(urlPath)
                    .Where(a => a.Published <= DateTimeOffset.UtcNow &&
                                activeStatusCodes.Contains(a.StatusCode))
                    .OrderByDescending(o => o.VersionNumber).FirstOrDefaultAsync();
            else
                article = await DbContext.Articles.WithPartitionKey(urlPath)
                    .Where(a => activeStatusCodes.Contains(a.StatusCode))
                    .OrderByDescending(o => o.VersionNumber)
                    .FirstOrDefaultAsync();


            if (article == null) return null;

            return await BuildArticleViewModel(article, lang);
        }

        #endregion

        #region PRIVATE METHODS

        /// <summary>
        ///     This method creates an <see cref="ArticleViewModel" /> ready for display and edit.
        /// </summary>
        /// <param name="article"></param>
        /// <param name="lang"></param>
        /// <returns>
        ///     <para>Returns <see cref="ArticleViewModel" /> that includes:</para>
        ///     <list type="bullet">
        ///         <item>
        ///             Current ArticleVersionInfo
        ///         </item>
        ///         <item>
        ///             If the site is in authoring or publishing mode (<see cref="ArticleViewModel.ReadWriteMode" />)
        ///         </item>
        ///     </list>
        /// </returns>
        protected async Task<ArticleViewModel> BuildArticleViewModel(Article article, string lang, bool useCache = true)
        {

            var languageName = "US English";

            if (!string.IsNullOrEmpty(lang) && _translationServices != null && CosmosOptions.Value.GoogleCloudAuthConfig != null && CosmosOptions.Value.PrimaryLanguageCode.Equals(lang, StringComparison.CurrentCultureIgnoreCase) == false)
            {
                var result =
                    await _translationServices.GetTranslation(lang, "", new[] { article.Title, article.Content });

                languageName =
                    (await GetSupportedLanguages(lang))?.Languages.FirstOrDefault(f => f.LanguageCode == lang)
                    ?.DisplayName ?? lang;

                article.Title = result.Translations[0].TranslatedText;

                article.Content = result.Translations[1].TranslatedText;
            }

            
            return new ArticleViewModel
            {
                ArticleNumber = article.ArticleNumber,
                LanguageCode = lang,
                LanguageName = languageName,
                CacheDuration = 10,
                Content = article.Content,
                StatusCode = (StatusCodeEnum)article.StatusCode,
                Id = article.Id,
                Published = article.Published.HasValue ? article.Published.Value : null,
                Title = article.Title,
                UrlPath = article.UrlPath,
                Updated = article.Updated,
                VersionNumber = article.VersionNumber,
                HeadJavaScript = article.HeaderJavaScript,
                FooterJavaScript = article.FooterJavaScript,
                Layout = await GetDefaultLayout(),
                ReadWriteMode = _isEditor,
                RoleList = article.RoleList,
                Expires = article.Expires.HasValue ? article.Expires.Value : null
            };
        }

        /// <summary>
        ///     Provides a standard method for turning a title into a URL Encoded path.
        /// </summary>
        /// <param name="title">Title to be converted into a URL.</param>
        /// <returns></returns>
        /// <remarks>
        ///     <para>This is accomplished using <see cref="HttpUtility.UrlEncode(string)" />.</para>
        ///     <para>Blanks are turned into underscores (i.e. "_").</para>
        ///     <para>All strings are normalized to lower case.</para>
        /// </remarks>
        public static string HandleUrlEncodeTitle(string title)
        {
            return HttpUtility.UrlEncode(title.Trim().Replace(" ", "_").ToLower()).Replace("%2f", "/");
        }

        /// <summary>
        ///     Get the list of languages supported for translation by Google.
        /// </summary>
        /// <param name="lang"></param>
        /// <returns></returns>
        public async Task<SupportedLanguages> GetSupportedLanguages(string lang)
        {
            if (_translationServices == null) return new SupportedLanguages();

            return await _translationServices.GetSupportedLanguages(lang);
        }

        /// <summary>
        ///     Gets the default layout, including navigation menu.
        /// </summary>
        /// <param name="lang"></param>
        /// <param name="includeMenu"></param>
        /// <returns></returns>
        /// <remarks>
        ///     <para>
        ///         Inserts a Bootstrap style nav bar where this '&lt;!--{COSMOS-UL-NAV}--&gt;' is placed in the
        ///         <see cref="LayoutViewModel.HtmlHeader" />
        ///     </para>
        /// </remarks>
        public async Task<LayoutViewModel> GetDefaultLayout(MemoryCacheEntryOptions options = null)
        {
            if (options == null)
            {
                options = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5) };
            }
            LayoutViewModel layoutViewModel;

            var layout = DbContext.Layouts.FirstOrDefault(a => a.IsDefault);

            //
            // If no layout exists, creates a new default one.
            //
            if (layout == null)
            {
                layoutViewModel = new LayoutViewModel();
                layout = layoutViewModel.GetLayout();
                await DbContext.Layouts.AddAsync(layout);
                await DbContext.SaveChangesAsync();
            }
            else
            {
                layoutViewModel = new LayoutViewModel()
                {
                    FooterHtmlContent = layout.FooterHtmlContent,
                    Head = layout.Head,
                    HtmlHeader = layout.HtmlHeader,
                    Id = layout.Id,
                    IsDefault = layout.IsDefault,
                    LayoutName = layout.LayoutName,
                    Notes = layout.Notes
                };
            }

            // Make sure no changes are tracked with the layout.
            DbContext.Entry(layout).State = EntityState.Detached;

            return new LayoutViewModel(layout);
        }


        #endregion

        #region CACHE FUNCTIONS

        /// <summary>
        ///     Serializes an object using <see cref="Newtonsoft.Json.JsonConvert.SerializeObject(object)" />
        ///     and <see cref="System.Text.Encoding.UTF32" />.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] Serialize(object obj)
        {
            if (obj == null) return null;
            return Encoding.UTF32.GetBytes(JsonConvert.SerializeObject(obj));
        }

        /// <summary>
        ///     Deserializes an object using <see cref="Newtonsoft.Json.JsonConvert.DeserializeObject(string)" />
        ///     and <see cref="System.Text.Encoding.UTF32" />.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static T Deserialize<T>(byte[] bytes)
        {
            var data = Encoding.UTF32.GetString(bytes);
            return JsonConvert.DeserializeObject<T>(data);
        }

        #endregion

        /// <summary>
        /// Determines if a publisher can serve requests.
        /// </summary>
        /// <returns></returns>
        public bool GetPublisherHealth()
        {
            return true;
        }
    }
}