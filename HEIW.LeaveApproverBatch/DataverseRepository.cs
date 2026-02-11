using HEIW.LeaveApproverBatch.Application.Abstractions;
using HEIW.LeaveApproverBatch.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HEIW.LeaveApproverBatch.Infrastructure.Dataverse;

public sealed class DataverseRepository : IDataverseRepository
{
    private readonly IDataverseClientFactory _factory;
    private readonly ILogger<DataverseRepository> _log;

    public DataverseRepository(IDataverseClientFactory factory, ILogger<DataverseRepository> log)
    {
        _factory = factory;
        _log = log;
    }

    private ServiceClient CreateClient() => _factory.Create();

    public async Task<string?> GetSystemUserEmailAsync(Guid systemUserId, CancellationToken ct)
    {
        using var dv = CreateClient();
        var cols = new ColumnSet(DataverseConstants.SystemUserInternalEmail);
        var e = await Task.Run(() => dv.Retrieve(DataverseConstants.SystemUser, systemUserId, cols), ct);
        return e.GetAttributeValue<string>(DataverseConstants.SystemUserInternalEmail);
    }

    public async Task<BatchRequest> LoadBatchRequestAsync(Guid batchProcessId, CancellationToken ct)
    {
        using var dv = CreateClient();
        var cols = new ColumnSet(
            DataverseConstants.Batch_Selected,
            DataverseConstants.Batch_Replacement,
            DataverseConstants.Batch_Scope,
            DataverseConstants.Batch_IncludeActioned);

        var row = await Task.Run(() => dv.Retrieve(DataverseConstants.BatchProcess, batchProcessId, cols), ct);

        var selected = row.GetAttributeValue<EntityReference>(DataverseConstants.Batch_Selected)
                      ?? throw new InvalidOperationException("Selected approver not set");
        var replacement = row.GetAttributeValue<EntityReference>(DataverseConstants.Batch_Replacement)
                         ?? throw new InvalidOperationException("Replacement approver not set");

        var scopeOpt = row.GetAttributeValue<OptionSetValue>(DataverseConstants.Batch_Scope)?.Value
                       ?? (int)ReplaceScope.ReplacePendingAndFuture;

        var includeActioned = row.GetAttributeValue<bool?>(DataverseConstants.Batch_IncludeActioned) ?? false;

        // NOTE: post ids are supplied by the UI/controller screen or by a related table.
        // Here we keep it empty - your starter should populate it.
        return new BatchRequest(
            BatchProcessId: batchProcessId,
            CurrentApproverUserId: selected.Id,
            NewApproverUserId: replacement.Id,
            RequestedByUserId: Guid.Empty,
            PostEstablishmentIds: Array.Empty<Guid>(),
            Scope: (ReplaceScope)scopeOpt,
            IncludeAlreadyActioned: includeActioned);
    }

    public async Task<IReadOnlyList<Guid>> FindLeaveApproverAssignmentIdsAsync(
        Guid currentApproverUserId,
        IReadOnlyList<Guid> postEstablishmentIds,
        CancellationToken ct)
    {
        using var dv = CreateClient();

        if (postEstablishmentIds.Count == 0) return Array.Empty<Guid>();

        var fetch = FetchXml.LeaveApproverAssignments(currentApproverUserId, postEstablishmentIds);
        var res = await Task.Run(() => dv.RetrieveMultiple(new FetchExpression(fetch)), ct);

        return res.Entities
            .Select(e => e.Id)
            .Where(id => id != Guid.Empty)
            .ToList();
    }

