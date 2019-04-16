using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Gremlin.Net.Driver;
using System.Linq;
using Gremlin.Net.Structure.IO.GraphSON;
using System.Collections.Generic;
using Gremlin.Net.Driver.Exceptions;
using GlobalAzure2019.AddVertices.Helper;

namespace GlobalAzure2019.AddVertices.Functions
{
    public static class AddVerticesFunction
    {
        private static string hostname = "globalazure2019.gremlin.cosmos.azure.com";
        private static int port = 443;
        private static string authKey = "Gfs8AD48oGuiP0EwMcGcrb3LT3JsSHNAzSHArIKkFNKcxf4hYdNXrHRoDyagxKxlYgDe3JQaku2qF2NpcNQb1g==";
        private static string database = "eCommerce";
        private static string collection = "global";

        private static List<string> gremlinQueries = new List<string>();

        private static void AddGremlinQueries(IEnumerable<string> lines, string edgeType)
        {
            var linesList = lines.ToList();
            linesList.RemoveAt(0);

            var gremlinQueriesTyped = linesList.Select(line =>
            {
                var parts = line.Split("|");
                return $"g.V('{parts[0]}').addE('{edgeType}').to(g.V('{parts[1]}'))"; //g.V().hasLabel('customer').has('id', '10000000-0000-0000-0000-000000000001')
            });
            gremlinQueries.AddRange(gremlinQueriesTyped);
        }

        private static void AddAddressGremlinQueries(IEnumerable<string> lines)
        {
            var linesList = lines.ToList();
            linesList.RemoveAt(0);

            var gremlinQueriesTyped = linesList.Select(line =>
            {
                var parts = line.Split("|");
                return $"g.V().hasLabel('customer').has('id', '{parts[0]}').addE('hasAddress').to(g.V().hasLabel('address').has('id', '{parts[1]}'))"; //g.V().hasLabel('customer').has('id', '10000000-0000-0000-0000-000000000001')
            });
            gremlinQueries.AddRange(gremlinQueriesTyped);
        }

        [FunctionName("AddVertices")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            log.LogInformation("C# HTTP trigger function processed a request.");

            var files = Directory.GetFiles("data");

            foreach (var file in files)
            {
                var lines = File.ReadAllLines(file);
                var filename = Path.GetFileNameWithoutExtension(file);
                if (filename == "hasAddress")
                {
                    AddAddressGremlinQueries(lines);
                }
                else
                {
                    AddGremlinQueries(lines, filename.Remove(filename.Length - 1, 1));
                }
            }

            using (var gremlinClient = GremlinQueryHelper.GetGremlinClient())
            {
                foreach (var query in gremlinQueries)
                {
                    // Create async task to execute the Gremlin query.
                    var resultSet = GremlinQueryHelper.SubmitRequest(gremlinClient, query, log).Result;
                    if (resultSet.Count > 0)
                    {
                        log.LogInformation("\tResult:");
                        foreach (var result in resultSet)
                        {
                            // The vertex results are formed as Dictionaries with a nested dictionary for their properties
                            string output = JsonConvert.SerializeObject(result);
                            log.LogInformation($"\t{output}");
                        }
                        log.LogInformation(Environment.NewLine);
                    }

                    // Print the status attributes for the result set.
                    // This includes the following:
                    //  x-ms-status-code            : This is the sub-status code which is specific to Cosmos DB.
                    //  x-ms-total-request-charge   : The total request units charged for processing a request.
                    GremlinQueryHelper.PrintStatusAttributes(log, resultSet.StatusAttributes);
                    log.LogInformation(Environment.NewLine);
                }
            }

            return new OkObjectResult("OK");
        }

    }
}
