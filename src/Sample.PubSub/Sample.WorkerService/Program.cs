using Microsoft.EntityFrameworkCore;
using Sample.Common;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
	?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddDbContext<SampleDbContext>(options =>
	options.UseNpgsql(connectionString));

var workerEndpointName = builder.Configuration["Messaging:EndpointName"]
	?? throw new InvalidOperationException("Configuration value 'Messaging:EndpointName' is not configured.");
var errorQueueName = builder.Configuration["Messaging:ErrorQueue"]
	?? throw new InvalidOperationException("Configuration value 'Messaging:ErrorQueue' is not configured.");

var endpointConfiguration = new EndpointConfiguration(workerEndpointName);
endpointConfiguration.UseTransport(new PostgreSqlTransport(connectionString));
endpointConfiguration.UseSerialization<SystemJsonSerializer>();
endpointConfiguration.SendFailedMessagesTo(errorQueueName);

if (builder.Environment.IsDevelopment())
{
	endpointConfiguration.EnableInstallers();
}

builder.Services.AddNServiceBusEndpoint(endpointConfiguration);

// Add background workers here

var host = builder.Build();
host.Run();