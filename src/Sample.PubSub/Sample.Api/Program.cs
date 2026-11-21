using Microsoft.EntityFrameworkCore;
using Sample.Common;
using Sample.Common.Contracts;
using Sample.Common.Data;
using Sample.Common.Messages;
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

app.MapPost("/api/ticket", async (
    CreateTicketRequest request,
    IMessageSession messageSession) =>
{
    if (string.IsNullOrWhiteSpace(request.Text))
    {
        return Results.BadRequest("Ticket text is required.");
    }

    var id = Guid.NewGuid();
    await messageSession.Publish(new TicketSubmitted(id, request.Text));

    return Results.Accepted($"/api/ticket/{id}", new { id });
});

app.MapGet("/api/ticket", async (
	SampleDbContext db,
	TicketOrderBy orderBy = TicketOrderBy.CreatedAtDesc,
	CancellationToken cancellationToken = default) =>
{
	IQueryable<Ticket> query = db.Tickets;

	query = orderBy switch
	{
		TicketOrderBy.CreatedAtAsc => query.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id),
		TicketOrderBy.TextAsc => query.OrderBy(t => t.Text).ThenBy(t => t.Id),
		TicketOrderBy.TextDesc => query.OrderByDescending(t => t.Text).ThenBy(t => t.Id),
		_ => query.OrderByDescending(t => t.CreatedAt).ThenBy(t => t.Id)
	};

	var tickets = await query
		.Select(t => new TicketResponse(t.Id, t.Text, t.CreatedAt))
		.ToListAsync(cancellationToken);

	return Results.Ok(tickets);
});

app.Run();