using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SysProg.Services
{
    public class ApiService
    {
        private readonly string baseUrl;
        private readonly string apiKey;
        private readonly HttpClient client;
        private static ConcurrentDictionary<string, List<JObject>> cache = new ConcurrentDictionary<string, List<JObject>>();

        public ApiService(string baseUrl, string apiKey)
        {
            this.baseUrl = baseUrl;
            this.apiKey = apiKey;
            client = new HttpClient();
        }

        public async Task<List<JObject>> FetchDataForQueriesAsync(List<string> queries, List<string> types)
        {
            string cacheKey = $"{string.Join(",", queries)}-{string.Join(",", types)}";
            if (cache.TryGetValue(cacheKey, out var cachedResults))
            {
                Console.WriteLine("Cache hit.");
                return cachedResults;
            }

            Console.WriteLine("Cache miss.");
            var client = new HttpClient();
            var joinedtypes = string.Join(",", types);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var tasks = queries.Select(async query =>
            {
                string url = $"{baseUrl}?q={Uri.EscapeDataString(query)}&type={joinedtypes}";
                var response = await client.GetAsync(url);
                var resBody = await response.Content.ReadAsStringAsync();
                return JObject.Parse(resBody);
            }).ToList();

            var results = await Task.WhenAll(tasks);

            cache[cacheKey] = results.ToList();
            return results.ToList();
        }
    }
}
