using GitProxy.GitModel;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitProxy.RequestHandler
{
    public interface IRequestHandler
    {
        /// <summary>
        /// Get's the base endpoint URI for this request handler
        /// </summary>
        Uri BaseEndpointUri { get; }

        /// <summary>
        /// Forwards a GET request to the underlying endpoint at the relative URI location.
        /// Retry and Pagination are taken care of by the handler implementation
        /// </summary>
        /// <param name="resourcePath">The resource URI relative to the Base URI</param>
        Task<GitResponse> Get(string resourcePath);

        /// <summary>
        /// Returns a flag indicating if the resource has changed
        /// </summary>
        /// <param name="resourcePath">The relative resource path for the resource to be verified</param>
        /// <returns>A flag indicating if the resource has changed</returns>
        Task<bool> HasChanged(string resourcePath, GitResponse oldResourceValue);
    }
}
