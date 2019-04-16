using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using GlobalAzure2019.AddVertices.Helper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GlobalAzure2019.AddVertices.Functions
{
    public static class UserRegisterAndPlacesAndOrderFunction
    {
        [FunctionName("UserRegisterAndPlacesAndOrderFunction")]
        public static bool UserRegisterAndPlacesAndOrder([ActivityTrigger] string orderId, ILogger log)
        {
            using (var gremlinClient = GremlinQueryHelper.GetGremlinClient())
            {
                // User registers and eventually places a order

                /// Event: Registration - unique customer name, address, email, etc. 
                /// Event: Session - same customer id as registration, unique device id, IP address
                /// User logs in 1 day later and changes information 
                /// Event: Session-- same customer id, device id and IP address as previous
                /// User logs in 1 day later from mobile device for some other legit reason 
                /// Event: Session-- same customer id, different device id, different IP address
                /// User logs in 1 day later and places an order 
                /// Event: Session-- same customer id, same device id as first login, different IP address 
                /// Event: Order - unique credit card - order is approved
                
                /// 
                /// Legitime :)
                /// 

                var query = "g.V().has('customer', 'id', '10000000-0000-0000-0000-000000000001').emit().repeat(both().simplePath()).times(4)";

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

            log.LogInformation($"Legitime - :) Order:'{orderId}'.");
            return false;
        }

    }
}