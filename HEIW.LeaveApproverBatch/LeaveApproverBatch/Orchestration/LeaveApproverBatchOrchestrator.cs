using HEIW.LeaveApproverBatch.Domain;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HEIW.LeaveApproverBatch.Functions.Orchestration;

public static class LeaveApproverBatchOrchestrator
{
    [Function(nameof(LeaveApproverBatchOrchestrator))]
    public static async Task<BatchResult> Run([OrchestrationTrigger] TaskOrchestrationContext ctx)
    {
        var log = ctx.CreateReplaySafeLogger(nameof(LeaveApproverBatchOrchestrator));
        var req = ctx.GetInput<BatchRequest>() ?? throw new InvalidOperationException("Missing BatchRequest");

        log.LogInformation("Batch start: {BatchId}", req.BatchProcessId);

        var result = await ctx.CallActivityAsync<BatchResult>(
            nameof(Activities.BatchActivities.RunBatch),
            req);

        log.LogInformation("Batch end: {BatchId} => {@Result}", req.BatchProcessId, result);
        return result;
    }
}
