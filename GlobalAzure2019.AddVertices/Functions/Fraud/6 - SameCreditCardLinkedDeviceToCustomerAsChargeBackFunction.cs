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
    public static class SameCreditCardLinkedDeviceToCustomerAsChargeBackFunction
    {
        [FunctionName("SameCreditCardLinkedDeviceToCustomerAsChargeBackFunction")]
        public static bool SameCreditCardLinkedDeviceToCustomerAsChargeBack([ActivityTrigger] string orderId, ILogger log)
        {
            using (var gremlinClient = GremlinQueryHelper.GetGremlinClient())
            {
                // Order placed using a credit card which is linked to a device which was used by a customer who placed an order which resulted in a chargeback

                /// Event: Registration - unique customer name, address, email, etc. 
                /// Event: Session - same customer id (C11111) as registration, unique device id (e.g. D11111), IP address
                /// Event: Session 1 day later -same customer id(C11111) as above registration, unique(different) device id, unique (different) IP address 
                /// Event: Order(O11111) - unique credit card -order is approved
                /// Event: Registration 10 days later -unique customer name, address, email, etc. 
                /// Event: Session - same customer id(C22222) as above registration, same device id as customer's initial session (e.g., D11111), unique IP address 
                /// Event: Order (O22222) - device id linked to a customer (suspicious), unique credit card# (4111 1111 1111 1111) - order is approved
                /// Event: chargeback 90 days later -matched to first order & credit card
                /// Event: Registration 10 days later -unique customer name, address, email, etc. 
                /// Event: Session - same customer id(C33333) as above registration, unique device id, unique IP address 
                /// Event: Order(O33333) - credit card(4111 1111 1111 1111) matched to O22222, customer from O22222(C22222) linked by device id linked to customer(C11111) who placed an order with a chargeback - order is declined

                ///
                /// Fraud :(
                ///

                var degrees = 5;
                var myOrder = "40000000-0000-0000-0003-000000000188";
                var deviceIdQuery = $"g.V(['order', '{myOrder}']).values('creditcardhashed')";

                var deviceId = GremlinQueryHelper.SubmitRequest(gremlinClient, deviceIdQuery, log).Result.Cast<string>().FirstOrDefault();
                if (deviceId != null)
                {
                    var subgraph = $"g.V().has('creditcard', 'creditcardhashed', '{deviceId}').repeat(__.bothE().subgraph('sg').otherV().simplePath()).times({degrees}).cap('sg').V().hasLabel('chargeback').count()";

                    var numChargebacksbyCreditCard = GremlinQueryHelper.SubmitRequest(gremlinClient, subgraph, log).Result.Cast<long>().FirstOrDefault();

                    if (numChargebacksbyCreditCard > 0)
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