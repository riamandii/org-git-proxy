using GitProxy.GitModel;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitProxy.Proxy
{
    public interface IEndpointProxy
    {
        Task<GitResponse> Get(string resourcePath);
    }
}
