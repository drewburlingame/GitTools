using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BbGit.Framework;
using MoreLinq;
using SharpBucket.V2;
using SharpBucket.V2.Pocos;

namespace BbGit
{
    public class BbService
    {
        private readonly PipedInput pipedInput;

        private readonly SharpBucketV2 bbApi;

        private readonly AppConfig appConfig;

        public BbService(SharpBucketV2 bbApi, AppConfig appConfig, PipedInput pipedInput)
        {
            this.pipedInput = pipedInput;
            this.bbApi = bbApi;
            this.appConfig = appConfig;
        }

        public IEnumerable<Repository> GetRepos(string projects = null, bool usePipedValuesIfAvailable = false)
        {
            var repositories = bbApi.RepositoriesEndPoint()
                .ListRepositories(appConfig.DefaultAccount)
                .OrderBy(r => r.name)
                .AsEnumerable();

            if (projects != null)
            {
                repositories = repositories.Where(r =>
                    Regex.IsMatch(r.project.key, $"^{projects}$".Replace(",", "$|^"), RegexOptions.IgnoreCase));
            }

            if (usePipedValuesIfAvailable && this.pipedInput.HasValues)
            {
                var set = this.pipedInput.Values.ToHashSet();
                repositories = repositories.Where(r => set.Contains(r.name));
            }

            return repositories.ToList();
        }
    }
}