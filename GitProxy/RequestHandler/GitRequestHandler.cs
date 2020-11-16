using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GitProxy.GitModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitProxy.RequestHandler
{
    public class GitRequestHandler : IRequestHandler
    {
        private readonly string gitAuthToken;
        private DateTime throttledTill = DateTime.MinValue;

        /// <summary>
        /// Get's the base endpoint URI for this request handler
        /// </summary>
        public Uri BaseEndpointUri => throw new NotImplementedException();

        public GitRequestHandler(string gitAuthToken)
        {
            this.gitAuthToken = gitAuthToken;
        }

        /// <summary>
        /// Forwards a GET request to the underlying endpoint at the relative URI location.
        /// Retry and Pagination are taken care of by the handler implementation
        /// </summary>
        /// <param name="resourceUri">The resource URI relative to the Base URI</param>
        public async Task<GitResponse> Get(string resourcePath)
        {
           var client = GetHttpClient();

            GitResponse gitResponse = null;
            JToken mergedJsonResult = null;
            JToken currentParseResult = null;
            List<string> etags = new List<string>();
            var isPaged = false;
            var pageIndex = 1;

            do
            {
                var nextPageResourcePath = this.GetPagedResourcePath(resourcePath, pageIndex);
                var response = await this.SendRequest(this.throttledTill, client, nextPageResourcePath);

                response.EnsureSuccessStatusCode();

                var responseStream = await response.Content.ReadAsStreamAsync();
                string json = new StreamReader(responseStream).ReadToEnd();
                currentParseResult = JToken.Parse(json);
                
                if(response.Headers.TryGetValues("ETag", out IEnumerable<string> etag)
                    && etag != null && etag.Any())
                {
                    etags.Add(etag.ElementAt(0));
                }

                if(mergedJsonResult == null)
                {
                    mergedJsonResult = currentParseResult;

                    if(currentParseResult.GetType() == typeof(JArray))
                    {
                        // This api could potentially return paginated results
                        isPaged = true;
                    }
                }
                else
                {
                    ((JArray)mergedJsonResult).Merge((JArray)currentParseResult);
                }

                pageIndex++;
            }
            while (this.HasNext(currentParseResult));

            if(isPaged)
            {
                gitResponse = new GitResponseMultiPage
                {
                    JsonResult = mergedJsonResult,
                    PageETags = etags
                };
            }
            else
            {
                gitResponse = new GitResponseSinglePage
                {
                    JsonResult = mergedJsonResult,
                    ETag = etags[0]
                };
            }

            return gitResponse;
        }

        /// <summary>
        /// Returns a flag indicating if the resource has changed
        /// </summary>
        /// <param name="resourceUri">The relative resource path for the resource to be verified</param>
        /// <returns>A flag indicating if the resource has changed</returns>
        public async Task<bool> HasChanged(string resourcePath, GitResponse oldResourceValue)
        {
            if(oldResourceValue == null)
            {
                return true;
            }

            var hasChanged = false;
            List<string> etags;

            if (oldResourceValue is GitResponseSinglePage)
            {
                etags = new List<string> { ((GitResponseSinglePage)oldResourceValue).ETag};
            }
            else
            {
                etags = ((GitResponseMultiPage)oldResourceValue).PageETags;
            }

            // Validate that none of the etags has changed
            var client = GetHttpClient();
            var pageIndex = 1;
            foreach (var etag in etags)
            {
                client.DefaultRequestHeaders.Add("If-None-Match", etag);

                var nextResourcePage = GetPagedResourcePath(resourcePath, pageIndex);
                var response = await this.SendRequest(this.throttledTill, client, nextResourcePage);

                if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.NotModified)
                {
                    // OK means that the resource has been modified
                    if(response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        hasChanged = true;
                        break;
                    }
                }
                else
                {
                    throw new InvalidOperationException("Failed HTTP request");
                }

                pageIndex++;
            }
           
            // If this is a paginated result, we also want to check if a new page has exists
            if(oldResourceValue is GitResponseMultiPage)
            {
                client = GetHttpClient();

                // access the next page
                var lastPage = GetPagedResourcePath(resourcePath, pageIndex);
                var response = await this.SendRequest(this.throttledTill, client, lastPage);
                var responseStream = await response.Content.ReadAsStreamAsync();
                string json = new StreamReader(responseStream).ReadToEnd();
                var lastPageResult = JToken.Parse(json);

                // If we found a new extra page, then we deem the resource as changed and refresh everything
                // TODO: We could optimize and refresh only those pages that have changed
                return lastPageResult == null || lastPageResult.HasValues;
            }

            return hasChanged;
        }

        // TODO: Implement dispose pattern and cancel the refresh loop
        // public bool Dispose()
        // public bool Dispose(bool isDisposing)
        
        private HttpClient GetHttpClient()
        {
            var client = new HttpClient();

            client.BaseAddress = new Uri("https://api.github.com/");

            // GitHub API versioning
            client.DefaultRequestHeaders.Add("Accept",
                "application/vnd.github.v3+json");

            // GitHub requires a user-agent
            client.DefaultRequestHeaders.Add("User-Agent",
                "HttpClientFactory-Sample");

            // Pass token
            client.DefaultRequestHeaders.Add("Authorization", $"token {this.gitAuthToken}");

            return client;
        }

        private bool HasNext(JToken currentResult)
        {
            if(currentResult.GetType() == typeof(JArray)
                && ((JArray) currentResult).Count>0)
            {
                // For some reason the Link metadata does not arrive in the request header.
                // Hence we will execute one extra request after the last page, which will return as empty.
                return true;
            }

            return false;
        }

        private string GetPagedResourcePath(string resourcePath, int pageCount)
        {
            // This works for non-paginated resources as well, like orgs/Netflix
            return $"{resourcePath}?page_size=30&page={pageCount}";
        }

        private async Task<HttpResponseMessage> SendRequest(DateTime throttledTill, HttpClient client, string resourcePath)
        {
            if(DateTime.UtcNow < throttledTill)
            {
                throw new InvalidOperationException($"Git API is being throttled until {throttledTill}");
            }

            var result = await client.GetAsync(resourcePath);

            if(result.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                var remainingCalls = result.Headers.GetValues("X-RateLimit-Remaining").ElementAt(0);

                if(remainingCalls == "0")
                {
                    var resetThrottle = Convert.ToInt32(result.Headers.GetValues("X-RateLimit-Reset").ElementAt(0));
                    this.throttledTill = new DateTime(resetThrottle * 1000);
                }
            }

            return result;
        }

    }
}
