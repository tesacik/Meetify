using System.ComponentModel.DataAnnotations;

namespace Meetify.Domain;

public class GoogleUser
{
	public int Id { get; set; }

	[Required, MaxLength(450)]
	public string Email { get; set; } = default!;      // unique, normalized (lower)

	[MaxLength(100)]
	public string? FirstName { get; set; }

	[MaxLength(100)]
	public string? LastName { get; set; }

	[MaxLength(450)]
	public string? IdentityUserId { get; set; }        // link to ASP.NET Identity user

	public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
	public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
