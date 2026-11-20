using Microsoft.EntityFrameworkCore;
using Sample.Common;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
	?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<SampleDbContext>(options =>
	options.UseNpgsql(connectionString));

var apiEndpointName = builder.Configuration["Messaging:EndpointName"]
	?? throw new InvalidOperationException("Configuration value 'Messaging:EndpointName' is not configured.");
var errorQueueName = builder.Configuration["Messaging:ErrorQueue"]
	?? throw new InvalidOperationException("Configuration value 'Messaging:ErrorQueue' is not configured.");

var endpointConfiguration = new EndpointConfiguration(apiEndpointName);
endpointConfiguration.UseTransport(new PostgreSqlTransport(connectionString));
endpointConfiguration.UseSerialization<SystemJsonSerializer>();
endpointConfiguration.SendFailedMessagesTo(errorQueueName);

if (builder.Environment.IsDevelopment())
{
	endpointConfiguration.EnableInstallers();
}

builder.Services.AddNServiceBusEndpoint(endpointConfiguration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.MapScalarApiReference();

	// Quick local setup only; switch to EF Core migrations once schema changes need versioning
	using var scope = app.Services.CreateScope();
	var db = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
	await db.Database.EnsureCreatedAsync();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.MapPost("/api/ticket", () =>
{
	// TODO: Publish TicketSubmitted via NServiceBus
	return Results.Accepted();
});

app.MapGet("/api/ticket", async (SampleDbContext db) =>
{
	var tickets = await db.Tickets
		.OrderByDescending(t => t.CreatedAt)
		.Select(t => new { t.Id, t.Text })
		.ToListAsync();

	return Results.Ok(tickets);
});

app.Run();