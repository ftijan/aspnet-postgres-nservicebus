using Microsoft.EntityFrameworkCore;
using Sample.Common;
using Sample.WorkerService;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
	?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddDbContext<SampleDbContext>(options =>
	options.UseNpgsql(connectionString));

// TODO: Configure NServiceBus endpoint to subscribe to/handle messages

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();