
using Cosmos.BlobService;
using Cosmos.BlobService.Config;
using Cosmos.BlobService.Models;
using Cosmos.Cms.Common.Data;
using Cosmos.Cms.Common.Models;
using Cosmos.Cms.Common.Services.Configurations;
using Cosmos.Cms.Models;
using Google.Rpc;
using Jering.Javascript.NodeJS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

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
        private readonly FileStorageContext _storageContext;
        private readonly IOptions<CosmosConfig> _options;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="nodeJSService"></param>
        /// <param name="cosmosConfig"></param>
        /// <param name="dbContext"></param>
        /// <param name="logger"></param>
        /// <param name="storageContext"></param>
        public CodeController(INodeJSService nodeJSService, IOptions<CosmosConfig> cosmosConfig,
            ApplicationDbContext dbContext, ILogger<CodeController> logger, FileStorageContext storageContext)
        {
            _nodeJSService = nodeJSService;
            _cosmosConfig = cosmosConfig;
            _dbContext = dbContext;
            _logger = logger;
            _storageContext = storageContext;
            _options = cosmosConfig;
        }

        private static long DivideByAndRoundUp(long number, long divideBy)
        {
            return (long)Math.Ceiling((float)number / (float)divideBy);
        }

        /// <summary>
        ///     Encodes a URL
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <remarks>
        ///     For more information, see
        ///     <a
        ///         href="https://docs.microsoft.com/en-us/rest/api/storageservices/Naming-and-Referencing-Containers--Blobs--and-Metadata#blob-names">
        ///         documentation
        ///     </a>
        ///     .
        /// </remarks>
        public string UrlEncode(string path)
        {
            var parts = ParsePath(path);
            var urlEncodedParts = new List<string>();
            foreach (var part in parts) urlEncodedParts.Add(HttpUtility.UrlEncode(part.Replace(" ", "-")).Replace("%40", "@"));

            return TrimPathPart(string.Join('/', urlEncodedParts));
        }

        /// <summary>
        ///     Parses out a path into a string array.
        /// </summary>
        /// <param name="pathParts"></param>
        /// <returns></returns>
        public string[] ParsePath(params string[] pathParts)
        {
            if (pathParts == null) return new string[] { };

            var paths = new List<string>();

            foreach (var part in pathParts)
                if (!string.IsNullOrEmpty(part))
                {
                    var split = part.Split("/");
                    foreach (var p in split)
                        if (!string.IsNullOrEmpty(p))
                        {
                            var path = TrimPathPart(p);
                            if (!string.IsNullOrEmpty(path)) paths.Add(path);
                        }
                }

            return paths.ToArray();
        }

        /// <summary>
        ///     Trims leading and trailing slashes and white space from a path part.
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        public string TrimPathPart(string part)
        {
            if (string.IsNullOrEmpty(part))
                return "";

            return part.Trim('/').Trim('\\').Trim();
        }

        /// <summary>
        /// Script inventory
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index(string sortOrder, string currentSort, int pageNo = 0, int pageSize = 10)
        {
            ViewData["sortOrder"] = sortOrder;
            ViewData["currentSort"] = currentSort;
            ViewData["pageNo"] = pageNo;
            ViewData["pageSize"] = pageSize;

            var query = _dbContext.NodeScripts.AsQueryable();

            ViewData["RowCount"] = await query.CountAsync();

            if (sortOrder == "desc")
            {
                if (!string.IsNullOrEmpty(currentSort))
                {
                    switch (currentSort)
                    {
                        case "Version":
                            query = query.OrderByDescending(o => o.Version);
                            break;
                        case "EndPoint":
                            query = query.OrderByDescending(o => o.EndPoint);
                            break;
                        case "Published":
                            query = query.OrderByDescending(o => o.Published);
                            break;
                        case "Updated":
                            query = query.OrderByDescending(o => o.Updated);
                            break;
                        case "Description":
                            query = query.OrderByDescending(o => o.Description);
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
                        case "Version":
                            query = query.OrderBy(o => o.Version);
                            break;
                        case "EndPoint":
                            query = query.OrderBy(o => o.EndPoint);
                            break;
                        case "Published":
                            query = query.OrderBy(o => o.Published);
                            break;
                        case "Updated":
                            query = query.OrderBy(o => o.Updated);
                            break;
                        case "Description":
                            query = query.OrderBy(o => o.Description);
                            break;
                    }
                }
            }
            var model = query.Skip(pageNo * pageSize).Take(pageSize);

            return View(await model.ToListAsync());
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


        #region FILE MANAGER FUNCTIONS

        /// <summary>
        /// Moves items to a new folder.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Move(MoveFilesViewModel model)
        {
            try
            {
                foreach (var item in model.Items)
                {
                    string dest;

                    if (item.EndsWith("/"))
                    {
                        // moving a directory
                        dest = model.Destination + item.TrimEnd('/').Split('/').LastOrDefault();
                    }
                    else
                    {
                        // moving a file
                        dest = model.Destination + item.Split('/').LastOrDefault();
                    }

                    await _storageContext.RenameAsync(item, dest);
                }

            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

            return Ok();
        }

        /// <summary>
        /// New folder action
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewFolder(NewFolderViewModel model)
        {
            var relativePath = string.Join('/', ParsePath(model.ParentFolder, model.FolderName));
            relativePath = UrlEncode(relativePath);

            // Check for duplicate entries
            var existingEntries = await _storageContext.GetFolderContents(model.ParentFolder);

            if (existingEntries.Any(f => f.Name.Equals(model.FolderName)) == false)
            {
                var fileManagerEntry = _storageContext.CreateFolder(relativePath);
            }

            return RedirectToAction("Source", new { target = model.ParentFolder, directoryOnly = model.DirectoryOnly });

        }

        /// <summary>
        /// Download a file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<IActionResult> Download(string path)
        {
            var blob = await _storageContext.GetFileAsync(path);

            if (!blob.IsDirectory)
            {
                using var stream = await _storageContext.OpenBlobReadStreamAsync(path);
                using var memStream = new MemoryStream();
                stream.CopyTo(memStream);
                return File(memStream.ToArray(), "application/octet-stream", fileDownloadName: blob.Name);
            }

            return NotFound();
        }

        /// <summary>
        ///     Creates a new entry, using relative path-ing, and normalizes entry name to lower case.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="entry"></param>
        /// <returns><see cref="JsonResult" />(<see cref="BlobService.FileManagerEntry" />)</returns>
        public async Task<ActionResult> Create(string target, BlobService.FileManagerEntry entry)
        {
            target = target == null ? "" : target;
            entry.Path = target;
            entry.Name = UrlEncode(entry.Name);
            entry.Extension = entry.Extension;

            if (!entry.Path.StartsWith("/pub", StringComparison.CurrentCultureIgnoreCase))
            {
                return Unauthorized("New folders can't be created here using this tool. Please select the 'pub' folder and try again.");
            }

            // Check for duplicate entries
            var existingEntries = await _storageContext.GetFolderContents(target);

            if (existingEntries != null && existingEntries.Any())
            {
                var results = existingEntries.FirstOrDefault(f => f.Name.Equals(entry.Name));

                if (results != null)
                {
                    //var i = 1;
                    var originalName = entry.Name;
                    for (var i = 0; i < existingEntries.Count; i++)
                    {
                        entry.Name = originalName + "-" + (i + 1);
                        if (!existingEntries.Any(f => f.Name.Equals(entry.Name))) break;
                        i++;
                    }
                }
            }

            var relativePath = string.Join('/', ParsePath(entry.Path, entry.Name));
            relativePath = UrlEncode(relativePath);

            var fileManagerEntry = _storageContext.CreateFolder(relativePath);

            return Json(fileManagerEntry);
        }

        /// <summary>
        ///     Deletes a folder, normalizes entry to lower case.
        /// </summary>
        /// <param name="model">Item to delete using relative path</param>
        /// <returns></returns>
        public async Task<ActionResult> DeleteItems(DeleteBlobItemsViewModel model)
        { 
            foreach (var item in model.Paths)
            {
                if (item.EndsWith('/'))
                {
                    await _storageContext.DeleteFolderAsync(item.TrimEnd('/'));
                }
                else
                {
                    await _storageContext.DeleteFileAsync(item);
                }
            }

            return Ok();
        }

        /// <summary>
        /// Rename a blob item.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rename(RenameBlobViewModel model)
        {
            if (!string.IsNullOrEmpty(model.ToBlobName))
            {
                // Note rules:
                // 1. New folder names must end with slash.
                // 2. New file names must never end with a slash.
                if (model.FromBlobName.EndsWith("/"))
                {
                    if (!model.ToBlobName.EndsWith("/"))
                    {
                        model.ToBlobName = model.ToBlobName + "/";
                    }
                }
                else
                {
                    model.ToBlobName = model.ToBlobName.TrimEnd('/');
                }

                var target = $"{model.BlobRootPath}/{model.FromBlobName}";

                var dest = $"{model.BlobRootPath}/{UrlEncode(model.ToBlobName)}";

                await _storageContext.RenameAsync(target, dest);
            }

            return Ok();
        }

        /// <summary>
        /// Source files inventory
        /// </summary>
        /// <param name="target"></param>
        /// <param name="sortOrder"></param>
        /// <param name="currentSort"></param>
        /// <param name="pageNo"></param>
        /// <param name="pageSize"></param>
        /// <param name="directoryOnly"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Source(string target, string sortOrder = "asc", string currentSort = "Name", int pageNo = 0, int pageSize = 10, bool directoryOnly = false)
        {
            target = string.IsNullOrEmpty(target) ? "" : HttpUtility.UrlDecode(target);

            ViewData["PathPrefix"] = target.StartsWith('/') ? target : "/" + target;
            ViewData["DirectoryOnly"] = directoryOnly;
            ViewData["Container"] = null;

            //
            // GET FULL OR ABSOLUTE PATH
            //
            var model = await _storageContext.GetFolderContents(target);

            ViewData["sortOrder"] = sortOrder;
            ViewData["currentSort"] = currentSort;
            ViewData["pageNo"] = pageNo;
            ViewData["pageSize"] = pageSize;

            var query = model.AsQueryable();

            ViewData["RowCount"] = query.Count();

            if (sortOrder == "desc")
            {
                if (!string.IsNullOrEmpty(currentSort))
                {
                    switch (currentSort)
                    {
                        case "Name":
                            query = query.OrderByDescending(o => o.Name);
                            break;
                        case "IsDirectory":
                            query = query.OrderByDescending(o => o.IsDirectory);
                            break;
                        case "CreatedUtc":
                            query = query.OrderByDescending(o => o.CreatedUtc);
                            break;
                        case "Extension":
                            query = query.OrderByDescending(o => o.Extension);
                            break;
                        case "ModifiedUtc":
                            query = query.OrderByDescending(o => o.ModifiedUtc);
                            break;
                        case "Path":
                            query = query.OrderByDescending(o => o.Path);
                            break;
                        case "Size":
                            query = query.OrderByDescending(o => o.Size);
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
                        case "Name":
                            query = query.OrderBy(o => o.Name);
                            break;
                        case "IsDirectory":
                            query = query.OrderBy(o => o.IsDirectory);
                            break;
                        case "CreatedUtc":
                            query = query.OrderBy(o => o.CreatedUtc);
                            break;
                        case "Extension":
                            query = query.OrderBy(o => o.Extension);
                            break;
                        case "ModifiedUtc":
                            query = query.OrderBy(o => o.ModifiedUtc);
                            break;
                        case "Path":
                            query = query.OrderBy(o => o.Path);
                            break;
                        case "Size":
                            query = query.OrderBy(o => o.Size);
                            break;
                    }
                }
            }
            if (directoryOnly)
            {
                return View(model.Where(w => w.IsDirectory == true).ToList());
            }

            return View(model.ToList());
        }

        /// <summary>
        /// Gets a unique GUID for FilePond
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Process([FromForm] string files)
        {
            var parsed = JsonConvert.DeserializeObject<FilePondMetadata>(files);

            var mime = MimeTypeMap.GetMimeType(Path.GetExtension(parsed.FileName));

            var uid = $"{parsed.Path.TrimEnd('/')}|{parsed.RelativePath.TrimStart('/')}|{Guid.NewGuid().ToString()}|{mime}";

            return Ok(uid);
        }

        /// <summary>
        /// Process a chunched upload
        /// </summary>
        /// <param name="patch"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        [HttpPatch]
        public async Task<ActionResult> Process(string patch, string options = "")
        {
            try
            {
                var patchArray = patch.Split('|');

                // Mime type
                var contentType = patchArray[3];

                // 0 based index
                var uploadOffset = long.Parse(Request.Headers["Upload-Offset"]);

                // File name being uploaded
                var UploadName = ((string)Request.Headers["Upload-Name"]);

                // Total size of the file in bytes
                var uploadLenth = long.Parse(Request.Headers["Upload-Length"]);

                // Size of the chunk
                var contentSize = long.Parse(Request.Headers["Content-Length"]);

                long chunk = 0;

                if (uploadOffset > 0)
                {
                    chunk = DivideByAndRoundUp(uploadLenth, uploadOffset);
                }

                var totalChunks = DivideByAndRoundUp(uploadLenth, contentSize);


                var blobName = UrlEncode(UploadName);

                var relativePath = UrlEncode(patchArray[0].TrimEnd('/'));

                if (!string.IsNullOrEmpty(patchArray[1]))
                {
                    var dpath = Path.GetDirectoryName(patchArray[1]).Replace('\\', '/'); // Convert windows paths to unix style.
                    var epath = UrlEncode(dpath);
                    relativePath += "/" + UrlEncode(epath);
                }

                var metaData = new FileUploadMetaData()
                {
                    ChunkIndex = chunk,
                    ContentType = contentType,
                    FileName = blobName,
                    RelativePath = relativePath + "/" + blobName,
                    TotalChunks = totalChunks,
                    TotalFileSize = uploadLenth,
                    UploadUid = patchArray[1]
                };

                // Make sure full folder path exists
                var pathParts = patchArray[0].Trim('/').Split('/');
                var part = "";
                for (int i = 0; i < pathParts.Length - 1; i++)
                {
                    if (i == 0 && pathParts[i] != "pub")
                    {
                        throw new Exception("Must upload folders and files under /pub directory.");
                    }

                    part = $"{part}/{pathParts[i]}";
                    if (part != "/pub")
                    {
                        var folder = part.Trim('/');
                        await _storageContext.CreateFolder(folder);
                    }
                }

                using var memoryStream = new MemoryStream();
                await Request.Body.CopyToAsync(memoryStream);

                await _storageContext.AppendBlob(memoryStream, metaData);

            }
            catch (Exception e)
            {
                //var t = e; // For debugging
                _logger.LogError(e.Message, e);
                throw;
            }


            return Ok();
        }

        #endregion


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
                model.EndPoint = model.EndPoint;
            }

            if (!string.IsNullOrEmpty(model.InputVars))
            {
                model.InputVars = model.InputVars.Trim();
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

            string result;
            try
            {
                var script = await _dbContext.NodeScripts.FirstOrDefaultAsync(f => f.Id == Id);

                var values = GetArgs(Request, script);

                // Send the module string to NodeJS where it's compiled, invoked and cached.
                if (string.IsNullOrEmpty(script.Code))
                {
                    result = await _nodeJSService.InvokeFromFileAsync<string>($"{script.EndPoint}", args: values.Select(s => s.Value).ToArray());

                }
                else
                {
                    result = await _nodeJSService.InvokeFromStringAsync<string>(script.Code, args: values);
                }


                debugResult.ApiResult = new ApiResult("Done!")
                {
                    IsSuccess = true, ReturnData = result
                };
            }
            catch (Exception e)
            {
                var error = new ApiResult("Error")
                {
                    IsSuccess = false
                };

                error.Errors.Add("Error", e.Message);
                if (e.InnerException != null)
                {
                    error.Errors.Add("Inner:", e.InnerException.Message);
                }

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
            var inputVarDefs = script.InputVars.Select(s => new InputVarDefinition(s)).ToList();

            if (request.Method == "POST")
            {
                if (request.ContentType == null)
                {
                    var values = new List<ApiArgument>();

                    foreach (var item in inputVarDefs)
                    {
                        var value = (string) request.Headers[item.Name];
                        value = string.IsNullOrEmpty(value) ? "" : value.Substring(0, item.MaxLength);

                        values.Add(new ApiArgument() { Key = item.Name, Value = value });
                    }

                    return values.ToArray();
                }

                if (request.Form != null)
                {
                    return request.Form.Where(a => script.InputVars.Contains(a.Key))
                   .Select(s => new ApiArgument()
                   {
                       Key = s.Key,
                       Value = s.Value
                   }).ToArray();
                }

                return null;
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
        public async Task<IActionResult> Versions(string Id, string sortOrder = "desc", string currentSort = "Version", int pageNo = 0, int pageSize = 10, int? versionNumber = null)
        {
            ViewData["EndPoint"] = Id;

            ViewData["sortOrder"] = sortOrder;
            ViewData["currentSort"] = currentSort;
            ViewData["pageNo"] = pageNo;
            ViewData["pageSize"] = pageSize;

            var query = _dbContext.NodeScripts
                .Where(w => w.EndPoint == Id)
                .Select(s => new NodeScriptItemViewModel
                {
                    Id = s.Id,
                    Published = s.Published,
                    EndPoint = s.EndPoint,
                    Updated = s.Updated,
                    Version = s.Version,
                    Expires = s.Expires
                });

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
                        case "Version":
                            query = query.OrderByDescending(o => o.Version);
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
                        case "Version":
                            query = query.OrderBy(o => o.Version);
                            break;
                        case "Expires":
                            query = query.OrderBy(o => o.Expires);
                            break;
                    }
                }
            }

            return View(await query.ToListAsync());
        }


        #region CODE EDITOR METHODS

        /// <summary>
        /// Edit code for a file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<IActionResult> EditCode(string path)
        {
            try
            {
                var extension = Path.GetExtension(path.ToLower());

                var filter = _options.Value.SiteSettings.AllowedFileTypes.Split(',');
                var editorField = new EditorField
                {
                    FieldId = "Content",
                    FieldName = Path.GetFileName(path)
                };

                if (!filter.Contains(extension)) return new UnsupportedMediaTypeResult();

                switch (extension)
                {
                    case ".js":
                        editorField.EditorMode = EditorMode.JavaScript;
                        editorField.IconUrl = "/images/seti-ui/icons/javascript.svg";
                        break;
                    case ".html":
                        editorField.EditorMode = EditorMode.Html;
                        editorField.IconUrl = "/images/seti-ui/icons/html.svg";
                        break;
                    case ".css":
                        editorField.EditorMode = EditorMode.Css;
                        editorField.IconUrl = "/images/seti-ui/icons/css.svg";
                        break;
                    case ".xml":
                        editorField.EditorMode = EditorMode.Xml;
                        editorField.IconUrl = "/images/seti-ui/icons/javascript.svg";
                        break;
                    case ".json":
                        editorField.EditorMode = EditorMode.Json;
                        editorField.IconUrl = "/images/seti-ui/icons/javascript.svg";
                        break;
                    default:
                        editorField.EditorMode = EditorMode.Html;
                        editorField.IconUrl = "/images/seti-ui/icons/html.svg";
                        break;
                }

                //
                // Get the blob now, so we can determine the type, or use this client as-is
                //
                //var properties = blob.GetProperties();

                // Open a stream
                await using var memoryStream = new MemoryStream();

                await using (var stream = await _storageContext.OpenBlobReadStreamAsync(path))
                {
                    // Load into memory and release the blob stream right away
                    await stream.CopyToAsync(memoryStream);
                }

                var metaData = await _storageContext.GetFileAsync(path);


                ViewData["PageTitle"] = metaData.Name;
                ViewData[" Published"] = DateTimeOffset.FromFileTime(metaData.ModifiedUtc.Ticks);

                return View(new FileManagerEditCodeViewModel
                {
                    Id = path,
                    Path = path,
                    EditorTitle = Path.GetFileName(Path.GetFileName(path)),
                    EditorFields = new List<EditorField>
                    {
                        editorField
                    },
                    Content = Encoding.UTF8.GetString(memoryStream.ToArray()),
                    EditingField = "Content",
                    CustomButtons = new List<string>()
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
                throw;
            }
        }

        /// <summary>
        /// Save the file
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCode(FileManagerEditCodeViewModel model)
        {

            var extension = Path.GetExtension(model.Path.ToLower());

            var filter = _options.Value.SiteSettings.AllowedFileTypes.Split(',');
            var editorField = new EditorField
            {
                FieldId = "Content",
                FieldName = Path.GetFileName(model.Path)
            };

            if (!filter.Contains(extension)) return new UnsupportedMediaTypeResult();

            var contentType = string.Empty;

            switch (extension)
            {
                case ".js":
                    editorField.EditorMode = EditorMode.JavaScript;
                    editorField.IconUrl = "/images/seti-ui/icons/javascript.svg";
                    break;
                case ".html":
                    editorField.EditorMode = EditorMode.Html;
                    editorField.IconUrl = "/images/seti-ui/icons/html.svg";
                    break;
                case ".css":
                    editorField.EditorMode = EditorMode.Css;
                    editorField.IconUrl = "/images/seti-ui/icons/css.svg";
                    break;
                case ".xml":
                    editorField.EditorMode = EditorMode.Xml;
                    editorField.IconUrl = "/images/seti-ui/icons/javascript.svg";
                    break;
                case ".json":
                    editorField.EditorMode = EditorMode.Json;
                    editorField.IconUrl = "/images/seti-ui/icons/javascript.svg";
                    break;
                default:
                    editorField.EditorMode = EditorMode.Html;
                    editorField.IconUrl = "/images/seti-ui/icons/html.svg";
                    break;
            }

            // Save the blob now

            var bytes = Encoding.Default.GetBytes(model.Content);

            using var memoryStream = new MemoryStream(bytes, false);

            var formFile = new FormFile(memoryStream, 0, memoryStream.Length, Path.GetFileNameWithoutExtension(model.Path), Path.GetFileName(model.Path));

            var metaData = new FileUploadMetaData
            {
                ChunkIndex = 0,
                ContentType = contentType,
                FileName = Path.GetFileName(model.Path),
                RelativePath = Path.GetFileName(model.Path),
                TotalFileSize = memoryStream.Length,
                UploadUid = Guid.NewGuid().ToString(),
                TotalChunks = 1
            };

            var uploadPath = model.Path.TrimEnd(metaData.FileName.ToArray()).TrimEnd('/');

            var result = (JsonResult)await Upload(new IFormFile[] { formFile }, JsonConvert.SerializeObject(metaData), uploadPath);

            var resultMode = (FileUploadResult)result.Value;

            var jsonModel = new SaveCodeResultJsonModel
            {
                ErrorCount = ModelState.ErrorCount,
                IsValid = ModelState.IsValid
            };

            if (!resultMode.uploaded)
            {
                ModelState.AddModelError("", $"Error saving {Path.GetFileName(model.Path)}");
            }

            jsonModel.Errors.AddRange(ModelState.Values
                .Where(w => w.ValidationState == Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid)
                .ToList());
            jsonModel.ValidationState = ModelState.ValidationState;

            return Json(jsonModel);
        }


        /// <summary>
        ///     Used to upload files, one chunk at a time, and normalizes the blob name to lower case.
        /// </summary>
        /// <param name="files"></param>
        /// <param name="metaData"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        [HttpPost]
        [RequestSizeLimit(
            6291456)] // AWS S3 multi part upload requires 5 MB parts--no more, no less so pad the upload size by a MB just in case
        public async Task<ActionResult> Upload(IEnumerable<IFormFile> files,
            string metaData, string path)
        {
            
            try
            {
                if (files == null || files.Any() == false)
                    return Json("");

                if (string.IsNullOrEmpty(path) || path.Trim('/') == "") return Unauthorized("Cannot upload here. Please select the 'pub' folder first, or sub-folder below that, then try again.");

                //
                // Get information about the chunk we are on.
                //
                var ms = new MemoryStream(Encoding.UTF8.GetBytes(metaData));

                var serializer = new JsonSerializer();
                FileUploadMetaData fileMetaData;
                using (var streamReader = new StreamReader(ms))
                {
                    fileMetaData =
                        (FileUploadMetaData)serializer.Deserialize(streamReader, typeof(FileUploadMetaData));
                }

                if (fileMetaData == null) throw new Exception("Could not read the file's metadata");

                var file = files.FirstOrDefault();

                if (file == null) throw new Exception("No file found to upload.");

                var blobName = UrlEncode(fileMetaData.FileName);

                fileMetaData.FileName = blobName;
                fileMetaData.RelativePath = (path.TrimEnd('/') + "/" + fileMetaData.RelativePath);

                // Make sure full folder path exists
                var parts = fileMetaData.RelativePath.Trim('/').Split('/');
                var part = "";
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    part = $"{part}/{parts[i]}";
                    var folder = part.Trim('/');
                    await _storageContext.CreateFolder(folder);
                }

                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);

                await _storageContext.DeleteFileAsync(fileMetaData.RelativePath);

                await _storageContext.AppendBlob(memoryStream, fileMetaData);

                var fileBlob = new FileUploadResult
                {
                    uploaded = fileMetaData.TotalChunks - 1 <= fileMetaData.ChunkIndex,
                    fileUid = fileMetaData.UploadUid
                };
                return Json(fileBlob);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                throw ex;
            }
        }

        #endregion

    }
}
