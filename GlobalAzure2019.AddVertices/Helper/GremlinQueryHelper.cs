using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Exceptions;
using Gremlin.Net.Structure.IO.GraphSON;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GlobalAzure2019.AddVertices.Helper
{
    public static class GremlinQueryHelper
    {
        private static string hostname = "globalazure2019.gremlin.cosmos.azure.com";
        private static int port = 443;
        private static string authKey = "Gfs8AD48oGuiP0EwMcGcrb3LT3JsSHNAzSHArIKkFNKcxf4hYdNXrHRoDyagxKxlYgDe3JQaku2qF2NpcNQb1g==";
        private static string database = "eCommerce";
        private static string collection = "global";

        internal static GremlinClient GetGremlinClient()
        {
            var gremlinServer = new GremlinServer(hostname, port, enableSsl: true,
                                        username: "/dbs/" + database + "/colls/" + collection,
                                        password: authKey);

            return new GremlinClient(gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType);
        }

        internal static Task<ResultSet<dynamic>> SubmitRequest(GremlinClient gremlinClient, string query, ILogger log)
        {
            try
            {
                return gremlinClient.SubmitAsync<dynamic>(query);
            }
            catch (ResponseException e)
            {
                Console.WriteLine("\tRequest Error!");

                // Print the Gremlin status code.
                Console.WriteLine($"\tStatusCode: {e.StatusCode}");

                // On error, ResponseException.StatusAttributes will include the common StatusAttributes for successful requests, as well as
                // additional attributes for retry handling and diagnostics.
                // These include:
                //  x-ms-retry-after-ms         : The number of milliseconds to wait to retry the operation after an initial operation was throttled. This will be populated when
                //                              : attribute 'x-ms-status-code' returns 429.
                //  x-ms-activity-id            : Represents a unique identifier for the operation. Commonly used for troubleshooting purposes.
                PrintStatusAttributes(log, e.StatusAttributes);
                Console.WriteLine($"\t[\"x-ms-retry-after-ms\"] : { GetValueAsString(e.StatusAttributes, "x-ms-retry-after-ms")}");
                Console.WriteLine($"\t[\"x-ms-activity-id\"] : { GetValueAsString(e.StatusAttributes, "x-ms-activity-id")}");

                throw;
            }
        }

        internal static void PrintStatusAttributes(ILogger log, IReadOnlyDictionary<string, object> attributes)
        {
            log.LogInformation($"\tStatusAttributes:");
            log.LogInformation($"\t[\"x-ms-status-code\"] : { GetValueAsString(attributes, "x-ms-status-code")}");
            log.LogInformation($"\t[\"x-ms-total-request-charge\"] : { GetValueAsString(attributes, "x-ms-total-request-charge")}");
        }

        internal static string GetValueAsString(IReadOnlyDictionary<string, object> dictionary, string key)
        {
            return JsonConvert.SerializeObject(GetValueOrDefault(dictionary, key));
        }

        internal static object GetValueOrDefault(IReadOnlyDictionary<string, object> dictionary, string key)
        {
            if (dictionary.ContainsKey(key))
            {
                return dictionary[key];
            }

            return null;
        }

    }
}
