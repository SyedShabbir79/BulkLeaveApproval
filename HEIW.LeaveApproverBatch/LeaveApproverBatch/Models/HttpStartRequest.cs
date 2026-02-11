using HEIW.LeaveApproverBatch.Domain;
using System;
using System.Collections.Generic;

namespace HEIW.LeaveApproverBatch.Functions.Models;

public sealed class HttpStartRequest
{
    public Guid BatchProcessId { get; set; }
    public Guid CurrentApproverUserId { get; set; }
    public Guid NewApproverUserId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public List<Guid> PostEstablishmentIds { get; set; } = new();
    public ReplaceScope Scope { get; set; } = ReplaceScope.ReplacePendingAndFuture;
    public bool IncludeAlreadyActioned { get; set; } = false;
    public int LookbackMonths { get; set; } = 6;
    public bool DryRun { get; set; } = false;

    public Domain.BatchRequest ToDomain()
        => new(BatchProcessId, CurrentApproverUserId, NewApproverUserId, RequestedByUserId,
            PostEstablishmentIds, Scope, IncludeAlreadyActioned, LookbackMonths, DryRun);
}
