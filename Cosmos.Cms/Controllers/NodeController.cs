using Jering.Javascript.NodeJS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using NuGet.ContentModel;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;

namespace Cosmos.Cms.Controllers
{
    /// <summary>
    /// API Controller
    /// </summary>
    [AllowAnonymous]
    public class NodeController : ControllerBase
    {

        private readonly INodeJSService _nodeJSService;
        public NodeController(INodeJSService nodeJSService)
        {
            _nodeJSService = nodeJSService;
        }

        [HttpGet]
        public async Task<string> HelloWorld()
        {
            var args = JsonConvert.SerializeObject(Request.Query.Select(s => new
            {
                s.Key,
                s.Value
            }));

            var js = "module.exports = async (arg1) => { return \"Hello World\"; }";
            var result = await _nodeJSService.InvokeFromStringAsync<string>(js, args: new[] { args });
            return result;
        }

        [HttpGet]
        public async Task<string> Call(string id)
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

            return result;
        }
    }
}
