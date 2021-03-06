using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AzureFunctions.Presentation.Demo.DurableFunctions
{
    public static class MyOrchestration
    {
        //This is orchestration function
        [FunctionName("MyOrchestration")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var payment = new Payment
            {
                Email = "daniel.rusnok@itixo.com", ProductId = 456
            };
            var order = await context.CallActivityAsync<Order>("MyOrchestration_GenerateOrder", payment);

            var notifyTask = context.CallActivityAsync("MyOrchestration_NotifyAboutAcceptedPayment", payment);
            var generateLicenceTask = context.CallActivityAsync("MyOrchestration_GenerateLicenceFile", order);

            await Task.WhenAll(notifyTask, generateLicenceTask);
        }

        //This is orchestration activity function
        [FunctionName("MyOrchestration_GenerateOrder")]
        public static Order GenerateOrder([ActivityTrigger] Payment payment, ILogger log)
        {
            log.LogInformation("*************** RECEIVED PAYMENT ******************");
            log.LogInformation($"ProductId: {payment.ProductId}");
            log.LogInformation($"Email: {payment.Email}");

            var order = new Order
            {
                Email = payment.Email,
                ProductId = payment.ProductId,
                OrderId = new Random().Next()
            };

            log.LogInformation("*************** GENERATE LICENCE ORDER ******************");
            log.LogInformation($"OrderId: {order.OrderId}");

            return order;
        }

        //This is orchestration activity function
        [FunctionName("MyOrchestration_GenerateLicenceFile")]
        public static void GenerateLicenceFile(
            [ActivityTrigger] Order order,
            [Blob("licenses/{rand-guid}.lic")] TextWriter outputBlob, //Example of input binding
            ILogger log)
        {
            //Thread.Sleep(3000);

            outputBlob.WriteLine($"OrderId: {order.OrderId}");
            outputBlob.WriteLine($"Email: {order.Email}");
            outputBlob.WriteLine($"ProductId: {order.ProductId}");
            outputBlob.WriteLine($"PurchaseDate: {DateTime.UtcNow}");
            outputBlob.WriteLine($"LicenceCode: {Guid.NewGuid()}");

            log.LogInformation("************** GENERATING LICENCE FILE *******************\n");
            log.LogInformation($"OrderId: {order.OrderId}");
            log.LogInformation($"Email: {order.Email}");
            log.LogInformation($"ProductId: {order.ProductId}");
            log.LogInformation($"PurchaseDate: {DateTime.UtcNow}");
            log.LogInformation($"LicenceCode: {Guid.NewGuid()}");
        }

        //This is orchestration activity function
        [FunctionName("MyOrchestration_NotifyAboutAcceptedPayment")]
        [return: Queue("notifications", Connection = "AzureWebJobsStorage")] // Example of output binding
        public static MyNotification NotifyAboutAcceptedPayment(
            [ActivityTrigger] Payment payment,
            ILogger log)
        {
            //Thread.Sleep(3000);

            log.LogInformation("************** NOTIFYING USER *******************\n");
            var notification = new MyNotification
            {
                Email = payment.Email,
                Message = $"Your payment for product {payment.ProductId} was accepted!"
            };

            return notification;
        }

        //This is orchestration start function
        [FunctionName("MyOrchestration_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("MyOrchestration", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}