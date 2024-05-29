using SysProg.Services;
using SysProg.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text.RegularExpressions;

namespace SysProg
{
    public class Program
    {
        static ApiService apiService;

        static async Task Main(string[] args)
        {
            //kljuc je validan 1h
            apiService = new ApiService("https://api.spotify.com/v1/search", "BQAUP7yV0nV2YUdmIu0DavHkOJhmSzLFwsUPjwzGJkCM3eKGYx9kFTA2fEnaSPP3XcP9hwPPUfehHQRTpeGD7pTVjU5gNgNjlRVshpaOE8R5Occky2s");

            string basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\.."));
            string folderPath = Path.Combine(basePath, "fajlovi");

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:5000/");
            listener.Start();
            Console.WriteLine("Listening...");

            while (true)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                _ = Task.Run(() => HandleRequestAsync(context, folderPath));
            }
        }

        static async Task HandleRequestAsync(HttpListenerContext context, string folderPath)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                var request = context.Request;
                var response = context.Response;

                string queriesParam = request.QueryString["queries"];
                string typesParam = request.QueryString["types"];

                if (string.IsNullOrEmpty(queriesParam) || string.IsNullOrEmpty(typesParam))
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    byte[] errorBytes = Encoding.UTF8.GetBytes("Parametri nisu validni!");
                    await response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
                }
                else
                {
                    var queries = new List<string>(queriesParam.Split(',')).Where(q => !string.IsNullOrWhiteSpace(q) && IsValidParameter(q)).ToList();
                    var types = new List<string>(typesParam.Split(',')).Where(t => !string.IsNullOrWhiteSpace(t) && IsValidParameter(t)).ToList();

                    if (queries.Count == 0 || types.Count == 0)
                    {
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        byte[] errorBytes = Encoding.UTF8.GetBytes("Parametri nisu validni! Proverite da parametri nisu specijalni znakovi!");
                        await response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
                    }
                    else
                    {
                        var results = await apiService.FetchDataForQueriesAsync(queries, types);

                        string resultContent = JArray.FromObject(results).ToString();
                        byte[] buffer = Encoding.UTF8.GetBytes(resultContent);

                        response.ContentType = "application/json";
                        response.ContentLength64 = buffer.Length;
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);

                        await FileUtil.WriteResultsToFileAsync(folderPath, results, queries, types);
                    }
                }

                response.OutputStream.Close();
                Console.WriteLine("Request processed successfully.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }

            stopwatch.Stop();
            Console.WriteLine($"Time taken: {stopwatch.Elapsed}");
        }

        static bool IsValidParameter(string param)
        {
            return Regex.IsMatch(param, @"^[a-zA-Z0-9 ]+$");
        }
    }
}
