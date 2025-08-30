using Meetify.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Meetify.Data;

public class ApplicationDbContext : IdentityDbContext
{
	public DbSet<ShareLink> ShareLinks => Set<ShareLink>();
	public DbSet<Appointment> Appointments => Set<Appointment>();
	public DbSet<GoogleUser> GoogleUsers => Set<GoogleUser>();

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

		builder.Entity<GoogleUser>(cfg =>
		{
			cfg.HasIndex(x => x.Email).IsUnique();
			cfg.Property(x => x.Email).IsRequired().HasMaxLength(450);
			cfg.Property(x => x.FirstName).HasMaxLength(100);
			cfg.Property(x => x.LastName).HasMaxLength(100);
		});
	}
}
