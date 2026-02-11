using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System.Collections.Generic;

namespace HEIW.LeaveApproverBatch.Infrastructure.Dataverse;

internal static class ExecuteMultipleHelper
{
    public static ExecuteMultipleRequest Build(IReadOnlyList<OrganizationRequest> requests)
    {
        var emr = new ExecuteMultipleRequest
        {
            Settings = new ExecuteMultipleSettings
            {
                ContinueOnError = true,
                ReturnResponses = true
            },
            Requests = new OrganizationRequestCollection()
        };
        foreach (var r in requests) emr.Requests.Add(r);
        return emr;
    }
}
