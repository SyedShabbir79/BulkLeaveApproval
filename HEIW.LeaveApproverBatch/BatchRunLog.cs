using System;

namespace HEIW.LeaveApproverBatch.Domain;

public sealed record BatchRunLog(
    Guid BatchProcessId,
    DateTimeOffset StartedOn,
    DateTimeOffset? CompletedOn,
    string Status,
    string Summary
);
