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
    public static class HighlyUsageDeviceIdFunction
    {
        [FunctionName("HighlyUsageDeviceIdFunction")]
        public static bool HighlyUsageDeviceId([ActivityTrigger] string orderId, ILogger log)
        {
            using (var gremlinClient = GremlinQueryHelper.GetGremlinClient())
            {
                // User registers and places an order with highly used device id

                /// Event: Registration - unique customer name, address, email, etc. 
                /// Event: Session - same customer id as registration, unique device id, unique IP address
                /// Event: Registration 1 day later -unique customer name, address, email, etc. 
                /// Event: Session - same customer id as registration, device id seen on 1 other customer registrations, IP address seen on 1 other customer registrations
                /// Event: Registration 1 day later -unique customer name, address, email, etc. 
                /// Event: Session - same customer id as registration, device id seen on 2 other customer registrations, IP address seen on 2 other customer registrations
                /// Event: Registration 1 day later -unique customer name, address, email, etc. 
                /// Event: Session - same customer id as registration, device id seen on 3 other customer registrations, IP address seen on 3 other customer registrations
                /// Event: Registration 1 day later -unique customer name, address, email, etc. 
                /// Event: Session - same customer id as registration, device id seen on 4 other customer registrations, IP address seen on 4 other customer registrations
                /// Event: Registration 1 day later -unique customer name, address, email, etc. 
                /// Event: Session - same customer id as registration, device id seen on 5 other customer registrations, IP address seen on 5 other customer registrations
                /// Event: Registration 1 day later -unique customer name, address, email, etc. 
                /// Event: Session - same customer id as registration, device id seen on 6 other customer registrations, IP address seen on 6 other customer registrations
                /// Event: Registration - unique customer name, address, email, etc. 
                /// Event: Session - same customer id as registration, device id seen on 7 other customer registrations, IP address seen on 7 other customer registrations
                /// Event: Registration 1 day later -unique customer name, address, email, etc. 
                /// Event: Session - same customer id as registration, device id seen on 8 other customer registrations, IP address seen on 8 other customer registrations
                /// Event: Registration - unique customer name, address, email, etc. 
                /// Event: Session - same customer id as registration, device id seen on 9 other customer registrations, IP address seen on 9 other customer registrations 
                /// Event: Order - unique credit card, same customer id as above registration - order is declined

                ///
                /// Fraud :(
                ///

                var degrees = 3;
                var myOrder = "40000000-0000-0000-0003-000000000003";
                var deviceIdQuery = $"g.V(['order', '{myOrder}']).values('deviceid')";

                var deviceId = GremlinQueryHelper.SubmitRequest(gremlinClient, deviceIdQuery, log).Result.Cast<string>().FirstOrDefault();
                if (deviceId != null)
                {
                    var subgraph = $"g.V().has('device', 'deviceid', '{deviceId}').repeat(__.bothE('using', 'customerSession').subgraph('sg').otherV().simplePath()).times({degrees}).cap('sg').V().hasLabel('customer').count()";

                    var usersPerDevice = GremlinQueryHelper.SubmitRequest(gremlinClient, subgraph, log).Result.Cast<long>().FirstOrDefault();

                    if (usersPerDevice > 0)
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