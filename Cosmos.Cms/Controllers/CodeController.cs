using Cosmos.Cms.Common.Data;
using Cosmos.Cms.Common.Data.Logic;
using Cosmos.Cms.Common.Models;
using Cosmos.Cms.Common.Services.Configurations;
using Cosmos.Cms.Models;
using Jering.Javascript.NodeJS;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
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
    public class CodeController : ApiController
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
            ApplicationDbContext dbContext, ILogger logger) : base(nodeJSService, cosmosConfig, dbContext, logger)
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
        public IActionResult Index()
        {
            return View();
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
                    EndPoint = model.EndPoint,
                    Updated = DateTimeOffset.UtcNow,
                    Id = model.Id,
                    Version = model.Version,
                    InputVars = string.IsNullOrEmpty(model.InputVars) ? new[] { "" } : model.InputVars.Split(',').Select(s => s.Trim()).ToArray()
                };

                var entry = new ScriptCatalogEntry()
                {
                    EndPoint = script.EndPoint,
                    Updated = script.Updated,
                    Published = script.Published
                };

                _dbContext.NodeScripts.Add(script);
                _dbContext.ScriptCatalog.Add(entry);
                await _dbContext.SaveChangesAsync();

                return RedirectToAction("Edit", "Code", new { Id = model.Id });
            }

            return View(model);
        }

        /// <summary>
        /// Edit code
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public async Task<IActionResult> EditCode(Guid Id)
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
        public async Task<IActionResult> EditCode(EditScriptPostModel model)
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
                        entity.InputVars = new string [] {};
                    }
                    else
                    {
                        entity.InputVars = model.InputVars.Split(',').Select(s => s.Trim()).ToArray();
                    }

                    entity.Updated = DateTimeOffset.UtcNow;
                    entity.Code = model.Code;
                    entity.Published = model.Published;

                    await _dbContext.SaveChangesAsync();

                    if (model.EndPoint != entity.EndPoint)
                    {
                        var vers = await _dbContext.NodeScripts
                            .WithPartitionKey(model.EndPoint)
                            .ToListAsync();


                        var entry = await _dbContext.ScriptCatalog
                            .FindAsync(entity.EndPoint);

                        entry.EndPoint = model.EndPoint;

                        foreach (var v in vers)
                        {
                            _dbContext.Entry(v).State = EntityState.Detached;
                            v.Id = Guid.NewGuid();
                            v.EndPoint = model.EndPoint;
                            _dbContext.NodeScripts.Add(v);
                        }

                        await _dbContext.SaveChangesAsync();

                        var doomed = await _dbContext.NodeScripts
                            .WithPartitionKey(model.EndPoint)
                            .ToListAsync();

                        _dbContext.NodeScripts.RemoveRange(doomed);

                        await _dbContext.SaveChangesAsync();

                    }
                }
                catch (Exception e)
                {
                    var provider = new EmptyModelMetadataProvider();
                    ModelState.AddModelError("Save", e, provider.GetMetadataForType(typeof(string)));
                }
            }

            // ReSharper disable once PossibleNullReferenceException
            ViewData["Version"] = entity.Version;

            var jsonModel = new SaveCodeResultJsonModel
            {
                ErrorCount = ModelState.ErrorCount,
                IsValid = ModelState.IsValid
            };
            jsonModel.Errors.AddRange(ModelState.Values
                .Where(w => w.ValidationState == ModelValidationState.Invalid)
                .ToList());
            jsonModel.ValidationState = ModelState.ValidationState;

            DateTimeOffset? publishedDateTime = null;
            if (entity.Published.HasValue)
            {
                publishedDateTime = entity.Published.Value.ToUniversalTime();
            }

            return Json(jsonModel);
        }

        /// <summary>
        /// Script inventory
        /// </summary>
        /// <returns></returns>
        public IActionResult Versions(string Id)
        {
            ViewData["EndPoint"] = Id;
            return View();
        }

        #region GRID DATA

        /// <summary>
        /// Reads the script catalog
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<IActionResult> Read_Scripts([DataSourceRequest] DataSourceRequest request)
        {
            var data = await _dbContext.ScriptCatalog.ToDataSourceResultAsync(request);
            return Json(data);
        }

        /// <summary>
        /// Sends an article (or page) to trash bin.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Trash_Script([DataSourceRequest] DataSourceRequest request,
            ScriptCatalogEntry model)
        {
            _dbContext.Entry(model).State = EntityState.Deleted;
            var doomed = await _dbContext.NodeScripts
                .Where(w => w.EndPoint == model.EndPoint).ToListAsync();
            foreach (var d in doomed)
            {
                d.StatusCode = (int)StatusCodeEnum.Deleted;
            }

            await _dbContext.SaveChangesAsync();

            return Json(await new[] { model }.ToDataSourceResultAsync(request, ModelState));
        }

        /// <summary>
        /// Get all the versions of a script
        /// </summary>
        /// <param name="request"></param>
        /// <param name="Id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Read_Versions([DataSourceRequest] DataSourceRequest request, string Id)
        {
            var data = await _dbContext.NodeScripts
                .WithPartitionKey(Id)
                .OrderByDescending(o => o.Version)
                .Select(s => new NodeScriptItemViewModel
                {
                    Id = s.Id,
                    Published = s.Published,
                    EndPoint = s.EndPoint,
                    Updated = s.Updated,
                    Version = s.Version,
                    Expires = s.Expires
                }).ToDataSourceResultAsync(request);

            return Json(data);
        }

        #endregion
    }
}
