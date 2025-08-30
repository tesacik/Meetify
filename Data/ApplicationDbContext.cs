using Meetify.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Meetify.Data;

public class ApplicationDbContext : IdentityDbContext
{
	public DbSet<ShareLink> ShareLinks => Set<ShareLink>();
	public DbSet<Appointment> Appointments => Set<Appointment>();

	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

	protected override void OnModelCreating(ModelBuilder builder)
	{
		base.OnModelCreating(builder);

		builder.Entity<Appointment>()
			.HasIndex(a => new { a.OwnerUserId, a.StartUtc })
			.IsUnique();
	}
}
