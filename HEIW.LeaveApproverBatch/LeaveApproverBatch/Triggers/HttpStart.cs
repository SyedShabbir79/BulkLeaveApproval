using HEIW.LeaveApproverBatch.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace HEIW.LeaveApproverBatch.Functions.Triggers;

public static class HttpStart
{
    [Function(nameof(HttpStart))]
    public static async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "batch/start")] HttpRequestData req,
        [DurableClient] DurableTaskClient client)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync();
        var dto = JsonSerializer.Deserialize<HttpStartRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                  ?? throw new InvalidOperationException("Invalid body");

        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(Orchestration.LeaveApproverBatchOrchestrator),
            dto.ToDomain());

        var res = req.CreateResponse(HttpStatusCode.Accepted);
        await res.WriteStringAsync($"Started {instanceId}");
        return res;
    }
}
