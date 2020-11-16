using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitProxy.Aggregation;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace GitProxy.Controllers
{
    public class ViewController : Controller
    {
        private readonly IApiViewManager gitViewManager;

        public ViewController(IApiViewManager gitViewManager)
        {
            this.gitViewManager = gitViewManager;
        }

        // GET: api/<controller>
        [HttpGet]
        public JToken ViewTop(int count, string dimension)
        {
            var result = gitViewManager.GetTopN(dimension, count);

            if(result == null)
            {
                return null;
            }

            var resultArray = new JArray();
            foreach (var item in result)
            {
                resultArray.Add(item);
            }

            return resultArray;
        }
    }
}
