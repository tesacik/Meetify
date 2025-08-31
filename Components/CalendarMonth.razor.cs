using Meetify.Data;
using Meetify.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Meetify.Components;

public partial class CalendarMonth
{
	[Parameter] public DateOnly Month { get; set; }
        [Parameter] public string OwnerUserId { get; set; } = default!;
        [Parameter] public bool IsPublicView { get; set; }
        [Parameter] public EventCallback<DateOnly> OnDayClick { get; set; }
        [Parameter] public EventCallback<DateOnly> MonthChanged { get; set; }

	[Inject]
	private NavigationManager Nav { get; set; } = default!;

	[Inject]
	private IDbContextFactory<ApplicationDbContext> DbFactory { get; set; } = default!;

	[Inject]
	private Services.SlotService Slots { get; set; } = default!;

	private readonly string[] _dayNames =
		new[] { "Po", "Út", "St", "Čt", "Pá", "So", "Ne" };

	private readonly System.Globalization.CultureInfo _csCulture =
		System.Globalization.CultureInfo.GetCultureInfo("cs-CZ");

	private List<List<DateOnly>> _weeks = new();
	private Dictionary<DateOnly, List<(TimeOnly from, string text)>> _eventsByDay = new();

	protected override async Task OnParametersSetAsync()
	{
		Month = new DateOnly(Month.Year, Month.Month, 1);
		BuildWeeks();
		await LoadEvents();
	}

	private void BuildWeeks()
	{
		_weeks.Clear();

		var first = new DateOnly(Month.Year, Month.Month, 1);
		// Monday-based calendar: shift Sunday(0) -> 6, else -1
		var firstDow = (int)first.DayOfWeek;
		firstDow = firstDow == 0 ? 6 : firstDow - 1;

		var cursor = first.AddDays(-firstDow);

		for (int w = 0; w < 6; w++)
		{
			var row = new List<DateOnly>(7);
			for (int d = 0; d < 7; d++)
			{
				row.Add(cursor);
				cursor = cursor.AddDays(1);
			}
			_weeks.Add(row);
		}
	}

	private async Task LoadEvents()
	{
		_eventsByDay.Clear();

		//var firstOfMonth = new DateOnly(Month.Year, Month.Month, 1);
		var rangeStart = Month.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local);
		var rangeEnd = rangeStart.AddMonths(1);

		await using var db = await DbFactory.CreateDbContextAsync();

		var apps = await db.Appointments
			.Where(a => a.OwnerUserId == OwnerUserId &&
				a.StartUtc >= rangeStart.ToUniversalTime() &&
				a.StartUtc < rangeEnd.ToUniversalTime())
			.OrderBy(a => a.StartUtc)
			.ToListAsync();

		foreach (var a in apps)
		{
			var localStart = a.StartUtc.ToLocalTime();
			var localEnd = a.EndUtc.ToLocalTime();
			var day = DateOnly.FromDateTime(localStart);

			if (!_eventsByDay.TryGetValue(day, out var list))
				_eventsByDay[day] = list = new();

			var label = $"{localStart:HH\\:mm}-{localEnd:HH\\:mm}, {a.GuestFirstName} {a.GuestLastName}";
			list.Add((TimeOnly.FromDateTime(localStart), label));
		}

		foreach (var list in _eventsByDay.Values)
			list.Sort((a, b) => a.from.CompareTo(b.from));
	}

	private bool IsClickable(DateOnly day)
	{
		if (!IsPublicView)
			return day.Month == Month.Month; // owner view: no restriction, just current month cells clickable (if you want)

		var today = DateOnly.FromDateTime(DateTime.Today);
		return day.Month == Month.Month
			   && SlotService.IsWithinWindow(day, today)
			   && !SlotService.IsWeekendOrHoliday(day);
	}

	private async Task OnDayClickInternal(DateOnly day)
	{
		if (IsClickable(day))
			await OnDayClick.InvokeAsync(day);
	}

        public async Task PrevMonth()
        {
                Month = Month.AddMonths(-1);
                BuildWeeks();
                await LoadEvents();
                await MonthChanged.InvokeAsync(Month);
                StateHasChanged();
        }

        public async Task NextMonth()
        {
                Month = Month.AddMonths(1);
                BuildWeeks();
                await LoadEvents();
                await MonthChanged.InvokeAsync(Month);
                StateHasChanged();
        }
}
