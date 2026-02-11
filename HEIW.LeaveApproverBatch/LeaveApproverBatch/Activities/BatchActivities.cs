using HEIW.LeaveApproverBatch.Application.UseCases;
using HEIW.LeaveApproverBatch.Domain;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace HEIW.LeaveApproverBatch.Functions.Activities;

public sealed class BatchActivities
{
    private readonly RunBatchReplacement _useCase;
    private readonly Application.Abstractions.IDataverseRepository _dv;
    private readonly ILogger<BatchActivities> _log;

    public BatchActivities(RunBatchReplacement useCase, Application.Abstractions.IDataverseRepository dv, ILogger<BatchActivities> log)
    {
        _useCase = useCase;
        _dv = dv;
        _log = log;
    }

    [Function(nameof(RunBatch))]
    public async Task<BatchResult> RunBatch([ActivityTrigger] BatchRequest req)
    {
        try
        {
            var result = await _useCase.ExecuteAsync(req, CancellationToken.None);
            await _dv.MarkBatchCompletedAsync(req.BatchProcessId, result, CancellationToken.None);
            return result;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Batch failed: {BatchId}", req.BatchProcessId);
            await _dv.MarkBatchFailedAsync(req.BatchProcessId, ex.Message, CancellationToken.None);
            throw;
        }
    }
}
