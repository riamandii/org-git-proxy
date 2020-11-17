using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitProxy.GitModel;
using Newtonsoft.Json.Linq;

namespace GitProxy.Aggregation
{
    public class GitApiViewManager : IApiViewManager
    {
        public GitApiViewManager(string orgName)
        {
            this.orgName = orgName;
        }

        private ConcurrentDictionary<string, JToken[]> resourceCache = new ConcurrentDictionary<string, JToken[]>();
        private readonly string orgName;

        public JToken[] GetTopN(string dimension, int count)
        {
            if(!resourceCache.ContainsKey(dimension))
            {
                return null;
            }

            switch(dimension)
            {
                case "forks":
                case "open_issues":
                case "last_updated":
                    return resourceCache[dimension].Take(count).ToArray();
                default:
                    throw new InvalidOperationException($"Dimension {dimension} not supported by view aggregator");
            }
        }

        public void UpdateView(string resourcePath, GitResponse resourceValue)
        {
            // TODO: do not hardcode, also move this validation in a more generic base class
            if(resourcePath != "orgs/netflix/repos" || !(resourceValue is GitResponseMultiPage))
            {
                // We support caching for results coming from this api, that are of type multipage (array of Jsons)
                return;
            }

            var multiPageResult = resourceValue as GitResponseMultiPage;
            UpdateForksView(multiPageResult);
            UpdateUpdatedTimeView(multiPageResult);
            UpdateIssuesView(multiPageResult);
        }

        private void UpdateForksView(GitResponseMultiPage multiPageResponse)
        {
            var byForks = ((JArray)multiPageResponse.JsonResult).Children().ToList().OrderByDescending(x => Convert.ToInt32(x["forks_count"].ToString()))
                .Select(x=> GetTokenValue(x["name"].ToString(), x["forks_count"].ToString())).ToArray();
            this.resourceCache.AddOrUpdate("forks", byForks, (key, oldvalue) => byForks);
        }

        private void UpdateUpdatedTimeView(GitResponseMultiPage multiPageResponse)
        {
            var byForks = ((JArray)multiPageResponse.JsonResult).Children().ToList().OrderByDescending(x => DateTime.Parse(x["updated_at"].ToString()))
                .Select(x => GetTokenValue(x["name"].ToString(), x["updated_at"].ToString())).ToArray();
            this.resourceCache.AddOrUpdate("last_updated", byForks, (key, oldvalue) => byForks);
        }

        private void UpdateIssuesView(GitResponseMultiPage multiPageResponse)
        {
            var byForks = ((JArray)multiPageResponse.JsonResult).Children().ToList().OrderByDescending(x => Convert.ToInt32(x["open_issues_count"].ToString()))
                .Select(x => GetTokenValue(x["name"].ToString(), x["open_issues_count"].ToString())).ToArray();
            this.resourceCache.AddOrUpdate("open_issues", byForks, (key, oldvalue) => byForks);
        }

        private JToken GetTokenValue(string name, string value)
        {
            return JToken.Parse($"[\"{this.orgName}/{name}\",\"{value}\"]");
        }
    }
}
