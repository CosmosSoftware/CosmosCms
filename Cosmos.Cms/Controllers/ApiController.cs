using Jering.Javascript.NodeJS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using NuGet.ContentModel;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using Cosmos.Cms.Models;
using Cosmos.Cms.Common.Data;
using Cosmos.Cms.Common.Services.Configurations;
using Microsoft.Extensions.Options;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.Extensions.Logging;

namespace Cosmos.Cms.Controllers
{
    /// <summary>
    /// API Controller
    /// </summary>
    [AllowAnonymous]
    [Authorize(Roles = "Reviewers, Administrators, Editors, Authors")]
    public class ApiController : Controller
    {

        private readonly INodeJSService _nodeJSService;
        private readonly IOptions<CosmosConfig> _cosmosConfig;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ApiController> _logger;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="nodeJSService"></param>
        /// <param name="cosmosConfig"></param>
        /// <param name="dbContext"></param>
        /// <param name="logger"></param>
        public ApiController(INodeJSService nodeJSService, IOptions<CosmosConfig> cosmosConfig,
            ApplicationDbContext dbContext, ILogger logger)
        {
            _nodeJSService = nodeJSService;
            _cosmosConfig = cosmosConfig;
            _dbContext = dbContext;
            _logger = (ILogger<ApiController>)logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string id)
        {
            // Try to invoke from the NodeJS cache
            //(bool success, var result) = await _nodeJSService.TryInvokeFromCacheAsync<string>(id, args: new[] { "success" });

            // If the module hasn't been cached, cache it. If the NodeJS process dies and restarts, the cache will be invalidated, so always check whether success is false.
            var script = await _dbContext.NodeScripts.WithPartitionKey(id).OrderByDescending(o => o.Version).FirstOrDefaultAsync();
            // This is a trivialized example. In practice, to avoid holding large module strings in memory, you might retrieve the module 
            // string from an on-disk or remote source.
            // string moduleString = "module.exports = (callback, message) => callback(null, { message: message });";

            // Send the module string to NodeJS where it's compiled, invoked and cached.
            var result = await _nodeJSService.InvokeFromStringAsync<string>(script.Code, null, args: new[] { "success" });

            var apiResult = new ApiResult(result)
            {
                IsSuccess = true
            };

            return Json(apiResult);
        }

        /// <summary>
        /// Runs a script in debug mode
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> DebugGet(Guid id)
        {
            var script = await _dbContext.NodeScripts.FirstOrDefaultAsync(f => f.Id == id);
            return Json(await Run(script, true));
        }

        private async Task<ApiResult> Run(NodeScript script, bool debug = false)
        {
            try
            {
                var args = Request.Query.Where(w => script.InputVars.Contains(w.Key)).Select(s => (object)new ApiArg
                {
                    Key = s.Key,
                    Value = s.Value
                }).ToList();

                var result = await _nodeJSService.InvokeFromStringAsync<string>(script.Code, null, args: args.ToArray());
                return new ApiResult(result) { IsSuccess = true };
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);

                if (debug)
                    return new ApiResult(e.Message) { IsSuccess = false };

                return new ApiResult("Server error 500.") { IsSuccess = false };
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post(string id)
        {
            // Try to invoke from the NodeJS cache
            (bool success, var result) = await _nodeJSService.TryInvokeFromCacheAsync<string>(id, args: new[] { "success" });

            // If the module hasn't been cached, cache it. If the NodeJS process dies and restarts, the cache will be invalidated, so always check whether success is false.
            if (!success)
            {
                // This is a trivialized example. In practice, to avoid holding large module strings in memory, you might retrieve the module 
                // string from an on-disk or remote source.
                string moduleString = "module.exports = (callback, message) => callback(null, { message: message });";

                // Send the module string to NodeJS where it's compiled, invoked and cached.
                result = await _nodeJSService.InvokeFromStringAsync<string>(moduleString, id, args: new[] { "success" });
            }

            var apiResult = new ApiResult(result)
            {
                IsSuccess = true
            };

            return Json(apiResult);
        }

    }
}
