using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SysProg.Utils
{
    public class FileUtil
    {
        public static async Task WriteResultsToFileAsync(string basePath, List<JObject> results, List<string> queries, List<string> types)
        {
            if (results == null || results.Count == 0)
            {
                Console.WriteLine("NULL OR MISSING DATA");
                return;
            }

            string filePath = Path.Combine(basePath, "query_result.txt");

            try
            {
                using (StreamWriter sw = new StreamWriter(filePath))
                {
                    foreach (var type in types)
                    {
                        foreach (var result in results)
                        {
                            if (result.TryGetValue(type + "s", out var typeValue))
                            {
                                var items = typeValue["items"];
                                foreach (var item in items)
                                {
                                    var content = $"Za tip: {type}\n" +
                                                  $"Result: {item}\n";
                                    await sw.WriteLineAsync(content);
                                }
                            }
                        }
                    }
                }
                Console.WriteLine("Results written to file successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}
