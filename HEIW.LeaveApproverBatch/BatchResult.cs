namespace HEIW.LeaveApproverBatch.Domain;

public sealed record BatchResult(
    int LeaveApproverRowsUpdated,
    int ApprovalsFound,
    int ApprovalsCancelled,
    int ApprovalsReassigned,
    int Failures
)
{
    public static BatchResult Empty => new(0, 0, 0, 0, 0);
}
