using GitProxy.GitModel;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitProxy.Aggregation
{
    public interface IApiViewManager
    {
        ///// <summary>
        ///// The API that is being aggregated (i.e. orgs/Netflix/repos)
        ///// </summary>
        //string AggregatedApi { get; }

        ///// <summary>
        ///// The Dimensions on which this View manager can aggregate (i.e. forks, last_updated etc.)
        ///// </summary>
        //string Dimensions { get; }

        JToken[] GetTopN(string dimension, int count);

        void UpdateView(string resourcePath, GitResponse resourceValue);
    }
}
