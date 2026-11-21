namespace Sample.Common.Contracts;

public sealed record CreateTicketRequest(string Text);

public sealed record TicketResponse(Guid Id, string Text, DateTimeOffset CreatedAt);

public enum TicketOrderBy
{
	CreatedAtDesc,
	CreatedAtAsc,
	TextAsc,
	TextDesc
}