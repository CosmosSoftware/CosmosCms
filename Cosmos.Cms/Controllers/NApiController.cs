using Cosmos.BlobService.Config;
using Cosmos.Cms.Common.Data;
using Cosmos.Cms.Common.Models;
using Cosmos.Cms.Common.Services.Configurations;
using Cosmos.Cms.Models;
using Jering.Javascript.NodeJS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Cosmos.Cms.Controllers
{
    /// <summary>
    /// API Controller
    /// </summary>
    [AllowAnonymous]
    [Authorize(Roles = "Reviewers, Administrators, Editors, Authors")]
    public class NApiController : Controller
    {

        private readonly INodeJSService _nodeJSService;
        private readonly IOptions<CosmosConfig> _cosmosConfig;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<NApiController> _logger;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="nodeJSService"></param>
        /// <param name="cosmosConfig"></param>
        /// <param name="dbContext"></param>
        /// <param name="logger"></param>
        public NApiController(INodeJSService nodeJSService, IOptions<CosmosConfig> cosmosConfig,
            ApplicationDbContext dbContext, ILogger logger)
        {
            _nodeJSService = nodeJSService;
            _cosmosConfig = cosmosConfig;
            _dbContext = dbContext;
            _logger = (ILogger<NApiController>)logger;
        }

        /// <summary>
        /// API End Point
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Index(string id)
        {
            try
            {

                // Try to invoke from the NodeJS cache
                //(bool success, var result) = await _nodeJSService.TryInvokeFromCacheAsync<string>(id, args: new[] { "success" });

                // If the module hasn't been cached, cache it. If the NodeJS process dies and restarts, the cache will be invalidated, so always check whether success is false.
                NodeScript script;
                
                if (Guid.TryParse(id, out var gid))
                {
                    script = await _dbContext.NodeScripts.FirstOrDefaultAsync(f => f.Id == gid);
                }
                else
                {
                    script = await _dbContext.NodeScripts.WithPartitionKey(id).OrderByDescending(o => o.Version).FirstOrDefaultAsync();
                }

                var values = GetArgs(script);

                // Send the module string to NodeJS where it's compiled, invoked and cached.
                var result = await _nodeJSService.InvokeFromStringAsync<string>(script.Code, null, args: values);

                var apiResult = new ApiResult(result)
                {
                    IsSuccess = true
                };

                return Json(apiResult);

            }
            catch (Exception e)
            {
                var apiResult = new ApiResult($"Error: {id}.")
                {
                    IsSuccess = true
                };

                apiResult.Errors.Add(id, e.Message);

                return Json(apiResult);
            }

        }

        private ApiArgument[] GetArgs(NodeScript script)
        {
            if (Request.Method == "POST")
            {
                return Request.Form.Where(a => script.InputVars.Contains(a.Key))
                    .Select(s => new ApiArgument()
                    {
                        Key = s.Key,
                        Value = s.Value
                    }).ToArray();
            }
            else if (Request.Method == "GET")
            {
                return Request.Query.Where(a => script.InputVars.Contains(a.Key))
                    .Select(s => new ApiArgument()
                    {
                        Key = s.Key,
                        Value = s.Value
                    }).ToArray();
            }
            return null;
        }
    }
}
