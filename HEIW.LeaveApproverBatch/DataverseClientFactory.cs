using Microsoft.PowerPlatform.Dataverse.Client;

namespace HEIW.LeaveApproverBatch.Infrastructure.Dataverse;

public interface IDataverseClientFactory
{
    ServiceClient Create();
}

public sealed class DataverseClientFactory : IDataverseClientFactory
{
    private readonly string _connectionString;

    public DataverseClientFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public ServiceClient Create()
        => new(_connectionString);
}
