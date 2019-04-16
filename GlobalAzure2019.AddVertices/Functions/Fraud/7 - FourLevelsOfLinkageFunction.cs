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
    public static class FourLevelsOfLinkageFunction
    {
        [FunctionName("FourLevelsOfLinkageFunction")]
        public static bool FourLevelsOfLinkage([ActivityTrigger] string orderId, ILogger log)
        {
            using (var gremlinClient = GremlinQueryHelper.GetGremlinClient())
            {
                // Four levels of linkage are suspicious even without a chargeback

                /// Event: Registration - unique customer name, address, email, etc. 
                /// Event: Session - same customer id (C11111) as registration, unique device id (e.g. D11111), IP address 
                /// Event: Order (O11111) - unique credit card - order is approved
                /// Event: Registration 10 days later -unique customer name, address, email, etc. 
                /// Event: Session - same customer id(C22222) as above registration, same device id as customer C11111's session (e.g., D11111), unique IP address 
                /// Event: Order (O22222) - device id linked to a customer (suspicious), unique credit card# (4111 1111 1111 1111) - order is approved
                /// Event: Registration 10 days later -unique customer name(Joe Banks), address, email(volcanojoe@gmail.com), etc.
                /// Event: Session - same customer id(C33333) as above registration, unique device id, unique IP address 
                /// Event: Order(O33333) - credit card(4111 1111 1111 1111) matched to O22222
                /// Event: Registration 10 days later -customer name(Joe Banks) and same physical address as C33333, unique email 
                /// Event: Session - same customer id(C44444) as above registration, unique device id, unique IP address 
                /// Event: Order(O44444) - Name and Email linked to customer from O33333-- order is declined --too many layers of account linkage(despite the fact that there are no links to chargebacks or other "hard" fraud indicators)

                ///
                /// Fraud :(
                ///

                var degrees = 9;
                var myOrder = "40000000-0000-0000-0148-000000000304";
                var numCustomersQuery = $"g.V(['order', '{myOrder}']).repeat(__.bothE().subgraph('sg').otherV().simplePath()).times({degrees}).cap('sg').V().hasLabel('customer').dedup().count()";
                var numCreditCardsQuery = $"g.V(['order', '{myOrder}']).repeat(__.bothE().subgraph('sg').otherV().simplePath()).times({degrees}).cap('sg').V().hasLabel('creditcard').dedup().count()";

                var numCustomers = GremlinQueryHelper.SubmitRequest(gremlinClient, numCustomersQuery, log).Result.Cast<long>().FirstOrDefault();
                var numCreditCards = GremlinQueryHelper.SubmitRequest(gremlinClient, numCustomersQuery, log).Result.Cast<long>().FirstOrDefault();

                if (numCustomers > 0 || numCreditCards > 0)
                {
                    log.LogError($"Fraud - :( Order:'{orderId}'.");
                    return true;
                }

                log.LogInformation(Environment.NewLine);

            }

            log.LogInformation($"Legitime - :) Order:'{orderId}'.");
            return false;
        }

    }
}