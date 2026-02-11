using HEIW.LeaveApproverBatch.Domain;
using System;
using System.Collections.Generic;
using System.Security;

namespace HEIW.LeaveApproverBatch.Infrastructure.Dataverse;

internal static class FetchXml
{
    // Find leaveapprover assignment ids for approver + selected posts
    public static string LeaveApproverAssignments(Guid approverUserId, IReadOnlyList<Guid> postIds)
    {
        var postFilter = string.Join("", postIds.Select(p => $"<value uitype=\"heiw_postestablishment\">{p}</value>"));

        return $@"
<fetch distinct='false' no-lock='true'>
  <entity name='{DataverseConstants.LeaveApprover}'>
    <attribute name='{DataverseConstants.LeaveApproverId}' />
    <filter type='and'>
      <condition attribute='{DataverseConstants.StateCode}' operator='eq' value='0' />
      <condition attribute='{DataverseConstants.LeaveApprover_User}' operator='eq' value='{approverUserId}' />
      <condition attribute='{DataverseConstants.LeaveApprover_Post}' operator='in'>
        {postFilter}
      </condition>
    </filter>
  </entity>
</fetch>";
    }

    // Pending leave approvals assigned to approver email, within lookback window, and constrained to posts (via leaveapplication join)
    public static string PendingLeaveApprovals(BatchRequest req, string currentApproverEmail)
    {
        var postFilter = string.Join("", req.PostEstablishmentIds.Select(p => $"<value uitype=\"heiw_postestablishment\">{p}</value>"));

        // heiw_approvalstatus: 0 = Requested (from metadata)
        return $@"
<fetch distinct='true' no-lock='true'>
  <entity name='{DataverseConstants.LeaveApproval}'>
    <attribute name='{DataverseConstants.LeaveApprovalId}' />
    <attribute name='{DataverseConstants.LeaveApproval_RequestId}' />
    <filter type='and'>
      <condition attribute='{DataverseConstants.StateCode}' operator='eq' value='0' />
      <condition attribute='{DataverseConstants.LeaveApproval_Status}' operator='eq' value='0' />
      <condition attribute='{DataverseConstants.LeaveApproval_AssignedTo}' operator='like' value='%{SecurityElement.Escape(currentApproverEmail)}%' />
      <condition attribute='createdon' operator='last-x-months' value='{req.LookbackMonths}' />
    </filter>
    <link-entity name='{DataverseConstants.LeaveApplication}'
                 from='heiw_leaveapplicationid'
                 to='{DataverseConstants.LeaveApproval_LeaveApplication}'
                 link-type='inner'
                 alias='la'>
      <filter type='and'>
        <condition attribute='{DataverseConstants.LeaveApplication_Post}' operator='in'>
          {postFilter}
        </condition>
      </filter>
    </link-entity>
  </entity>
</fetch>";
    }
}
