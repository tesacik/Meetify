namespace Meetify.Domain;

public class ShareLink
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string OwnerUserId { get; set; } = default!;
	public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
	public bool IsUsed { get; set; } = false;
}

