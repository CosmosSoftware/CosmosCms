using Cosmos.Cms.Common.Data;
using Cosmos.Cms.Common.Services.Configurations;
using Cosmos.Cms.Models;
using Jering.Javascript.NodeJS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
            ApplicationDbContext dbContext, ILogger<ApiController> logger)
        {
            _nodeJSService = nodeJSService;
            _cosmosConfig = cosmosConfig;
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// API End Point
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Index(string Id)
        {
            try
            {

                if (string.IsNullOrEmpty(Id))
                {
                    return View();
                }

                // Try to invoke from the NodeJS cache
                //(bool success, var result) = await _nodeJSService.TryInvokeFromCacheAsync<string>(id, args: new[] { "success" });

                // If the module hasn't been cached, cache it. If the NodeJS process dies and restarts, the cache will be invalidated, so always check whether success is false.
                NodeScript script;

                if (Guid.TryParse(Id, out var gid))
                {
                    script = await _dbContext.NodeScripts.FirstOrDefaultAsync(f => f.Published != null && f.Published <= DateTimeOffset.UtcNow && f.Id == gid);
                }
                else
                {
                    script = await _dbContext.NodeScripts.WithPartitionKey(Id).Where(f => f.Published != null && f.Published <= DateTimeOffset.UtcNow).OrderByDescending(o => o.Version).FirstOrDefaultAsync();
                }

                var values = CodeController.GetArgs(Request, script);

                ApiResult apiResult;

                try
                {
                    // Send the module string to NodeJS where it's compiled, invoked and cached.
                    var result = await _nodeJSService.InvokeFromStringAsync<string>(script.Code, null, args: values);

                    apiResult = new ApiResult(result)
                    {
                        IsSuccess = true
                    };
                }
                catch (Exception e)
                {
                    apiResult = new ApiResult(e.Message)
                    {
                        IsSuccess = true
                    };
                }

                return Json(apiResult);

            }
            catch (Exception e)
            {
                var apiResult = new ApiResult($"Error: {Id}.")
                {
                    IsSuccess = true
                };

                apiResult.Errors.Add(Id, e.Message);

                return Json(apiResult);
            }

        }

        /// <summary>
        /// Returns the Open API (Swagger) definition for this API
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Specification()
        {
            var scripts = await _dbContext.NodeScripts.Where(w => w.Published != null && w.Published <= DateTimeOffset.UtcNow).OrderBy(o => o.EndPoint).ToListAsync();

            var paths = new OpenApiPaths();

            foreach (var script in scripts)
            {
                var parameters = new List<OpenApiParameter>();
                
                foreach (var p in script.InputVars)
                {
                    parameters.Add(new OpenApiParameter()
                    {
                        Name = p,
                        Schema = new OpenApiSchema()
                        {
                            Type = "string"
                        },
                        In = ParameterLocation.Header
                    });
                }

                paths.Add($"/Index/{script.EndPoint}", new OpenApiPathItem()
                {

                    Operations = new Dictionary<OperationType, OpenApiOperation>
                    {
                        [OperationType.Post] = new OpenApiOperation
                        {
                            Description = script.Description,
                            Responses = new OpenApiResponses
                            {
                                ["200"] = new OpenApiResponse
                                {
                                    Description = "OK"
                                }
                            },
                            Parameters = parameters
                        }
                    }
                }); ;
            }

            var document = new OpenApiDocument
            {
                Info = new OpenApiInfo
                {
                    Version = "1.0.0",
                    Title = "Swagger Petstore (Simple)",
                },
                Servers = new List<OpenApiServer>
                            {
                                new OpenApiServer { Url = "/api" }
                            },
                Paths = paths
            };


            using var outputString = new StringWriter();

            var writer = new OpenApiJsonWriter(outputString);
            document.SerializeAsV3(writer);

            var model = JsonConvert.DeserializeObject(outputString.ToString());

            return Json(model);
        }

    }
}
