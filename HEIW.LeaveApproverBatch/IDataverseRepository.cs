using HEIW.LeaveApproverBatch.Domain;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HEIW.LeaveApproverBatch.Application.Abstractions;

public interface IDataverseRepository
{
    Task<string?> GetSystemUserEmailAsync(Guid systemUserId, CancellationToken ct);

    Task<BatchRequest> LoadBatchRequestAsync(Guid batchProcessId, CancellationToken ct);

    Task<IReadOnlyList<Guid>> FindLeaveApproverAssignmentIdsAsync(
        Guid currentApproverUserId,
        IReadOnlyList<Guid> postEstablishmentIds,
        CancellationToken ct);

    Task<int> UpdateLeaveApproverAssignmentsAsync(
        IReadOnlyList<Guid> leaveApproverIds,
        Guid newApproverUserId,
        CancellationToken ct);

    Task<IReadOnlyList<Guid>> FindPendingLeaveApprovalsAsync(
        BatchRequest req,
        string currentApproverEmail,
        CancellationToken ct);

    Task<IReadOnlyList<ApprovalCancellationInfo>> GetApprovalCancellationInfoAsync(
        IReadOnlyList<Guid> leaveApprovalIds,
        CancellationToken ct);

    Task MarkMsApprovalCancelledAsync(string approvalId, CancellationToken ct);
    Task CreateMsApprovalResponseCancelledAsync(string approvalId, CancellationToken ct);

    Task ReassignLeaveApprovalAsync(
        Guid leaveApprovalId,
        string currentEmail,
        string newEmail,
        string reassignedByEmail,
        CancellationToken ct);

    Task MarkBatchCompletedAsync(Guid batchProcessId, BatchResult result, CancellationToken ct);
    Task MarkBatchFailedAsync(Guid batchProcessId, string error, CancellationToken ct);
}
