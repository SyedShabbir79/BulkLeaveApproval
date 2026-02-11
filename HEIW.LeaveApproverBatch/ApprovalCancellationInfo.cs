using System;

namespace HEIW.LeaveApproverBatch.Domain;

public sealed record ApprovalCancellationInfo(
    Guid LeaveApprovalId,
    string? LeaveApprovalRequestId,          // heiw_leaveapprovalrequestid (string in Dataverse)
    string? FlowNotificationUri              // msdyn_flow_flowapproval_flownotificationuri
);