    public async Task<int> UpdateLeaveApproverAssignmentsAsync(
        IReadOnlyList<Guid> leaveApproverIds,
        Guid newApproverUserId,
        CancellationToken ct)
    {
        using var dv = CreateClient();

        var reqs = new List<OrganizationRequest>(leaveApproverIds.Count);
        foreach (var id in leaveApproverIds)
        {
            var ent = new Entity(DataverseConstants.LeaveApprover, id)
            {
                [DataverseConstants.LeaveApprover_User] = new EntityReference(DataverseConstants.SystemUser, newApproverUserId)
            };
            reqs.Add(new UpdateRequest { Target = ent });
        }

        var em = ExecuteMultipleHelper.Build(reqs);
        var resp = (ExecuteMultipleResponse)await Task.Run(() => dv.Execute(em), ct);

        int success = 0;
        foreach (var r in resp.Responses)
        {
            if (r.Fault is null) success++;
            else _log.LogWarning("UpdateLeaveApproverAssignments fault: {Message}", r.Fault.Message);
        }
        return success;
    }

    public async Task<IReadOnlyList<Guid>> FindPendingLeaveApprovalsAsync(
        BatchRequest req,
        string currentApproverEmail,
        CancellationToken ct)
    {
        using var dv = CreateClient();

        if (req.PostEstablishmentIds.Count == 0) return Array.Empty<Guid>();

        var fetch = FetchXml.PendingLeaveApprovals(req, currentApproverEmail);
        var res = await Task.Run(() => dv.RetrieveMultiple(new FetchExpression(fetch)), ct);

        return res.Entities.Select(e => e.Id).ToList();
    }

    public async Task<IReadOnlyList<ApprovalCancellationInfo>> GetApprovalCancellationInfoAsync(
        IReadOnlyList<Guid> leaveApprovalIds,
        CancellationToken ct)
    {
        using var dv = CreateClient();

        // Pull heiw_leaveapprovalrequestid values
        var qe = new QueryExpression(DataverseConstants.LeaveApproval)
        {
            ColumnSet = new ColumnSet(DataverseConstants.LeaveApproval_RequestId),
            Criteria = new FilterExpression(LogicalOperator.And)
        };
        qe.Criteria.AddCondition(new ConditionExpression(DataverseConstants.LeaveApprovalId, ConditionOperator.In, leaveApprovalIds.Cast<object>().ToArray()));

        var approvals = await Task.Run(() => dv.RetrieveMultiple(qe), ct);

        var result = new List<ApprovalCancellationInfo>(approvals.Entities.Count);

        foreach (var a in approvals.Entities)
        {
            var leaveApprovalId = a.Id;
            var requestId = a.GetAttributeValue<string>(DataverseConstants.LeaveApproval_RequestId);

            string? notificationUri = null;

            if (!string.IsNullOrWhiteSpace(requestId) && Guid.TryParse(requestId, out var approvalGuid))
            {
                // Find msdyn_flow_flowapprovals record that has flownotificationuri
                var fe = new QueryExpression("msdyn_flow_flowapprovals")
                {
                    ColumnSet = new ColumnSet("msdyn_flow_flowapproval_flownotificationuri"),
                    Criteria = new FilterExpression(LogicalOperator.And)
                };
                fe.Criteria.AddCondition("_msdyn_flow_flowapproval_approval_value", ConditionOperator.Equal, approvalGuid);
                fe.Criteria.AddCondition("msdyn_flow_flowapproval_flownotificationuri", ConditionOperator.NotNull);

                var flowApprovals = await Task.Run(() => dv.RetrieveMultiple(fe), ct);
                notificationUri = flowApprovals.Entities.FirstOrDefault()?.GetAttributeValue<string>("msdyn_flow_flowapproval_flownotificationuri");
            }

            result.Add(new ApprovalCancellationInfo(leaveApprovalId, requestId, notificationUri));
        }

        return result;
    }

    public async Task MarkMsApprovalCancelledAsync(string approvalId, CancellationToken ct)
    {
        // Mirrors flow: Update msdyn_flow_approvals stage/status/state
        using var dv = CreateClient();

        if (!Guid.TryParse(approvalId, out var approvalGuid)) return;

        var ent = new Entity("msdyn_flow_approvals", approvalGuid)
        {
            // from flow json:
            ["msdyn_flow_approval_stage"] = 192351000,
            ["statuscode"] = new OptionSetValue(192350002),
            ["statecode"] = new OptionSetValue(1)
        };

        await Task.Run(() => dv.Update(ent), ct);
    }

