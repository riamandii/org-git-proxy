using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitProxy.GitModel
{
    public abstract class GitResponse
    {
        public JToken JsonResult { get; set; }
    }

    public class GitResponseSinglePage : GitResponse
    {
        public string ETag { get; set; }
    }

    public class GitResponseMultiPage : GitResponse
    {
        // The index acts as the page count;
        public List<string> PageETags { get; set; }
    }
}
