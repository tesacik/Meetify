using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Meetify.Pages;

public partial class Calendar
{
	private string? _userId;
	private DateOnly _month = DateOnly.FromDateTime(DateTime.Today);

	[CascadingParameter]
	private Task<AuthenticationState> AuthTask { get; set; } = default!;


	protected override async Task OnInitializedAsync()
	{
		var user = (await AuthTask!)!.User;

		if (user is null) return;

		var userEmail = user.FindFirst("email")?.Value
		  ?? user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

		if (userEmail is null) return;

		_userId = userEmail;
	}

	private void OnMonthChanged(DateOnly month)
	{
		_month = month;
	}
}