    public async Task CreateMsApprovalResponseCancelledAsync(string approvalId, CancellationToken ct)
    {
        // Mirrors flow: Create row in msdyn_flow_approvalresponses
        using var dv = CreateClient();
        if (!Guid.TryParse(approvalId, out var approvalGuid)) return;

        var ent = new Entity("msdyn_flow_approvalresponses");
        ent["msdyn_flow_approvalresponse_approval@odata.bind"] = $"/msdyn_flow_approvals({approvalGuid})";
        ent["msdyn_flow_approvalresponse_approvalstagekey"] = approvalId;
        ent["msdyn_flow_approvalresponse_name"] = "Cancelled";
        ent["msdyn_flow_approvalresponse_response"] = "Cancelled";
        ent["msdyn_flow_approvalresponse_stage"] = new OptionSetValue(192351000);
        ent["statuscode"] = new OptionSetValue(192350002);
        ent["msdyn_flow_approvalresponseidx_approvalid"] = approvalId;
        ent["msdyn_flow_approvalresponse_comments"] = "Cancelled";
        ent["statecode"] = new OptionSetValue(1);

        await Task.Run(() => dv.Create(ent), ct);
    }

    public async Task ReassignLeaveApprovalAsync(
        Guid leaveApprovalId,
        string currentEmail,
        string newEmail,
        string reassignedByEmail,
        CancellationToken ct)
    {
        using var dv = CreateClient();

        var row = await Task.Run(() =>
            dv.Retrieve(DataverseConstants.LeaveApproval, leaveApprovalId,
                new ColumnSet(
                    DataverseConstants.LeaveApproval_AssignedTo,
                    DataverseConstants.LeaveApproval_RequestId)), ct);

        var assignedTo = row.GetAttributeValue<string>(DataverseConstants.LeaveApproval_AssignedTo) ?? string.Empty;

        // Replace current approver email with new approver email (flow does this)
        var updatedAssignedTo = assignedTo.Replace(currentEmail, newEmail, StringComparison.OrdinalIgnoreCase);

        var update = new Entity(DataverseConstants.LeaveApproval, leaveApprovalId)
        {
            [DataverseConstants.LeaveApproval_AssignedTo] = updatedAssignedTo,
            [DataverseConstants.LeaveApproval_RequestId] = null,
            [DataverseConstants.LeaveApproval_OriginalApprover] = currentEmail,
            [DataverseConstants.LeaveApproval_Reassigned] = true,
            [DataverseConstants.LeaveApproval_ReassignedBy] = reassignedByEmail
        };

        await Task.Run(() => dv.Update(update), ct);
    }

    public async Task MarkBatchCompletedAsync(Guid batchProcessId, BatchResult result, CancellationToken ct)
    {
        using var dv = CreateClient();

        var summary =
            $"Updated leave approver rows: {result.LeaveApproverRowsUpdated}\n" +
            $"Approvals found: {result.ApprovalsFound}\n" +
            $"Approvals cancelled: {result.ApprovalsCancelled}\n" +
            $"Approvals reassigned: {result.ApprovalsReassigned}\n" +
            $"Failures: {result.Failures}";

        var update = new Entity(DataverseConstants.BatchProcess, batchProcessId)
        {
            [DataverseConstants.Batch_LastResult] = summary,
            [DataverseConstants.Batch_LastRunOn] = DateTime.UtcNow.ToString("o")
        };

        await Task.Run(() => dv.Update(update), ct);
    }

    public async Task MarkBatchFailedAsync(Guid batchProcessId, string error, CancellationToken ct)
    {
        using var dv = CreateClient();
        var update = new Entity(DataverseConstants.BatchProcess, batchProcessId)
        {
            [DataverseConstants.Batch_LastResult] = $"FAILED: {error}",
            [DataverseConstants.Batch_LastRunOn] = DateTime.UtcNow.ToString("o")
        };
        await Task.Run(() => dv.Update(update), ct);
    }
}
