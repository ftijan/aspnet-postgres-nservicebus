namespace Sample.Common.Data;

public sealed class Ticket
{
	public Guid Id { get; set; }
	public string Text { get; set; } = string.Empty;
	public DateTimeOffset CreatedAt { get; set; }
}