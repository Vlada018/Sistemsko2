using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SysProg.Services;
using SysProg.Utils;

namespace SysProg
{
    public class Program
    {
        static ApiService apiService;

        static async Task Main(string[] args)
        {
            // API key is valid for 1 hour
            apiService = new ApiService("https://api.spotify.com/v1/search", "BQBWyGgXtaKldelYJHLszYGdzs_l0HWzu4yRFWcjTfzgThd2wGPBK9WdQLdDr_E33lLUVYH6lnP0n6ucqxMxIZyPre-Ok0ztE9aBTO29mASaCFXgR0E");

            string basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\.."));
            string folderPath = Path.Combine(basePath, "fajlovi");

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:5000/");
            listener.Start();
            Console.WriteLine("Listening...");

            while (true)
            {
                try
                {
                    HttpListenerContext context = await listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequestAsync(context, folderPath));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Listener error: {ex.Message}");
                }
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
                    byte[] errorBytes = Encoding.UTF8.GetBytes("Parameters are not valid!");
                    await response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
                    response.OutputStream.Close();
                    return;
                }

                var queries = new List<string>(queriesParam.Split(',')).Where(q => !string.IsNullOrWhiteSpace(q) && IsValidParameter(q)).ToList();
                var types = new List<string>(typesParam.Split(',')).Where(t => !string.IsNullOrWhiteSpace(t) && IsValidParameter(t)).ToList();

                if (queries.Count == 0 || types.Count == 0)
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    byte[] errorBytes = Encoding.UTF8.GetBytes("Parameters are not valid! Check that the parameters are not special characters!");
                    await response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
                    response.OutputStream.Close();
                    return;
                }

                var results = await apiService.FetchDataForQueriesAsync(queries, types);

                string resultContent = JArray.FromObject(results).ToString();
                byte[] buffer = Encoding.UTF8.GetBytes(resultContent);

                response.ContentType = "application/json";
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);

                await FileUtil.WriteResultsToFileAsync(folderPath, results, queries, types);
                response.OutputStream.Close();
                Console.WriteLine("Request processed successfully.");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Request error: {ex.Message}");
                byte[] errorBytes = Encoding.UTF8.GetBytes("API returned an error!");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await context.Response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
                context.Response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error: {ex.Message}");
                byte[] errorBytes = Encoding.UTF8.GetBytes("An unknown error occurred!");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await context.Response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
                context.Response.OutputStream.Close();
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
