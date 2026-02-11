using System;

namespace HEIW.LeaveApproverBatch.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
