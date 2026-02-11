using HEIW.LeaveApproverBatch.Application.Abstractions;
using HEIW.LeaveApproverBatch.Application.Options;
using HEIW.LeaveApproverBatch.Application.UseCases;
using HEIW.LeaveApproverBatch.Infrastructure.Approvals;
using HEIW.LeaveApproverBatch.Infrastructure.Dataverse;
using HEIW.LeaveApproverBatch.Infrastructure.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        var dvConn = Environment.GetEnvironmentVariable("DataverseConnectionString")
                     ?? throw new InvalidOperationException("Missing DataverseConnectionString");

        services.Configure<BatchOptions>(_ => { });

        services.AddSingleton<IDataverseClientFactory>(_ => new DataverseClientFactory(dvConn));
        services.AddSingleton<IDataverseRepository, DataverseRepository>();

        services.AddHttpClient<IApprovalCancellationService, ApprovalCancellationService>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<RunBatchReplacement>();
    })
    .Build();

host.Run();
