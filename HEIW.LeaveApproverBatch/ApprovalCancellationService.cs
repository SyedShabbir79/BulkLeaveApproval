using HEIW.LeaveApproverBatch.Application.Abstractions;
using HEIW.LeaveApproverBatch.Domain;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HEIW.LeaveApproverBatch.Infrastructure.Approvals;

public sealed class ApprovalCancellationService : IApprovalCancellationService
{
    private readonly HttpClient _http;
    private readonly ILogger<ApprovalCancellationService> _log;

    public ApprovalCancellationService(HttpClient http, ILogger<ApprovalCancellationService> log)
    {
        _http = http;
        _log = log;
    }

    public async Task<bool> CancelAsync(ApprovalCancellationInfo info, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(info.FlowNotificationUri))
            return false;

        try
        {
            // Matches flow behavior: POST to the FlowNotificationUri to cancel/clear card
            using var req = new HttpRequestMessage(HttpMethod.Post, info.FlowNotificationUri);
            using var resp = await _http.SendAsync(req, ct);

            if (!resp.IsSuccessStatusCode)
            {
                _log.LogWarning("CancelAsync: POST failed {Status} for LeaveApprovalId={LeaveApprovalId}",
                    resp.StatusCode, info.LeaveApprovalId);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "CancelAsync: exception for LeaveApprovalId={LeaveApprovalId}", info.LeaveApprovalId);
            return false;
        }
    }
}
