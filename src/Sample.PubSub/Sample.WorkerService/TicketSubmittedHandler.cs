using Microsoft.EntityFrameworkCore;
using Sample.Common;
using Sample.Common.Data;
using Sample.Common.Messages;

namespace Sample.WorkerService;

public sealed class TicketSubmittedHandler(
	SampleDbContext dbContext,
	ILogger<TicketSubmittedHandler> logger) : IHandleMessages<TicketSubmitted>
{
	public async Task Handle(TicketSubmitted message, IMessageHandlerContext context)
	{
		var exists = await dbContext.Tickets
			.AnyAsync(t => t.Id == message.Id, context.CancellationToken);

		if (exists)
		{
			logger.LogInformation("Ticket {TicketId} already exists; skipping duplicate message.", message.Id);
			return;
		}

		var ticket = new Ticket
		{
			Id = message.Id,
			Text = message.Text,
			CreatedAt = DateTimeOffset.UtcNow
		};

		dbContext.Tickets.Add(ticket);
		await dbContext.SaveChangesAsync(context.CancellationToken);

		logger.LogInformation("Persisted ticket {TicketId}.", ticket.Id);
	}
}