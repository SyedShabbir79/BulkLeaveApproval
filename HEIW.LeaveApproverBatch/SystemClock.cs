using HEIW.LeaveApproverBatch.Application.Abstractions;
using System;

namespace HEIW.LeaveApproverBatch.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
