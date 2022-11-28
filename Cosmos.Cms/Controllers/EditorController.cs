using Cosmos.Cms.Common.Data;
using Cosmos.Cms.Common.Data.Logic;
using Cosmos.Cms.Common.Models;
using Cosmos.Cms.Common.Services.Configurations;
using Cosmos.Cms.Data.Logic;
using Cosmos.Cms.Models;
using Cosmos.Cms.Services;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cosmos.Cms.Controllers
{
    /// <summary>
    /// Editor controller
    /// </summary>
    [Authorize(Roles = "Reviewers, Administrators, Editors, Authors")]
    public class EditorController : BaseController
    {
        private readonly ArticleEditLogic _articleLogic;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<EditorController> _logger;
        private readonly IOptions<CosmosConfig> _options;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly Uri _blobPublicAbsoluteUrl;
        private readonly IViewRenderService _viewRenderService;
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="dbContext"></param>
        /// <param name="userManager"></param>
        /// <param name="articleLogic"></param>
        /// <param name="options"></param>
        /// <param name="viewRenderService"></param>
        public EditorController(ILogger<EditorController> logger,
            ApplicationDbContext dbContext,
            UserManager<IdentityUser> userManager,
            ArticleEditLogic articleLogic,
            IOptions<CosmosConfig> options,
            IViewRenderService viewRenderService
        ) :
             base(dbContext, userManager, articleLogic, options)
        {
            _logger = logger;
            _dbContext = dbContext;
            _options = options;
            _userManager = userManager;
            _articleLogic = articleLogic;


            var htmlUtilities = new HtmlUtilities();

            if (htmlUtilities.IsAbsoluteUri(options.Value.SiteSettings.BlobPublicUrl))
            {
                _blobPublicAbsoluteUrl = new Uri(options.Value.SiteSettings.BlobPublicUrl);
            }
            else
            {
                _blobPublicAbsoluteUrl = new Uri(options.Value.SiteSettings.PublisherUrl.TrimEnd('/') + "/" + options.Value.SiteSettings.BlobPublicUrl.TrimStart('/'));
            }

            _viewRenderService = viewRenderService;
        }

        /// <summary>
        ///     Disposes of resources for this controller.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        #region LIST METHODS

        /// <summary>
        /// Catalog of web pages on this website.
        /// </summary>
        /// <param name="sortOrder"></param>
        /// <param name="currentSort"></param>
        /// <param name="pageNo"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<IActionResult> Index(string sortOrder, string currentSort, int pageNo = 0, int pageSize = 10)
        {
            ViewData["PublisherUrl"] = _options.Value.SiteSettings.PublisherUrl;
            ViewData["ShowFirstPageBtn"] = await _dbContext.Articles.CosmosAnyAsync() == false;
            
            ViewData["sortOrder"] = sortOrder;
            ViewData["currentSort"] = currentSort;
            ViewData["pageNo"] = pageNo;
            ViewData["pageSize"] = pageSize;

            var query = _dbContext.ArticleCatalog.AsQueryable();

            ViewData["RowCount"] = await query.CountAsync();

            if (sortOrder == "desc")
            {
                if (!string.IsNullOrEmpty(currentSort))
                {
                    switch (currentSort)
                    {
                        case "ArticleNumber":
                            query = query.OrderByDescending(o => o.ArticleNumber);
                            break;
                        case "Title":
                            query = query.OrderByDescending(o => o.Title);
                            break;
                        case "LastPublished":
                            query = query.OrderByDescending(o => o.Published);
                            break;
                        case "UrlPath":
                            query = query.OrderByDescending(o => o.UrlPath);
                            break;
                        case "Status":
                            query = query.OrderByDescending(o => o.Status);
                            break;
                        case "Updated":
                            query = query.OrderByDescending(o => o.Updated);
                            break;
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(currentSort))
                {
                    switch (currentSort)
                    {
                        case "ArticleNumber":
                            query = query.OrderBy(o => o.ArticleNumber);
                            break;
                        case "Title":
                            query = query.OrderBy(o => o.Title);
                            break;
                        case "LastPublished":
                            query = query.OrderBy(o => o.Published);
                            break;
                        case "UrlPath":
                            query = query.OrderBy(o => o.UrlPath);
                            break;
                        case "Status":
                            query = query.OrderBy(o => o.Status);
                            break;
                        case "Updated":
                            query = query.OrderBy(o => o.Updated);
                            break;
                    }
                }
            }

            var model = query.Select(s => new ArticleListItem()
            {
                ArticleNumber = s.ArticleNumber,
                Title = s.Title,
                IsDefault = s.UrlPath == "root",
                LastPublished = s.Published,
                UrlPath = s.UrlPath,
                Status = s.Status,
                Updated = s.Updated
            }).Skip(pageNo * pageSize).Take(pageSize);

            return View(await model.ToListAsync());
        }

        ///<summary>
        ///     Gets all the versions for an article
        /// </summary>
        /// <param name="id">Article number</param>
        /// <param name="sortOrder"></param>
        /// <param name="currentSort"></param>
        /// <param name="pageNo"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<IActionResult> Versions(int? id, string sortOrder = "desc", string currentSort = "VersionNumber", int pageNo = 0, int pageSize = 10)
        {
            if (id == null)
                return RedirectToAction("Index");

            ViewData["sortOrder"] = sortOrder;
            ViewData["currentSort"] = currentSort;
            ViewData["pageNo"] = pageNo;
            ViewData["pageSize"] = pageSize;

            var query = _dbContext.Articles.Where(w => w.ArticleNumber == id)
                .Select(s => new ArticleVersionViewModel()
                {
                    Id = s.Id,
                    Published = s.Published,
                    Title = s.Title,
                    Updated = s.Updated,
                    VersionNumber = s.VersionNumber,
                    Expires = s.Expires,
                    UsesHtmlEditor = s.Content.ToLower().Contains(" editable=") || s.Content.ToLower().Contains(" data-ccms-ceid=")
                }).AsQueryable();

            ViewData["RowCount"] = await query.CountAsync();


            if (sortOrder == "desc")
            {
                if (!string.IsNullOrEmpty(currentSort))
                {
                    switch (currentSort)
                    {
                        case "Published":
                            query = query.OrderByDescending(o => o.Published);
                            break;
                        case "Updated":
                            query = query.OrderByDescending(o => o.Updated);
                            break;
                        case "VersionNumber":
                            query = query.OrderByDescending(o => o.VersionNumber);
                            break;
                        case "Expires":
                            query = query.OrderByDescending(o => o.Expires);
                            break;
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(currentSort))
                {
                    switch (currentSort)
                    {
                        case "Published":
                            query = query.OrderBy(o => o.Published);
                            break;
                        case "Updated":
                            query = query.OrderBy(o => o.Updated);
                            break;
                        case "VersionNumber":
                            query = query.OrderBy(o => o.VersionNumber);
                            break;
                        case "Expires":
                            query = query.OrderBy(o => o.Expires);
                            break;
                    }
                }
            }

            var article = await _dbContext.Pages.Where(a => a.ArticleNumber == id.Value)
                .Select(s => new { s.Title, s.VersionNumber }).FirstOrDefaultAsync();

            ViewData["ArticleTitle"] = article.Title;
            ViewData["ArticleId"] = id.Value;

            return View(await query.ToListAsync());
        }

        /// <summary>
        /// Open trash
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors, Authors")]
        public async Task<IActionResult> Trash(string sortOrder, string currentSort, int pageNo = 0, int pageSize = 10)
        {
            ViewData["sortOrder"] = sortOrder;
            ViewData["currentSort"] = currentSort;
            ViewData["pageNo"] = pageNo;
            ViewData["pageSize"] = pageSize;

            var data = await _articleLogic.GetArticleTrashList();
            var query = data.AsQueryable();

            ViewData["RowCount"] = query.Count();


            if (sortOrder == "desc")
            {
                if (!string.IsNullOrEmpty(currentSort))
                {
                    switch (currentSort)
                    {
                        case "ArticleNumber":
                            query = query.OrderByDescending(o => o.ArticleNumber);
                            break;
                        case "Title":
                            query = query.OrderByDescending(o => o.Title);
                            break;
                        case "LastPublished":
                            query = query.OrderByDescending(o => o.LastPublished);
                            break;
                        case "UrlPath":
                            query = query.OrderByDescending(o => o.UrlPath);
                            break;
                        case "Status":
                            query = query.OrderByDescending(o => o.Status);
                            break;
                        case "Updated":
                            query = query.OrderByDescending(o => o.Updated);
                            break;
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(currentSort))
                {
                    switch (currentSort)
                    {
                        case "ArticleNumber":
                            query = query.OrderBy(o => o.ArticleNumber);
                            break;
                        case "Title":
                            query = query.OrderBy(o => o.Title);
                            break;
                        case "LastPublished":
                            query = query.OrderBy(o => o.LastPublished);
                            break;
                        case "UrlPath":
                            query = query.OrderBy(o => o.UrlPath);
                            break;
                        case "Status":
                            query = query.OrderBy(o => o.Status);
                            break;
                        case "Updated":
                            query = query.OrderBy(o => o.Updated);
                            break;
                    }
                }
            }
            
            return View(query.ToList());
        }
        #endregion

        /// <summary>
        /// Compare two versions.
        /// </summary>
        /// <param name="leftId"></param>
        /// <param name="rightId"></param>
        /// <returns></returns>
        public async Task<IActionResult> Compare(Guid leftId, Guid rightId)
        {
            
            var left = await _articleLogic.Get(leftId, EnumControllerName.Edit, await GetUserId());
            var right = await _articleLogic.Get(rightId, EnumControllerName.Edit, await GetUserId());
            @ViewData["PageTitle"] = left.Title;

            ViewData["LeftVersion"] = left.VersionNumber;
            ViewData["RightVersion"] = right.VersionNumber;

            var model = new CompareCodeViewModel()
            {
                EditorTitle = left.Title,
                EditorFields = new[]
                {
                    new EditorField
                    {
                        FieldId = "HeadJavaScript",
                        FieldName = "Head Block",
                        EditorMode = EditorMode.Html,
                        IconUrl = "/images/seti-ui/icons/html.svg",
                        ToolTip = "Content to appear at the bottom of the <head> tag."
                    },
                    new EditorField
                    {
                        FieldId = "Content",
                        FieldName = "Html Content",
                        EditorMode = EditorMode.Html,
                        IconUrl = "~/images/seti-ui/icons/html.svg",
                        ToolTip = "Content to appear in the <body>."
                    },
                    new EditorField
                    {
                        FieldId = "FooterJavaScript",
                        FieldName = "Footer Block",
                        EditorMode = EditorMode.Html,
                        IconUrl = "~/images/seti-ui/icons/html.svg",
                        ToolTip = "Content to appear at the bottom of the <body> tag."
                    }
                },
                Articles = new ArticleViewModel[] { left, right }
            };
            return View(model);
        }

        /// <summary>
        /// Gets template page information.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> GetTemplateInfo(Guid? Id)
        {
            if (Id == null)
                return Json("");

            var model = await _dbContext.Templates.FirstOrDefaultAsync(f => f.Id == Id.Value);

            return Json(model);
        }


        /// <summary>
        ///     Creates a <see cref="CreatePageViewModel" /> used to create a new article.
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> Create()
        {
            var layout = await _dbContext.Layouts.FirstOrDefaultAsync(l => l.IsDefault);

            var templates = await _dbContext.Templates.Where(t => t.LayoutId == layout.Id).Select(s =>
                        new SelectListItem
                        {
                            Value = s.Id.ToString(),
                            Text = s.Title
                        }).ToListAsync();

            if (User.IsInRole("Team Members"))
            {
                return View(new CreatePageViewModel
                {
                    Id = Guid.NewGuid(),
                    Title = string.Empty,
                    Templates = templates
                });
            }

            ViewData["Teams"] = null;
            return View(new CreatePageViewModel
            {
                Id = Guid.NewGuid(),
                Title = string.Empty,
                Templates = templates
            });
        }

        /// <summary>
        ///     Uses <see cref="ArticleEditLogic.Create(string, string, Guid?)" /> to create an <see cref="ArticleViewModel" /> that is
        ///     saved to
        ///     the database with <see cref="ArticleEditLogic.UpdateOrInsert(ArticleViewModel, string, bool)" /> ready for editing.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        [HttpPost]
        public async Task<IActionResult> Create(CreatePageViewModel model)
        {
            if (model == null) return NotFound();

            var validTitle = await _articleLogic.ValidateTitle(model.Title, null);

            if (!validTitle)
            {
                ModelState.AddModelError("Title", $"Title: {model.Title} conflicts with another article title or reserved word.");
                return View(model);
            }

            var article = await _articleLogic.Create(model.Title, await GetUserId(), model.TemplateId);

            return RedirectToAction("Edit", new { article.Id });
        }

        /// <summary>
        ///     Creates a new version for an article and redirects to editor.
        /// </summary>
        /// <param name="id">Article ID</param>
        /// <param name="entityId">Entity Id to use as new version</param>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors, Authors")]
        public async Task<IActionResult> CreateVersion(int id, Guid? entityId = null)
        {
            IQueryable<Article> query;

            //
            // Are we basing this on an existing entity?
            //
            if (entityId == null)
            {
                //
                // If here, we are not. Clone the new version from the last version.
                //
                // Find the last version here
                var maxVersion = await _dbContext.Articles.Where(a => a.ArticleNumber == id)
                    .MaxAsync(m => m.VersionNumber);

                //
                // Now find that version.
                //
                query = _dbContext.Articles.Where(f =>
                    f.ArticleNumber == id &&
                    f.VersionNumber == maxVersion);
            }
            else
            {
                //
                // We are here because the new version is being based on a
                // specific older version, not the latest version.
                //
                //
                // Create a new version based on a specific version
                //
                query = _dbContext.Articles.Where(f =>
                    f.Id == entityId.Value);
            }

            var article = await query.FirstOrDefaultAsync();

            // var defaultLayout = await ArticleLogic.GetDefaultLayout("en-US");
            var model = new ArticleViewModel
            {
                Id = article.Id, // This is the article we are going to clone as a new version.
                StatusCode = StatusCodeEnum.Active,
                ArticleNumber = article.ArticleNumber,
                UrlPath = article.UrlPath,
                VersionNumber = 0,
                Published = null,
                Title = article.Title,
                Content = article.Content,
                Updated = DateTime.UtcNow,
                HeadJavaScript = article.HeaderJavaScript,
                FooterJavaScript = article.FooterJavaScript,
                ReadWriteMode = false,
                PreviewMode = false,
                EditModeOn = false,
                CacheKey = null,
                CacheDuration = 0
            };

            var userId = _userManager.GetUserId(User);

            var result = await _articleLogic.UpdateOrInsert(model, userId, true);

            return RedirectToAction("EditCode", "Editor", new { result.Model.Id });
        }

        /// <summary>
        /// Create a duplicate page from a specified page.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Duplicate(Guid id)
        {
            var articleViewModel = await _articleLogic.Get(id, EnumControllerName.Edit, await GetUserId());

            ViewData["Original"] = articleViewModel;

            if (articleViewModel == null)
            {
                return NotFound();
            }

            return View(new DuplicateViewModel()
            {
                Id = articleViewModel.Id,
                Published = articleViewModel.Published,
                Title = articleViewModel.Title
            });
        }

        /// <summary>
        /// Creates a duplicate page from a specified page and version.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrators, Editors, Authors")]
        public async Task<IActionResult> Duplicate(DuplicateViewModel model)
        {
            string title = "";

            if (string.IsNullOrEmpty(model.ParentPageTitle))
            {
                title = model.Title;
            }
            else
            {
                title = $"{model.ParentPageTitle.Trim('/')}/{model.Title.Trim('/')} ";
            }

            if (await _dbContext.Articles.Where(a => a.Title.ToLower() == title.ToLower()).CosmosAnyAsync())
            {
                if (string.IsNullOrEmpty(model.ParentPageTitle))
                {
                    ModelState.AddModelError("Title", "Page title already taken.");
                }
                else
                {
                    ModelState.AddModelError("Title", "Sub-page title already taken.");
                }
            }

            var userId = await GetUserId();

            var articleViewModel = await _articleLogic.Get(model.Id, EnumControllerName.Edit, userId);

            if (ModelState.IsValid)
            {
                articleViewModel.ArticleNumber = 0;
                articleViewModel.Id = Guid.NewGuid();
                articleViewModel.Published = model.Published;
                articleViewModel.Title = title;

                try
                {
                    var result = await _articleLogic.UpdateOrInsert(articleViewModel, userId, true);

                    return RedirectToAction("Edit", new { result.Model.Id });
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("", e.Message);
                }
            }

            ViewData["Original"] = articleViewModel;

            return View(model);
        }

        /// <summary>
        ///     Creates a <see cref="CreatePageViewModel" /> used to create a new article.
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> NewHome(int id)
        {
            var page = await _dbContext.Articles.FirstOrDefaultAsync(f => f.ArticleNumber == id);
            return View(new NewHomeViewModel
            {
                Id = page.Id,
                ArticleNumber = page.ArticleNumber,
                Title = page.Title,
                IsNewHomePage = false,
                UrlPath = page.UrlPath
            });
        }

        /// <summary>
        /// Make a web page the new home page
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> NewHome(NewHomeViewModel model)
        {
            if (model == null) return NotFound();
            await _articleLogic.NewHomePage(model.Id, _userManager.GetUserId(User));

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Recovers an article from trash
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors, Authors")]
        public async Task<IActionResult> Recover(int Id)
        {
            await _articleLogic.RetrieveFromTrash(Id, await GetUserId());

            return RedirectToAction("Trash");
        }

        /// <summary>
        ///     Publishes a website.
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors")]
        public IActionResult Publish()
        {
            return View();
        }
                
        /// <summary>
        /// Open Cosmos CMS logs
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> Logs()
        {
            var data = await _dbContext.ArticleLogs
                .OrderByDescending(o => o.DateTimeStamp)
                .Select(s => new
                {
                    s.Id,
                    s.ActivityNotes,
                    s.DateTimeStamp,
                    s.IdentityUserId
                }).ToListAsync();

            var model = data.Select(s => new ArticleLogJsonModel
            {
                Id = s.Id,
                ActivityNotes = s.ActivityNotes,
                DateTimeStamp = s.DateTimeStamp.ToUniversalTime(),
                IdentityUserId = s.IdentityUserId
            }).AsQueryable();

            return View(model);
        }

        ///// <summary>
        ///// Read logs
        ///// </summary>
        ///// <param name="request"></param>
        ///// <returns></returns>
        //[Authorize(Roles = "Administrators, Editors")]
        //public async Task<IActionResult> Read_Logs([DataSourceRequest] DataSourceRequest request)
        //{
        //    var data = await _dbContext.ArticleLogs
        //        .OrderByDescending(o => o.DateTimeStamp)
        //        .Select(s => new
        //        {
        //            s.Id,
        //            s.ActivityNotes,
        //            s.DateTimeStamp,
        //            s.IdentityUserId
        //        }).ToListAsync();

        //    var result = await data.Select(s => new ArticleLogJsonModel
        //    {
        //        Id = s.Id,
        //        ActivityNotes = s.ActivityNotes,
        //        DateTimeStamp = s.DateTimeStamp.ToUniversalTime(),
        //        IdentityUserId = s.IdentityUserId
        //    }).ToDataSourceResultAsync(request);
        //    return Json(result);
        //}

        #region SAVING CONTENT METHODS

        #endregion

        #region EDIT ARTICLE FUNCTIONS


        /// <summary>
        /// Editor page
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public async Task<IActionResult> CcmsContent(Guid Id)
        {
            var article = await _articleLogic.Get(Id, EnumControllerName.Edit, await GetUserId());

            return View(article);
        }

        #region HTML AND CODE EDITOR METHODS

        /// <summary>
        ///     Gets an article to edit by ID for the HTML (WYSIWYG) Editor.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                // Web browser may ask for favicon.ico, so if the ID is not a number, just skip the response.
                if (Guid.TryParse(id, out var pageId))
                {
                    ViewData["BlobEndpointUrl"] = _options.Value.SiteSettings.BlobPublicUrl;

                    //
                    // Get an article, or a template based on the controller name.
                    //
                    var model = await _articleLogic.Get(pageId, EnumControllerName.Edit, await GetUserId());
                    ViewData["LastPubDateTime"] = await GetLastPublishingDate(model.ArticleNumber);

                    ViewData["PageTitle"] = model.Title;
                    ViewData["Published"] = model.Published;

                    // Override defaults
                    model.EditModeOn = true;

                    // Authors cannot edit published articles
                    if (model.Published.HasValue && User.IsInRole("Authors"))
                        return Unauthorized();

                    return View(new HtmlEditorViewModel(model));

                }

                return NotFound();
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                throw;
            }
        }

        /// <summary>
        ///     Saves an article meta data via HTTP POSTand returns JSON results.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <remarks>
        ///     Does not save article HTML. That is saved with <see cref="PostRegions"/>.
        /// </remarks>
        [HttpPost]
        public async Task<IActionResult> Edit(HtmlEditorViewModel model)
        {
            if (model == null) return NotFound();

            //
            // The HTML editor edits the title and Content fields.
            // Next two lines detect any HTML errors with each.
            // Errors are saved in ModelState.
            model.Title = BaseValidateHtml("Title", model.Title);
            //model.Content = BaseValidateHtml("Content", model.Content);

            // Next pull the original.
            var original = await _articleLogic.Get(model.Id, EnumControllerName.Edit, await GetUserId());
                        
            // Make sure model state is valid
            if (ModelState.IsValid)
            {
                // Get the user's ID for logging.
                var userId = await GetUserId();

                // START SAVE TO DATABASE ********
                //
                // Now save the changes to the database here.
                //
                var result = await _articleLogic.UpdateOrInsert(model, userId, model.SaveAsNewVersion);

                //
                // Echo back the changes made.
                //
                model.RoleList = result.Model.RoleList;
                model.UrlPath = result.Model.UrlPath;
                model.ArticleNumber = result.Model.ArticleNumber;
                model.VersionNumber = result.Model.VersionNumber;
                model.Id = result.Model.Id;
                model.Title = result.Model.Title;
                model.Published = result.Model.Published;



                //
                // END  SAVE TO  DATABASE ********

                // Re-enable editable sections.
                //model.Content = model.Content.Replace("crx=", "contenteditable=",
                //    StringComparison.CurrentCultureIgnoreCase);

                //
                // Flush Redis and CDN if required 
                // New: Delay CDN 10 seconds to allow for local memory cache(s) to drain
                //
                if (result.Urls.Any() &&
                    (_options.Value?.CdnConfig.AzureCdnConfig.ClientId != null) || _options.Value?.CdnConfig.AkamaiContextConfig.AccessToken != null)
                {

                    // Now get all the languages that were flushed
                    // TODO: This needs to be improved.
                    // Not sure this is the best way to do this.
                    var paths = new List<string>();

                    paths.Add($"/{model.UrlPath}");
                    // Add the primary path
                    foreach (var url in result.Urls)
                    {
                        paths.Add($"/{url.TrimStart('/')}");
                    }

                }

            }

            var errors = ModelState.Values
                .Where(w => w.ValidationState == ModelValidationState.Invalid)
                .ToList();

            var data = new SaveResultJsonModel
            {
                IsValid = ModelState.IsValid,
                ErrorCount = ModelState.ErrorCount,
                HasReachedMaxErrors = ModelState.HasReachedMaxErrors,
                ValidationState = ModelState.ValidationState,
                Model = model,
                Errors = errors
            };

            return Json(data);
        }

        /// <summary>
        /// Updates editable regions
        /// </summary>
        /// <param name="model"></param>
        /// <remarks>FromBody is used because the jQuery call puts the JSON in the body, not the "Form" as this is a JSON content type.</remarks>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> PostRegions([FromBody]HtmlEditorPost model)
        {
            // Next pull the original.
            var article = await _articleLogic.Get(model.Id, EnumControllerName.Edit, await GetUserId());

            // Get the editable regions from the original document.
            var originalHtmlDoc = new HtmlDocument();
            originalHtmlDoc.LoadHtml(article.Content);
            var originalEditableDivs = originalHtmlDoc.DocumentNode.SelectNodes("//*/div[@data-ccms-ceid]");

            foreach (var region in model.Regions)
            {
                var target = originalEditableDivs.FirstOrDefault(w => w.Attributes["data-ccms-ceid"].Value == region.Id);
                if (target != null)
                {
                    target.InnerHtml = region.Html;
                }
            }

            // Now carry over what's beein updated to the original.
            article.Content = originalHtmlDoc.DocumentNode.OuterHtml;

            _ = await _articleLogic.UpdateOrInsert(article, await GetUserId(), false);

            return Ok();
        }

        /// <summary>
        /// Edit web page code with Monaco editor.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> EditCode(Guid id)
        {
            var article = await _articleLogic.Get(id, EnumControllerName.Edit, await GetUserId());
            if (article == null) return NotFound();

            // Validate security for authors before going further
            if (article.Published.HasValue && User.IsInRole("Authors"))
                return Unauthorized();

            ViewData["Version"] = article.VersionNumber;

            ViewData["PageTitle"] = article.Title;
            ViewData["Published"] = article.Published;
            ViewData["LastPubDateTime"] = await GetLastPublishingDate(article.ArticleNumber);

            return View(new EditCodePostModel
            {
                Id = article.Id,
                ArticleNumber = article.ArticleNumber,
                Title = article.Title,
                Published = article.Published,
                RoleList = article.RoleList,
                EditorTitle = article.Title,
                EditorFields = new[]
                {
                    new EditorField
                    {
                        FieldId = "HeadJavaScript",
                        FieldName = "Head Block",
                        EditorMode = EditorMode.Html,
                        IconUrl = "/images/seti-ui/icons/html.svg",
                        ToolTip = "Content to appear at the bottom of the <head> tag."
                    },
                    new EditorField
                    {
                        FieldId = "Content",
                        FieldName = "Html Content",
                        EditorMode = EditorMode.Html,
                        IconUrl = "~/images/seti-ui/icons/html.svg",
                        ToolTip = "Content to appear in the <body>."
                    },
                    new EditorField
                    {
                        FieldId = "FooterJavaScript",
                        FieldName = "Footer Block",
                        EditorMode = EditorMode.Html,
                        IconUrl = "~/images/seti-ui/icons/html.svg",
                        ToolTip = "Content to appear at the bottom of the <body> tag."
                    }
                },
                HeadJavaScript = article.HeadJavaScript,
                FooterJavaScript = article.FooterJavaScript,
                Content = article.Content,
                EditingField = "HeadJavaScript",
                CustomButtons = new[] { "Preview", "Html", "Export", "Import" }
            });
        }

        /// <summary>
        ///     Saves the code and html of the page.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <remarks>
        ///     This method saves page code to the database.  <see cref="EditCodePostModel.Content" /> is validated using method
        ///     <see cref="BaseController.BaseValidateHtml" />.
        ///     HTML formatting errors that could not be automatically fixed are logged with
        ///     <see cref="ControllerBase.ModelState" /> and
        ///     the code is not saved in the database.
        /// </remarks>
        /// <exception cref="NotFoundResult"></exception>
        /// <exception cref="UnauthorizedResult"></exception>
        [HttpPost]
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> EditCode(EditCodePostModel model)
        {
            if (model == null) return NotFound();

            // Get the user's ID for logging.
            var userId = await GetUserId();

            var article = await _dbContext.Articles.FirstOrDefaultAsync(f => f.Id == model.Id);

            if (article == null) return NotFound();

            _dbContext.Entry(article).State = EntityState.Detached;

            var jsonModel = new SaveCodeResultJsonModel();

            try
            {
                var result = await _articleLogic.UpdateOrInsert(new ArticleViewModel()
                {
                    Id = model.Id,
                    ArticleNumber = article.ArticleNumber,
                    Content = model.Content,
                    Title = model.Title,
                    RoleList = model.RoleList,
                    Published = model.Published,
                    Expires = article.Expires,
                    FooterJavaScript = model.FooterJavaScript,
                    HeadJavaScript = model.HeadJavaScript,
                    StatusCode = (StatusCodeEnum)article.StatusCode,
                    Updated = DateTimeOffset.Now,
                    UrlPath = article.UrlPath,
                    VersionNumber = article.VersionNumber
                }, userId, model.SaveAsNewVersion);

                jsonModel.Model = new EditCodePostModel()
                {
                    ArticleNumber = result.Model.ArticleNumber,
                    Content = result.Model.Content,
                    EditingField = model.EditingField,
                    CustomButtons = model.CustomButtons,
                    EditorMode = model.EditorMode,
                    EditorFields = model.EditorFields,
                    EditorTitle = model.EditorTitle,
                    EditorType = model.EditorType,
                    FooterJavaScript = result.Model.FooterJavaScript,
                    HeadJavaScript = result.Model.HeadJavaScript,
                    Id = result.Model.Id,
                    Published = result.Model.Published,
                    RoleList = result.Model.RoleList,
                    Title = result.Model.Title
                };

            }
            catch (Exception e)
            {
                var provider = new EmptyModelMetadataProvider();
                ModelState.AddModelError("Save", e, provider.GetMetadataForType(typeof(string)));
            }

            ViewData["Version"] = article.VersionNumber;

            //
            jsonModel.ErrorCount = ModelState.ErrorCount;
            jsonModel.IsValid = ModelState.IsValid;

            jsonModel.Errors.AddRange(ModelState.Values
                .Where(w => w.ValidationState == ModelValidationState.Invalid)
                .ToList());
            jsonModel.ValidationState = ModelState.ValidationState;

            return Json(jsonModel);
        }

        /// <summary>
        /// Gets the last date this article was published.
        /// </summary>
        /// <param name="articleNumber"></param>
        /// <returns></returns>
        private async Task<DateTimeOffset?> GetLastPublishingDate(int articleNumber)
        {
            return await _dbContext.Articles.Where(a => a.ArticleNumber == articleNumber).MaxAsync(m => m.Published);
        }

        #endregion

        /// <summary>
        /// Exports a page as a file
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
        public async Task<IActionResult> ExportPage(Guid? id)
        {
            ArticleViewModel article;
            var userId = await GetUserId();
            if (id.HasValue)
            {
                article = await _articleLogic.Get(id.Value, EnumControllerName.Edit, userId);
            }
            else
            {
                // Get the user's ID for logging.
                article = await _articleLogic.Create("Blank Page", userId);
            }

            var html = await _articleLogic.ExportArticle(article, _blobPublicAbsoluteUrl, _viewRenderService);

            var exportName = $"pageid-{article.ArticleNumber}-version-{article.VersionNumber}.html";

            var bytes = Encoding.UTF8.GetBytes(html);

            return File(bytes, "application/octet-stream", exportName);
        }

        /// <summary>
        /// Pre-load the website (useful if CDN configured).
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Authorize(Roles = "Administrators")]
        public IActionResult Preload()
        {
            return View(new PreloadViewModel());
        }

        ///// <summary>
        ///// Execute preload.
        ///// </summary>
        ///// <param name="model"></param>
        ///// <param name="primaryOnly"></param>
        ///// <returns></returns>
        //[HttpPost]
        //[Authorize(Roles = "Administrators")]
        //public async Task<IActionResult> Preload(PreloadViewModel model, bool primaryOnly = false)
        //{
        //    var activeCode = (int)StatusCodeEnum.Active;
        //    var query = _dbContext.Articles.Where(p => p.Published != null && p.StatusCode == activeCode);
        //    var articleList = await _articleLogic.GetArticleList(query);
        //    var publicUrl = _options.Value.SiteSettings.PublisherUrl.TrimEnd('/');

        //    model.PageCount = 0;

        //    var client = new HttpClient();

        //    // Get a list of editors that are outside the current cloud.
        //    var otherEditors = _options.Value.EditorUrls.Where(w => w.CloudName.Equals(_options.Value.PrimaryCloud, StringComparison.CurrentCultureIgnoreCase) == false).ToList();

        //    model.EditorCount++;

        //    //
        //    // If we are preloading CDN
        //    if (model.PreloadCdn)
        //    {
        //        foreach (var article in articleList)
        //        {
        //            try
        //            {
        //                var response = await client.GetAsync($"{publicUrl}/{(article.UrlPath == "root" ? "" : article.UrlPath)}");
        //                response.EnsureSuccessStatusCode();
        //                _ = await response.Content.ReadAsStringAsync();
        //                //model.PageCount++;
        //            }
        //            catch (Exception e)
        //            {
        //                _logger.LogError(e.Message, e);
        //            }
        //        }
        //    }


        //    return View(model);
        //}

        #endregion

        #region Data Services

        /// <summary>
        /// Check to see if a page title is already taken.
        /// </summary>
        /// <param name="articleNumber"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> CheckTitle(int articleNumber, string title)
        {
            var result = await _articleLogic.ValidateTitle(title, articleNumber);

            if (result) return Json(true);

            return Json($"Title '{title}' is already taken.");
        }

        /// <summary>
        /// Gets a list of articles (web pages)
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetArticleList()
        {
            var model = _dbContext.ArticleCatalog.Select(s => new ArticleListItem()
            {
                ArticleNumber = s.ArticleNumber,
                Title = s.Title,
                IsDefault = s.UrlPath == "root",
                LastPublished = s.Published,
                UrlPath = s.UrlPath,
                Status = s.Status,
                Updated = s.Updated
            }).OrderBy(o => o.Title);
            return Json(await model.ToListAsync());
        }
                
        /// <summary>
        /// Gets a list of articles (pages) on this website.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        /// <remarks>Returns published and non-published links</remarks>
        public async Task<IActionResult> List_Articles(string text)
        {
            IQueryable<Article> query = _dbContext.Articles
            .OrderBy(o => o.Title)
            .Where(w => w.StatusCode == (int)StatusCodeEnum.Active || w.StatusCode == (int)StatusCodeEnum.Inactive);

            if (!string.IsNullOrEmpty(text))
            {
                query = query.Where(x => x.Title.ToLower().Contains(text.ToLower()));
            }

            var model = await query.Select(s => new
            {
                s.Title,
                s.UrlPath
            }).Distinct().Take(10).ToListAsync();

            return Json(model);
        }

        /// <summary>
        /// Sends an article (or page) to trash bin.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> TrashArticle(int Id)
        {
            await _articleLogic.TrashArticle(Id);
            return RedirectToAction("Index", "Editor");
        }

        /// <summary>
        ///     Gets a role list, and allows for filtering
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Get_RoleList(string text)
        {
            var query = _dbContext.Roles.Select(s => new RoleItemViewModel
            {
                Id = s.Id,
                RoleName = s.Name,
                RoleNormalizedName = s.NormalizedName
            });

            if (!string.IsNullOrEmpty(text)) query = query.Where(w => w.RoleName.StartsWith(text));

            return Json(await query.OrderBy(r => r.RoleName).ToListAsync());
        }

        #region REDIRECT MANAGEMENT

        /// <summary>
        /// Redirect manager page
        /// </summary>
        /// <param name="sortOrder"></param>
        /// <param name="currentSort"></param>
        /// <param name="pageNo"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> Redirects(string sortOrder, string currentSort, int pageNo = 0, int pageSize = 10)
        {

            ViewData["sortOrder"] = sortOrder;
            ViewData["currentSort"] = currentSort;
            ViewData["pageNo"] = pageNo;
            ViewData["pageSize"] = pageSize;

            var query = _articleLogic.GetArticleRedirects();

            ViewData["RowCount"] = await query.CountAsync();

            if (sortOrder == "desc")
            {
                if (!string.IsNullOrEmpty(currentSort))
                {
                    switch (currentSort)
                    {
                        case "FromUrl":
                            query = query.OrderByDescending(o => o.FromUrl);
                            break;
                        case "Title":
                            query = query.OrderByDescending(o => o.Id);
                            break;
                        case "ToUrl":
                            query = query.OrderByDescending(o => o.ToUrl);
                            break;
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(currentSort))
                {
                    switch (currentSort)
                    {
                        case "FromUrl":
                            query = query.OrderBy(o => o.FromUrl);
                            break;
                        case "Id":
                            query = query.OrderBy(o => o.Id);
                            break;
                        case "ToUrl":
                            query = query.OrderBy(o => o.ToUrl);
                            break;
                    }
                }
            }

            var model = await query.Skip(pageNo * pageSize).Take(pageSize).ToListAsync();

            return View(model);
        }

        /// <summary>
        /// Sends an article (or page) to trash bin.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> RedirectDelete(Guid Id)
        {
            var article = await _dbContext.Articles.FirstOrDefaultAsync(f => f.Id == Id);

            await _articleLogic.TrashArticle(article.ArticleNumber);

            return RedirectToAction("Redirects");
        }

        /// <summary>
        /// Updates a redirect
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="FromUrl"></param>
        /// <param name="ToUrl"></param>
        /// <returns></returns>
        [Authorize(Roles = "Administrators, Editors")]
        public async Task<IActionResult> RedirectEdit([FromForm] Guid Id, string FromUrl, string ToUrl)
        {
            var redirect = await _dbContext.Articles.FirstOrDefaultAsync(f => f.Id == Id && f.StatusCode == (int)StatusCodeEnum.Redirect);
            if (redirect == null)
                return NotFound();

            redirect.UrlPath = FromUrl;
            redirect.Content = ToUrl;

            await _dbContext.SaveChangesAsync();

            return RedirectToAction("Redirects");
        }

        #endregion

        #endregion

        /// <summary>
        /// Receives an encrypted signal from another editor to do something.
        /// </summary>
        /// <param name="data">Encrypted arguments</param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public string Signal(string data)
        {
            var result = new SignalResult();

            try
            {
                var args = DecryptString(data).Split('|');

                result.JsonValue = args[0] switch
                {
                    "VERIFY" => JsonConvert.SerializeObject(new SignalVerifyResult { Echo = args[1], Stamp = DateTime.UtcNow }),
                    _ => throw new Exception($"Signal {args[0]} not supported."),
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                result.Exceptions.Add(e);
            }

            result.HasErrors = result.Exceptions.Any();

            var json = JsonConvert.SerializeObject(result);

            return EncryptString(json);
        }

    }
}
