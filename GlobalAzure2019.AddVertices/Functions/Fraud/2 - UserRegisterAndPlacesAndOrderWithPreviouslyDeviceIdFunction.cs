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
    public static class UserRegisterAndPlacesAndOrderWithPreviouslyDeviceIdFunction
    {
        [FunctionName("UserRegisterAndPlacesAndOrderWithPreviouslyDeviceIdFunction")]
        public static bool UserRegisterAndPlacesAndOrderWithPreviouslyDeviceId([ActivityTrigger] string orderId, ILogger log)
        {
            using (var gremlinClient = GremlinQueryHelper.GetGremlinClient())
            {
                // User registers and places an order with previously used device id (might be husband and wife)

                /// Event: Registration - unique customer name, address, email, same physical address as another customer 
                /// Event: Session - same customer id as registration, device id seen on 1 other customer registrations (the one with the same physical address), IP address seen on 1 other customer registrations 
                /// Event: Order - unique credit card - order is approved
                
                /// 
                /// Suspicious - :|
                /// 

                // Direct connections
                var myOrder = "40000000-0000-0000-0002-000000000002";
                var myCustomerIdQuery = $"g.V().has('order', 'id', '{myOrder}').inE('customerOrder').outV().hasLabel('address').values('id')";

                var myCustomerId = GremlinQueryHelper.SubmitRequest(gremlinClient, myCustomerIdQuery, log).Result.Cast<string>().FirstOrDefault();
                if(myCustomerId != null)
                {
                    var connectionsByAddress = $"g.V().has('customer', 'id', '{myCustomerId}').outE('hasAddress').inV().inE('hasAddress').outV().dedup().count()";
                    var connectionsByAddressCount = GremlinQueryHelper.SubmitRequest(gremlinClient, connectionsByAddress, log).Result.Cast<long>().FirstOrDefault();
                    var connectionsByIp = $"g.V().has('customer', 'id', '{myCustomerId}').outE('customerSession').inV().inE('customerSession').inV().values('ipaddress').dedup().count()";
                    var connectionsByIpCount = GremlinQueryHelper.SubmitRequest(gremlinClient, connectionsByIp, log).Result.Cast<long>().FirstOrDefault();
                    var connectionsByMachine = $"g.V().has('customer', 'id', '{myCustomerId}').outE('customerSession').inV().inE('customerSession').inV().outE().dedup().count()";
                    var connectionsByMachineCount = GremlinQueryHelper.SubmitRequest(gremlinClient, connectionsByMachine, log).Result.Cast<long>().FirstOrDefault();


                    if (connectionsByAddressCount > 0 || connectionsByIpCount > 0 || connectionsByMachineCount > 0)
                    {
                        log.LogWarning($"Suspicious - :| Customer: {myCustomerId}");
                        log.LogWarning($"Suspicious - :| Order:'{orderId}'.");
                        return false;
                    }
                }

                log.LogInformation(Environment.NewLine);

            }

            log.LogInformation($"Legitime - :) Order:'{orderId}'.");
            return false;

        }

    }
}