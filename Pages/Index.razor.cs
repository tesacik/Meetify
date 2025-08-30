using Meetify.Data;
using Meetify.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Meetify.Pages;

public partial class Index
{
	private string? _newLink;

	[Inject]
	private UserManager<IdentityUser> UserManager { get; set; } = default!;

	[Inject]
	private NavigationManager Nav { get; set; } = default!;

        [Inject]
        private IDbContextFactory<ApplicationDbContext> DbFactory { get; set; } = default!;

	[CascadingParameter]
	private Task<AuthenticationState> AuthTask { get; set; } = default!;


	private async Task GenerateLink()
	{
		var user = (await AuthTask!)!.User;

		if (user is null) return;

		var userEmail = user.FindFirst("email")?.Value
		  ?? user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

		if (userEmail is null) return;

                var link = new ShareLink { OwnerUserId = userEmail };

                await using var db = await DbFactory.CreateDbContextAsync();
                db.ShareLinks.Add(link);
                await db.SaveChangesAsync();

		_newLink = Nav.BaseUri.TrimEnd('/') + $"/s/{link.Id}";
	}

}
