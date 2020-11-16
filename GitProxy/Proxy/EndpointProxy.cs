using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitProxy.Cache;
using GitProxy.GitModel;
using GitProxy.RequestHandler;
using Newtonsoft.Json.Linq;

namespace GitProxy.Proxy
{
    public class EndpointProxy : IEndpointProxy
    {
        private readonly IRequestHandler resourceRequestHandler;
        private readonly IEndpointCacheManager cacheManager;
        private readonly TimeSpan iterationRefreshDelay;
        private readonly TimeSpan resourceGapDelay;
        private Task refreshLoop;

        public EndpointProxy(
            IRequestHandler resourceRequestHandler,
            IEndpointCacheManager cacheManager,
            TimeSpan iterationRefreshDelay,
            TimeSpan resourceGapDelay)
        {
            this.resourceRequestHandler = resourceRequestHandler;
            this.cacheManager = cacheManager;
            this.iterationRefreshDelay = iterationRefreshDelay;
            this.resourceGapDelay = resourceGapDelay;
            this.refreshLoop = RefreshLoop();
        }

        public async Task<GitResponse> Get(string resourcePath)
        {
            if(!this.cacheManager.CachableResources.Contains(resourcePath))
            {
                // This call is expected to be forwarded to destination, without caching
                return await this.resourceRequestHandler.Get(resourcePath);
            }
            else
            {
                var latestResourceValue = await this.HandleResourceUpdateGetLatest(resourcePath);

                return latestResourceValue;
            }
        }

        private async Task RefreshLoop()
        {
            // We should have a cancellation token that gets triggered on dispose
            while (true)
            {
                foreach (var resource in this.cacheManager.CachableResources)
                {
                    await this.HandleResourceUpdateGetLatest(resource);

                    await Task.Delay(this.resourceGapDelay);
                }

                await Task.Delay(this.iterationRefreshDelay);
            }
        }

        private async Task<GitResponse> HandleResourceUpdateGetLatest(string resourcePath)
        {
            if (!this.cacheManager.TryGetResourceFromCache(resourcePath, out GitResponse resourceCacheValue) // If the resource is not in the cache
                || await resourceRequestHandler.HasChanged(resourcePath, resourceCacheValue)) // If the ETag has changed
            {
                // Write-through to in-memory cache, if the resource is new or has changed
                var newValue = await resourceRequestHandler.Get(resourcePath);
                this.cacheManager.UpsertResource(resourcePath, newValue);

                return newValue;
            }

            return resourceCacheValue;
        }

        // Implement dispose pattern and cancel the refresh loop
        // public bool Dispose()
        // public bool Dispose(bool isDisposing)
    }
}
