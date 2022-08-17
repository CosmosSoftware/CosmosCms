using Cosmos.Cms.Common.Data;
using Cosmos.Cms.Common.Data.Logic;
using Cosmos.Cms.Common.Services;
using Cosmos.Cms.Common.Services.Configurations;
using Cosmos.Cms.Website.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Cosmos.Cms.Website.ViewComponents
{
    public class ArticleComponent : ViewComponent
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ArticleComponent> _logger;
        private readonly IOptions<CosmosConfig> _cosmosOptions;

        public ArticleComponent(ApplicationDbContext dbContext, ILogger<ArticleComponent> logger, IOptions<CosmosConfig> cosmosOptions)
        {
            _dbContext = dbContext;
            _logger = logger;
            _cosmosOptions = cosmosOptions;
        }

        /// <summary>
        /// Internal method used to produce either a ViewResult or JsonResult
        /// </summary>
        /// <param name="id"></param>
        /// <param name="lang"></param>
        /// <param name="returnJson"></param>
        /// <param name="includeLayout"></param>
        /// <returns></returns>
        public async Task<IViewComponentResult> InvokeAsync(HomeIndexViewModel model)
        {
            try
            {
                // Make sure this is UrlEncoded, because this is the way it is stored in DB.
                if (!string.IsNullOrEmpty(model.UrlPath)) model.UrlPath = ArticleLogic.HandleUrlEncodeTitle(model.UrlPath);


                model.UrlPath = model.UrlPath?.Trim().ToLower();
                if (string.IsNullOrEmpty(model.UrlPath) || model.UrlPath.Trim() == "/")
                    model.UrlPath = "root";

                var articleLogic = new ArticleLogic(_dbContext, _cosmosOptions, false);

                var articleViewModel = await articleLogic.GetByUrl(model.UrlPath, model.Lang);

                if (articleViewModel == null)
                {
                    //
                    // Create your own not found page for a graceful page for users.
                    //
                    articleViewModel = await articleLogic.GetByUrl("not_found", model.Lang);

                    HttpContext.Response.StatusCode = 404;

                }

                if (articleViewModel == null)
                {
                    return View();
                }

                return View(articleViewModel);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                throw;
            }
        }
    }
}
