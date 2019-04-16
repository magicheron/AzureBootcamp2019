using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphSON;
using Gremlin.Net.Driver.Exceptions;
using GlobalAzure2019.AddVertices.Helper;
using System.Net.Http;

namespace GlobalAzure2019.AddVertices.Functions
{
    public static class FraudDetectionFunction
    {

        [FunctionName("FraudDetectionFunction_Orchestration")]
        public static async Task<List<bool>> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var orderId = context.GetInput<string>();
            var outputs = new List<bool>();

            outputs.Add(await context.CallActivityAsync<bool>("UserRegisterAndPlacesAndOrderFunction", orderId));
            outputs.Add(await context.CallActivityAsync<bool>("UserRegisterAndPlacesAndOrderWithPreviouslyDeviceIdFunction", orderId));
            outputs.Add(await context.CallActivityAsync<bool>("HighlyUsageDeviceIdFunction", orderId));
            outputs.Add(await context.CallActivityAsync<bool>("SameCreditCardAsChargeBackFunction", orderId));
            outputs.Add(await context.CallActivityAsync<bool>("SameDeviceAsChargeBackFunction", orderId));
            outputs.Add(await context.CallActivityAsync<bool>("SameCreditCardLinkedDeviceToCustomerAsChargeBackFunction", orderId));
            outputs.Add(await context.CallActivityAsync<bool>("FourLevelsOfLinkageFunction", orderId));
            
            return outputs;
        }

        [FunctionName("FraudDetectionFunction")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            ILogger log)
        {
            string order = req.RequestUri.ParseQueryString()["order"];

            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("FraudDetectionFunction_Orchestration", order);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }


    }
}
