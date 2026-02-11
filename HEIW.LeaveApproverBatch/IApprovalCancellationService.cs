using HEIW.LeaveApproverBatch.Domain;
using System.Threading;
using System.Threading.Tasks;

namespace HEIW.LeaveApproverBatch.Application.Abstractions;

public interface IApprovalCancellationService
{
    Task<bool> CancelAsync(ApprovalCancellationInfo info, CancellationToken ct);
}
