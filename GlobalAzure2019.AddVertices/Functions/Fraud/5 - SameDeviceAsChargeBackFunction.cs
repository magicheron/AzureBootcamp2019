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
using System.Linq;

namespace GlobalAzure2019.AddVertices.Functions
{
    public static class SameDeviceAsChargeBackFunction
    {
        [FunctionName("SameDeviceAsChargeBackFunction")]
        public static bool SameDeviceAsChargeBack([ActivityTrigger] string orderId, ILogger log)
        {
            using (var gremlinClient = GremlinQueryHelper.GetGremlinClient())
            {
                // Order placed using the same device as an order which resulted in a chargeback

                /// Event: Registration - unique customer name, address, email, etc. 
                /// Event: Session - same customer id as registration, unique device id, IP address
                /// Event: Session 1 day later -same customer id as above registration, unique(different) device id, unique (different) IP address 
                /// Event: Order - unique credit card -order is approved
                /// Event: Chargeback 90 days later -matched to previous order & credit card
                /// Event: Registration 1 day later -unique customer name, address, email, etc. 
                /// Event: Session - same customer id as above registration, same device id as customer's initial session, unique IP address 
                /// Event: Order - device id linked to a customer which is linked to a chargeback - order is declined

                ///
                /// Fraud :(
                ///

                var degrees = 3;
                var myOrder = "40000000-0000-0000-0025-000000000004";
                var deviceIdQuery = $"g.V(['order', '{myOrder}']).values('deviceid')";

                var deviceId = GremlinQueryHelper.SubmitRequest(gremlinClient, deviceIdQuery, log).Result.Cast<string>().FirstOrDefault();
                if (deviceId != null)
                {
                    var subgraph = $"g.V().has('device', 'deviceid', '{deviceId}').repeat(__.bothE().subgraph('sg').otherV().simplePath()).times({degrees}).cap('sg').V().hasLabel('chargeback').count()";

                    var numChargebacksByMachine = GremlinQueryHelper.SubmitRequest(gremlinClient, subgraph, log).Result.Cast<long>().FirstOrDefault();

                    if (numChargebacksByMachine > 0)
                    {
                        log.LogError($"Fraud - :( Device: {deviceId}");
                        log.LogError($"Fraud - :( Order:'{orderId}'.");
                        return true;
                    }
                }

                log.LogInformation(Environment.NewLine);

            }

            log.LogInformation($"Legitime - :) Order:'{orderId}'.");
            return false;

        }

    }
}