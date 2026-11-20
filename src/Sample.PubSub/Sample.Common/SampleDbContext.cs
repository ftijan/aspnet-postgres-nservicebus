using Microsoft.EntityFrameworkCore;

namespace Sample.Common;

public sealed class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options)
{
	public DbSet<Ticket> Tickets => Set<Ticket>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.HasDefaultSchema("app");

		modelBuilder.Entity<Ticket>(entity =>
		{
			entity.ToTable("tickets");
			entity.HasKey(x => x.Id);
			entity.Property(x => x.Text).IsRequired();
			entity.Property(x => x.CreatedAt).IsRequired();
		});
	}
}