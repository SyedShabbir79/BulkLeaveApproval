using HEIW.LeaveApproverBatch.Application.Abstractions;
using HEIW.LeaveApproverBatch.Application.Options;
using HEIW.LeaveApproverBatch.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HEIW.LeaveApproverBatch.Application.UseCases;

public sealed class RunBatchReplacement
{
    private readonly IDataverseRepository _dv;
    private readonly IApprovalCancellationService _approvals;
    private readonly BatchOptions _opts;
    private readonly ILogger<RunBatchReplacement> _log;

    public RunBatchReplacement(
        IDataverseRepository dv,
        IApprovalCancellationService approvals,
        IOptions<BatchOptions> opts,
        ILogger<RunBatchReplacement> log)
    {
        _dv = dv;
        _approvals = approvals;
        _opts = opts.Value;
        _log = log;
    }

    public async Task<BatchResult> ExecuteAsync(BatchRequest req, CancellationToken ct)
    {
        // Resolve emails (flow uses email strings for assignment replacement)
        var currentEmail = await _dv.GetSystemUserEmailAsync(req.CurrentApproverUserId, ct)
                          ?? throw new InvalidOperationException("Current approver has no email");
        var newEmail = await _dv.GetSystemUserEmailAsync(req.NewApproverUserId, ct)
                      ?? throw new InvalidOperationException("New approver has no email");
        var byEmail = await _dv.GetSystemUserEmailAsync(req.RequestedByUserId, ct)
                     ?? "system";

        _log.LogInformation("Batch {BatchId}: current={CurrentEmail}, new={NewEmail}, scope={Scope}, dryRun={DryRun}",
            req.BatchProcessId, currentEmail, newEmail, req.Scope, req.DryRun);

        // 1) Update LeaveApprover assignments for selected posts
        var assignmentIds = await _dv.FindLeaveApproverAssignmentIdsAsync(
            req.CurrentApproverUserId, req.PostEstablishmentIds, ct);

        _log.LogInformation("Batch {BatchId}: found {Count} heiw_leaveapprover rows to update",
            req.BatchProcessId, assignmentIds.Count);

        int updatedAssignments = 0;
        if (!req.DryRun && assignmentIds.Count > 0)
        {
            foreach (var chunk in assignmentIds.Chunk(_opts.ExecuteMultipleBatchSize))
            {
                var updated = await _dv.UpdateLeaveApproverAssignmentsAsync(chunk.ToList(), req.NewApproverUserId, ct);
                updatedAssignments += updated;
                _log.LogInformation("Batch {BatchId}: updated chunk {ChunkCount}, totalUpdated={Total}",
                    req.BatchProcessId, updated, updatedAssignments);
            }
        }

        // 2) Pending approvals to reassign (only where not actioned)
        var pendingApprovalIds = await _dv.FindPendingLeaveApprovalsAsync(req, currentEmail, ct);
        _log.LogInformation("Batch {BatchId}: found {Count} pending approvals to process",
            req.BatchProcessId, pendingApprovalIds.Count);

        int cancelled = 0;
        int reassigned = 0;
        int failures = 0;

        if (!req.DryRun && pendingApprovalIds.Count > 0)
        {
            foreach (var chunk in pendingApprovalIds.Chunk(_opts.ApprovalChunkSize))
            {
                var infos = await _dv.GetApprovalCancellationInfoAsync(chunk.ToList(), ct);

                foreach (var info in infos)
                {
                    try
                    {
                        // Cancel MS Approval (mirror flow: update msdyn_flow_approvals, create response, POST to URI)
                        if (!string.IsNullOrWhiteSpace(info.LeaveApprovalRequestId))
                        {
                            await _dv.MarkMsApprovalCancelledAsync(info.LeaveApprovalRequestId!, ct);
                            await _dv.CreateMsApprovalResponseCancelledAsync(info.LeaveApprovalRequestId!, ct);

                            var didPost = await _approvals.CancelAsync(info, ct);
                            if (didPost) cancelled++;
                        }

                        // Reassign heiw_leaveapproval fields
                        await _dv.ReassignLeaveApprovalAsync(info.LeaveApprovalId, currentEmail, newEmail, byEmail, ct);
                        reassigned++;
                    }
                    catch (Exception ex)
                    {
                        failures++;
                        _log.LogError(ex, "Batch {BatchId}: failed processing approval {LeaveApprovalId}",
                            req.BatchProcessId, info.LeaveApprovalId);
                    }
                }
            }
        }

        return new BatchResult(
            LeaveApproverRowsUpdated: updatedAssignments,
            ApprovalsFound: pendingApprovalIds.Count,
            ApprovalsCancelled: cancelled,
            ApprovalsReassigned: reassigned,
            Failures: failures);
    }
}
