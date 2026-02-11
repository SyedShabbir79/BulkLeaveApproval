namespace HEIW.LeaveApproverBatch.Infrastructure.Dataverse;

internal static class DataverseConstants
{
    // Tables
    public const string SystemUser = "systemuser";
    public const string LeaveApprover = "heiw_leaveapprover";
    public const string LeaveApproval = "heiw_leaveapproval";
    public const string LeaveApplication = "heiw_leaveapplication";
    public const string BatchProcess = "heiw_leaveapproverbatchprocess";

    // LeaveApprover columns
    public const string LeaveApproverId = "heiw_leaveapproverid";
    public const string LeaveApprover_User = "heiw_user";
    public const string LeaveApprover_Post = "heiw_postestablishment";
    public const string StateCode = "statecode";

    // LeaveApproval columns
    public const string LeaveApprovalId = "heiw_leaveapprovalid";
    public const string LeaveApproval_AssignedTo = "heiw_assignedto";
    public const string LeaveApproval_Status = "heiw_approvalstatus"; // 0 Requested
    public const string LeaveApproval_RequestId = "heiw_leaveapprovalrequestid";
    public const string LeaveApproval_OriginalApprover = "heiw_originalapprover";
    public const string LeaveApproval_Reassigned = "heiw_reassigned";
    public const string LeaveApproval_ReassignedBy = "heiw_reassignedby";
    public const string LeaveApproval_LeaveApplication = "heiw_leaveapplication";

    // LeaveApplication columns
    public const string LeaveApplication_Post = "heiw_postestablishment";

    // BatchProcess columns
    public const string Batch_Selected = "heiw_selectedleaveapprover";
    public const string Batch_Replacement = "heiw_replacementleaveapprover";
    public const string Batch_Scope = "heiw_replacescope";
    public const string Batch_IncludeActioned = "heiw_includealreadyactioned";
    public const string Batch_LastResult = "heiw_lastresult";
    public const string Batch_LastRunOn = "heiw_lastrunon";

    // systemuser columns
    public const string SystemUserId = "systemuserid";
    public const string SystemUserInternalEmail = "internalemailaddress";
}
