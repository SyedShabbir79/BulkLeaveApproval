using System;
using System.Collections.Generic;

public sealed record BatchRequest(
    Guid BatchProcessId,
    Guid CurrentApproverUserId,
    Guid NewApproverUserId,
    Guid RequestedByUserId,
    IReadOnlyList<Guid> PostEstablishmentIds,
    ReplaceScope Scope,
    bool IncludeAlreadyActioned,
    int LookbackMonths = 6,
    bool DryRun = false
);
