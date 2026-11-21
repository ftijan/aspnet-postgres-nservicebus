namespace Sample.Common.Messages;

public sealed record TicketSubmitted(Guid Id, string Text) : IEvent;