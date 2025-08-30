using Meetify.Data;
using Meetify.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using static System.Reflection.Metadata.BlobBuilder;

namespace Meetify.Pages;

public partial class Schedule
{
	private ShareLink? _link;
	private string? _linkOwner;
	private int _appointmentsCount;

	private DateOnly _currentMonth = DateOnly.FromDateTime(DateTime.Today);
	private DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

	// modal state
	private bool _showModal;
	private DateOnly _selectedDay;
	private string _duration = "30"; // 30,45,60 (min)
	private List<(TimeOnly from, TimeOnly to, bool available, string reason)> _slots = new();
	private string _guestFirst = string.Empty;
	private string _guestLast = string.Empty;
	private string? _message;
	private TimeOnly? _selectedFrom;

	[Inject]
	private UserManager<IdentityUser> UserManager { get; set; } = default!;

	[Inject]
	private NavigationManager Nav { get; set; } = default!;

        [Inject]
        private Services.SlotService Slots { get; set; } = default!;

	[Inject] 
	private IDbContextFactory<ApplicationDbContext> DbFactory { get; set; } = default!;

	[CascadingParameter]
	private Task<AuthenticationState> AuthTask { get; set; } = default!;

	[Parameter]
	public Guid Id { get; set; }

	protected override Task OnInitializedAsync()
	{
		return base.OnInitializedAsync();
	}

        protected override async Task OnParametersSetAsync()
        {
                await using var db = await DbFactory.CreateDbContextAsync();
                _link = await db.ShareLinks.FindAsync(Id);
                if (_link is null) { _message = "Odkaz je neplatný."; return; }

		_linkOwner = _link.OwnerUserId;
		if (_linkOwner is null) { _message = "Kalendář nenalezen."; return; }

		_appointmentsCount = await Slots.CountAppointmentsInMonthAsync(_link.OwnerUserId, _currentMonth);

		// Pokud odkaz otevře vlastník, přesměruj na kalendář
		var user = (await AuthTask!)!.User;
		var userEmail = user.FindFirst("email")?.Value
			?? user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

		if (userEmail == _linkOwner)
		{
			Nav.NavigateTo("/calendar", true);
			return;
		}
	}

	private string OwnerLabel()
	{
		//var email = _linkOwner?.Email ?? _linkOwner?.UserName ?? "";
		//var display = email?.Split('@')[0] ?? "uživatel";
		//var parts = display.Split('.', '-', '_');
		//var firstName = parts[0];
		//var lastInitial = parts.Length > 1 ? parts[1].Substring(0, 1).ToUpperInvariant() : "N";
		//return $"Sjednat schůzku s uživatelem {firstName} {lastInitial}.";
		return null;
	}

	private async Task OpenDay(DateOnly day)
	{
		_selectedDay = day;
		_showModal = true;
		await ReloadSlots();
	}

	private async Task ReloadSlots()
	{
		var dur = int.Parse(_duration);

		_slots = await Slots.GetSlotsAsync(_link!.OwnerUserId, _selectedDay, TimeSpan.FromMinutes(dur), Today);
		StateHasChanged();
	}

	private async Task Book()
	{
		//if (_selectedFrom is null) return;
		//var dur = TimeSpan.FromMinutes(int.Parse(_duration));
		//var (ok, err) = await Slots.TryBookAsync(
		//	_link!.OwnerUserId, _link.Id, _selectedDay, _selectedFrom.Value,
		//	dur, _guestFirst, _guestLast);

		//if (!ok)
		//{
		//	_message = err; _showModal = false; return;
		//}
		//_message = $"Vaše schůzka s {_owner?.Email?.Split('@')[0]} byla rezervována.";
		//_showModal = false;
	}
}
