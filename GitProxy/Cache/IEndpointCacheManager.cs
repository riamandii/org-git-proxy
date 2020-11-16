using GitProxy.GitModel;
using GitProxy.RequestHandler;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitProxy.Cache
{
    public interface IEndpointCacheManager
    {
        /// <summary>
        /// The list of resource paths to be cached
        /// </summary>
        HashSet<string> CachableResources { get; }

        /// <summary>
        /// Gets the cached object value associated with the resource path
        /// </summary>
        /// <param name="resourcePath">The relative resource path</param>
        /// <param name="resource">The resource value, if found in the cache, or null otherwise</param>
        /// <returns>A flag indicating if the resource was found in the cache or not</returns>
        bool TryGetResourceFromCache(string resourcePath, out GitResponse resource);

        /// <summary>
        /// Updates the resource value for the resource identified by the resourcePath
        /// </summary>
        /// <param name="resourcePath">The relative resource path identifying the resource to be updated</param>
        /// <param name="newResource">The new resource value</param>
        void UpsertResource(string resourcePath, GitResponse newResource);
    }
}
