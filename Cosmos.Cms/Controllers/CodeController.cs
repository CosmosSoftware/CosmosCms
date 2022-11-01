
using Cosmos.Cms.Common.Data;
using Cosmos.Cms.Common.Models;
using Cosmos.Cms.Common.Services.Configurations;
using Cosmos.Cms.Models;
using Jering.Javascript.NodeJS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cosmos.Cms.Controllers
{
    /// <summary>
    /// API Controller
    /// </summary>
    [AllowAnonymous]
    [Authorize(Roles = "Administrators, Editors, Authors")]
    public class CodeController : Controller
    {

        private readonly INodeJSService _nodeJSService;
        private readonly IOptions<CosmosConfig> _cosmosConfig;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<CodeController> _logger;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="nodeJSService"></param>
        /// <param name="cosmosConfig"></param>
        /// <param name="dbContext"></param>
        /// <param name="logger"></param>
        public CodeController(INodeJSService nodeJSService, IOptions<CosmosConfig> cosmosConfig,
            Cosmos.Cms.Common.Data.ApplicationDbContext dbContext, ILogger<CodeController> logger)
        {
            _nodeJSService = nodeJSService;
            _cosmosConfig = cosmosConfig;
            _dbContext = dbContext;
            _logger = (ILogger<CodeController>)logger;
        }

        /// <summary>
        /// Script inventory
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            var data = await _dbContext.NodeScripts.ToListAsync();

            return View(data.AsQueryable());
        }

        /// <summary>
        /// Create a new script
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Create()
        {
            return View(new NodeScriptItemViewModel());
        }

        /// <summary>
        /// Create a new script
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NodeScriptItemViewModel model)
        {
            if (model == null)
            {
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.EndPoint))
            {
                model.EndPoint = model.EndPoint.ToLowerInvariant();
            }

            if (!string.IsNullOrEmpty(model.InputVars))
            {
                model.InputVars = model.InputVars.Trim().ToLowerInvariant();
                var vars = model.InputVars.Split(',');

                foreach (var v in vars)
                {
                    if (!Regex.IsMatch(v, @"^[a-zA-Z0-9]+$"))
                    {
                        ModelState.AddModelError("InputVars", "Must be either letters or numbers");
                        break;
                    }
                }

            }

            if (await _dbContext.NodeScripts.Where(w => w.EndPoint == model.EndPoint).CosmosAnyAsync())
            {
                ModelState.AddModelError("EndPoint", "End point already exists");
            }

            if (ModelState.IsValid)
            {
                var script = new NodeScript()
                {
                    Code = "module.exports = async (arg1) => { return \"Hello World\"; }",
                    Description = "Add your description here.",
                    EndPoint = model.EndPoint,
                    Updated = DateTimeOffset.UtcNow,
                    Id = model.Id,
                    Version = model.Version,
                    InputVars = string.IsNullOrEmpty(model.InputVars) ? new[] { "" } : model.InputVars.Split(',').Select(s => s.Trim()).ToArray()
                };

                _dbContext.NodeScripts.Add(script);
                await _dbContext.SaveChangesAsync();

                return RedirectToAction("Edit", "Code", new { Id = model.Id });
            }

            return View(model);
        }

        /// <summary>
        /// Opens the debug terminal
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Debug(Guid Id)
        {
            var script = await _dbContext.NodeScripts.FirstOrDefaultAsync(f => f.Id == Id);
            return View(script);
        }

        /// <summary>
        /// Runs the script
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Run(Guid Id)
        {

            var debugResult = new DebugViewModel()
            {
                Id = Id
            };

            try
            {
                var script = await _dbContext.NodeScripts.FirstOrDefaultAsync(f => f.Id == Id);

                var values = GetArgs(Request, script);

                // Send the module string to NodeJS where it's compiled, invoked and cached.
                await _nodeJSService.InvokeFromFileAsync("SendGrid", args: values);

                debugResult.ApiResult = new ApiResult("Done!")
                {
                    IsSuccess = true
                };
            }
            catch (Exception e)
            {
                var error = new ApiResult("Error")
                {
                    IsSuccess = false
                };

                error.Errors.Add("Error", e.Message);

                debugResult.ApiResult = error;
            }

            return Json(debugResult);
        }

        /// <summary>
        /// Gets arguments from a request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="script"></param>
        /// <returns></returns>
        public static ApiArgument[] GetArgs(Microsoft.AspNetCore.Http.HttpRequest request, NodeScript script)
        {
            if (request.Method == "POST")
            {
                if (request.ContentType == null)
                {
                    var values = new List<ApiArgument>();

                    foreach (var item in script.InputVars)
                    {
                        values.Add(new ApiArgument() { Key = item, Value = request.Headers[item] });
                    }

                    return values.ToArray();
                }

                return request.Form.Where(a => script.InputVars.Contains(a.Key))
                    .Select(s => new ApiArgument()
                    {
                        Key = s.Key,
                        Value = s.Value
                    }).ToArray();
            }
            else if (request.Method == "GET")
            {
                return request.Query.Where(a => script.InputVars.Contains(a.Key))
                    .Select(s => new ApiArgument()
                    {
                        Key = s.Key,
                        Value = s.Value
                    }).ToArray();
            }
            return null;
        }

        /// <summary>
        /// Deletes an endpoint
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(Guid Id)
        {
            var doomed = await _dbContext.NodeScripts.FirstOrDefaultAsync(w => w.Id == Id);

            _dbContext.NodeScripts.Remove(doomed);

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                var t = e;
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Edit code
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(Guid Id)
        {
            var data = await _dbContext.NodeScripts.FindAsync(Id);

            if (data == null)
            {
                return NotFound();
            }

            ViewData["Version"] = data.Version;

            return View(new EditScriptPostModel()
            {
                Id = data.Id,
                Version = data.Version,
                Code = data.Code,
                Config = data.Config,
                Description = data.Description,
                RoleList = data.Roles == null ? "" : string.Join(',', data.Roles),
                EditingField = "Code",
                EditorTitle = data.EndPoint,
                EndPoint = data.EndPoint,
                Published = data.Published,
                InputVars = string.Join(',', data.InputVars),
                CustomButtons = new[] { "Debug", "Inputs" },
                EditorFields = new[]
                    {
                        new EditorField
                        {
                            FieldId = "Code",
                            FieldName = "Script",
                            EditorMode = EditorMode.JavaScript,
                            IconUrl = "/images/seti-ui/icons/javascript.svg",
                            ToolTip = "NodeJS script."
                        }
                    }
            });
        }

        /// <summary>
        /// Edit post back
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Edit(EditScriptPostModel model)
        {
            if (model == null) return NotFound();

            var entity = await _dbContext.NodeScripts.FindAsync(model.Id);

            if (entity == null) return NotFound();

            // Validate security for authors before going further
            if (entity.Published.HasValue && User.IsInRole("Authors"))
                return Unauthorized();

            if (ModelState.IsValid)
            {
                try
                {
                    if (string.IsNullOrEmpty(model.InputVars))
                    {
                        entity.InputVars = new string[] { };
                    }
                    else
                    {
                        entity.InputVars = model.InputVars.Split(',').Select(s => s.Trim()).ToArray();
                    }

                    entity.Updated = DateTimeOffset.UtcNow;
                    entity.Code = model.Code;
                    entity.Published = model.Published;
                    entity.Description = model.Description;
                    entity.Roles = string.IsNullOrEmpty(model.RoleList) ? null : model.RoleList.Split(',');

                    await _dbContext.SaveChangesAsync();
                    model.IsValid = true;

                }
                catch (Exception e)
                {
                    var provider = new EmptyModelMetadataProvider();
                    ModelState.AddModelError("Save", e, provider.GetMetadataForType(typeof(string)));
                }
            }


            return Json(model);
        }

        /// <summary>
        /// Script inventory
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Versions(Guid Id)
        {
            ViewData["EndPoint"] = Id;
            var data = await _dbContext.NodeScripts
                .Where(w => w.Id == Id)
                .OrderByDescending(o => o.Version)
                .Select(s => new NodeScriptItemViewModel
                {
                    Id = s.Id,
                    Published = s.Published,
                    EndPoint = s.EndPoint,
                    Updated = s.Updated,
                    Version = s.Version,
                    Expires = s.Expires
                }).ToListAsync();

            return View(data.AsQueryable());
        }

        #region GRID DATA

        /// <summary>
        /// Sends an article (or page) to trash bin.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        //[HttpPost]
        //public async Task<IActionResult> Trash_Script([DataSourceRequest] DataSourceRequest request,
        //    ScriptCatalogEntry model)
        //{
        //    _dbContext.Entry(model).State = EntityState.Deleted;
        //    var doomed = await _dbContext.NodeScripts
        //        .Where(w => w.EndPoint == model.EndPoint).ToListAsync();
        //    foreach (var d in doomed)
        //    {
        //        d.StatusCode = (int)StatusCodeEnum.Deleted;
        //    }

        //    await _dbContext.SaveChangesAsync();

        //    return Json(await new[] { model }.ToDataSourceResultAsync(request, ModelState));
        //}

        /// <summary>
        /// Get all the versions of a script
        /// </summary>
        /// <param name="request"></param>
        /// <param name="Id"></param>
        /// <returns></returns>
        //public async Task<IActionResult> Read_Versions([DataSourceRequest] DataSourceRequest request, string Id)
        //{
        //    var data = await _dbContext.NodeScripts
        //        .WithPartitionKey(Id)
        //        .OrderByDescending(o => o.Version)
        //        .Select(s => new NodeScriptItemViewModel
        //        {
        //            Id = s.Id,
        //            Published = s.Published,
        //            EndPoint = s.EndPoint,
        //            Updated = s.Updated,
        //            Version = s.Version,
        //            Expires = s.Expires
        //        }).ToDataSourceResultAsync(request);

        //    return Json(data);
        //}

        #endregion
    }
}
