using Microsoft.EntityFrameworkCore;
using Meetify.Data;
using Meetify.Domain;

namespace Meetify.Services;

public class GoogleUserService
{
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        public GoogleUserService(IDbContextFactory<ApplicationDbContext> dbFactory) => _dbFactory = dbFactory;

	public async Task<GoogleUser?> GetByEmailAsync(string email, CancellationToken ct = default)
	{
                await using var db = await _dbFactory.CreateDbContextAsync();
                var norm = Normalize(email);
                return await db.GoogleUsers.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Email == norm, ct);
	}

	public async Task<GoogleUser> UpsertFromClaimsAsync(
		string email, string? firstName, string? lastName, string? identityUserId = null,
		CancellationToken ct = default)
	{
                await using var db = await _dbFactory.CreateDbContextAsync();
                var norm = Normalize(email);

                var entity = await db.GoogleUsers
                        .FirstOrDefaultAsync(x => x.Email == norm, ct);

		if (entity is null)
		{
			entity = new GoogleUser
			{
				Email = norm,
				FirstName = firstName,
				LastName = lastName,
				IdentityUserId = identityUserId,
				CreatedUtc = DateTime.UtcNow,
				UpdatedUtc = DateTime.UtcNow
			};
                        db.GoogleUsers.Add(entity);
		}
		else
		{
			// update latest details
			entity.FirstName = string.IsNullOrWhiteSpace(firstName) ? entity.FirstName : firstName;
			entity.LastName = string.IsNullOrWhiteSpace(lastName) ? entity.LastName : lastName;
			if (!string.IsNullOrWhiteSpace(identityUserId))
				entity.IdentityUserId = identityUserId;
			entity.UpdatedUtc = DateTime.UtcNow;
		}

                await db.SaveChangesAsync(ct);
		return entity;
	}

	private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
