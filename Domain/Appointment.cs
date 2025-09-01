namespace Meetify.Domain;

public class Appointment
{
	public int Id { get; set; }
        public string OwnerUserId { get; set; } = default!; // vlastník kalendáře
        public DateTime StartUtc { get; set; }
        public DateTime EndUtc { get; set; }
        public Guid ShareLinkId { get; set; }


	// Kdo si schůzku domluvil (host)
        public string GuestFirstName { get; set; } = default!;
        public string GuestLastName { get; set; } = default!;

	public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

