namespace HEIW.LeaveApproverBatch.Application.Options;

public sealed class BatchOptions
{
    public int ExecuteMultipleBatchSize { get; set; } = 300;
    public int ApprovalChunkSize { get; set; } = 50;
    public int MaxDegreeOfParallelism { get; set; } = 1; // keep 1 unless you add strong throttling logic
    public int DataverseRetryCount { get; set; } = 5;
}
