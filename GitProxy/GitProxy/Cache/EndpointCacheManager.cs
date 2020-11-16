using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitProxy.Aggregation;
using GitProxy.GitModel;
using GitProxy.RequestHandler;
using Newtonsoft.Json.Linq;

namespace GitProxy.Cache
{
    public class EndpointCacheManager : IEndpointCacheManager
    {
        private readonly IApiViewManager apiViewManager;

        // TODO: Implement LRU eviction policy
        private ConcurrentDictionary<string, GitResponse> resourceCache;

        public EndpointCacheManager(
            HashSet<string> cachableResources,
            IApiViewManager apiViewManager)
        {
            // Validate params and throw

            this.CachableResources = cachableResources;
            this.apiViewManager = apiViewManager;
            this.resourceCache = new ConcurrentDictionary<string, GitResponse>();
        }

        public HashSet<string> CachableResources { get; }

        public bool TryGetResourceFromCache(string resourcePath, out GitResponse resource)
        {
            if (!CachableResources.Contains(resourcePath))
            {
                throw new InvalidOperationException($"Resource {resourcePath} cannot be cached");
            }

            if (resourceCache.TryGetValue(resourcePath, out resource))
            {
                return true;
            }

            return false;
        }

        public void UpsertResource(string resourcePath, GitResponse newResource)
        {
            if(!CachableResources.Contains(resourcePath))
            {
                throw new InvalidOperationException($"Resource {resourcePath} cannot be cached");
            }

            resourceCache.AddOrUpdate(resourcePath, newResource, (key, oldValue) => newResource);

            this.apiViewManager.UpdateView(resourcePath, newResource);
        }
    }
}
