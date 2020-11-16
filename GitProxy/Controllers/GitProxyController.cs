using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitProxy.GitModel;
using GitProxy.Proxy;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace GitProxy.Controllers
{
    
    public class GitProxyController : Controller
    {
        private readonly IEndpointProxy gitEndpointProxy;

        public GitProxyController(IEndpointProxy gitEndpointProxy)
        {
            this.gitEndpointProxy = gitEndpointProxy;
        }

         // GET: /
        [HttpGet]
        public async Task<JToken> CatchAll()
        {
            var result = await gitEndpointProxy.Get(SanitizePath(this.Request.Path));

            return result.JsonResult;
        }

        private string SanitizePath(string resourcePath)
        {
            var path = resourcePath.Trim(new char[] { ' ', '/' }).ToLower();
            return path;
        }


    }
}
