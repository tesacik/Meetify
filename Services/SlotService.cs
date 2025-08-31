using Meetify.Data;
using Meetify.Domain;
using Microsoft.EntityFrameworkCore;

namespace Meetify.Services;

public class SlotService
{
	private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
	private readonly TimeZoneInfo _tz; // Europe/Prague

	public SlotService(IDbContextFactory<ApplicationDbContext> dbFactory)
	{
		_dbFactory = dbFactory;
		_tz = TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time"); // Windows ID pro Prahu
	}

	public static readonly TimeSpan DayStart = new(9, 0, 0);
	public static readonly TimeSpan DayEnd = new(16, 0, 0);

	public async Task<int> CountAppointmentsInMonthAsync(string ownerUserId, DateOnly month)
	{
		await using var db = await _dbFactory.CreateDbContextAsync();
		var start = month.ToDateTime(TimeOnly.MinValue);
		var end = start.AddMonths(1);
		return await db.Appointments
			.Where(a => a.OwnerUserId == ownerUserId && a.StartUtc >= start && a.StartUtc < end)
			.CountAsync();
	}

	public async Task<List<(DateTime startUtc, DateTime endUtc)>> GetExistingForDayAsync(string ownerUserId, DateOnly day)
	{
		await using var db = await _dbFactory.CreateDbContextAsync();
		return await GetExistingForDayInternalAsync(db, ownerUserId, day);
	}

	private async Task<List<(DateTime startUtc, DateTime endUtc)>> GetExistingForDayInternalAsync(
			ApplicationDbContext db, string ownerUserId, DateOnly day)
	{
		var dayStartLocal = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
		var dayEndLocal = dayStartLocal.AddDays(1);
		var startUtc = TimeZoneInfo.ConvertTimeToUtc(dayStartLocal, _tz);
		var endUtc = TimeZoneInfo.ConvertTimeToUtc(dayEndLocal, _tz);

		return await db.Appointments
				.Where(a => a.OwnerUserId == ownerUserId && a.StartUtc >= startUtc && a.StartUtc < endUtc)
				.OrderBy(a => a.StartUtc)
				.Select(a => new ValueTuple<DateTime, DateTime>(a.StartUtc, a.EndUtc))
				.ToListAsync();
	}

	public static bool IsWeekendOrHoliday(DateOnly d)
	{
		if (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) return true;
		return CzechHolidays.IsHoliday(d);
	}

	public static bool IsWithinWindow(DateOnly d, DateOnly today) =>
		d > today && d <= today.AddMonths(2);

	public static bool RespectsDailyLimit(IEnumerable<(DateTime s, DateTime e)> existing) => existing.Count() < 3;

	public static bool FitsWithBuffer(DateTime startUtc, DateTime endUtc, IEnumerable<(DateTime s, DateTime e)> existing)
	{
		var buffer = TimeSpan.FromMinutes(15);
		foreach (var (s, e) in existing)
		{
			// nový slot musí končit <= s - buffer a začít >= e + buffer (tj. být mimo [s-buffer, e+buffer])
			if (startUtc < e + buffer && endUtc > s - buffer)
				return false;
		}

		return true;
	}

	public async Task<List<(TimeOnly from, TimeOnly to, bool available, string reason)>> GetSlotsAsync(
		string ownerUserId, DateOnly day, TimeSpan duration, DateOnly today)
	{
		var list = new List<(TimeOnly, TimeOnly, bool, string)>();


		if (!IsWithinWindow(day, today) || IsWeekendOrHoliday(day))
		{
			var step = duration + TimeSpan.FromMinutes(15);
			for (var start = DayStart; ; start += step)
			{
				var end = start + duration;
				if (end > DayEnd)
					break;
				var startTime = TimeOnly.FromTimeSpan(start);
				list.Add((startTime, TimeOnly.FromTimeSpan(end), false, "nedostupné"));
			}
			return list;
		}

		var existing = (await GetExistingForDayAsync(ownerUserId, day))
				.Select(x => (s: x.startUtc, e: x.endUtc))
				.ToList();
		var dailyOk = RespectsDailyLimit(existing);

		var slotStep = duration + TimeSpan.FromMinutes(15);
		for (var start = DayStart; ; start += slotStep)
		{
			var end = start + duration;
			if (end > DayEnd)
				break;

			var startTime = TimeOnly.FromTimeSpan(start);
			var endTime = TimeOnly.FromTimeSpan(end);
			var localStart = day.ToDateTime(startTime, DateTimeKind.Unspecified);
			var localEnd = localStart.Add(duration);
			var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, _tz);
			var endUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, _tz);

			var available = dailyOk && FitsWithBuffer(startUtc, endUtc, existing);
			var reason = available ? string.Empty : (dailyOk ? "obsazeno" : "limit překročen");
			list.Add((startTime, endTime, available, string.IsNullOrEmpty(reason) ? "" : reason));
		}
		return list;
	}

	public async Task<(bool ok, string? error)> TryBookAsync(
		string ownerUserId,
		Guid linkId,
		DateOnly day,
		TimeOnly from,
		TimeSpan duration,
		string guestFirst,
		string guestLast)
	{
		await using var db = await _dbFactory.CreateDbContextAsync();

		// Zkontrolovat odkaz
		var link = await db.ShareLinks.FirstOrDefaultAsync(l => l.Id == linkId && l.OwnerUserId == ownerUserId);
		if (link is null) return (false, "Neplatný odkaz.");
		if (link.IsUsed) return (false, "Tento odkaz už byl použit pro sjednání jedné schůzky.");

		var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTime.UtcNow, _tz).Date);
		if (!IsWithinWindow(day, today) || IsWeekendOrHoliday(day))
			return (false, "Vybraný den není povolen.");

		var localStart = day.ToDateTime(from, DateTimeKind.Unspecified);
		var localEnd = localStart.Add(duration);
		var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, _tz);
		var endUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, _tz);

		// Re-validace proti aktuálním datům v DB v transakci
		await using var tx = await db.Database.BeginTransactionAsync();

		var existing = await GetExistingForDayInternalAsync(db, ownerUserId, day);
		if (!RespectsDailyLimit(existing.Select(x => (x.startUtc, x.endUtc))))
			return (false, "Na tento den je již maximum 3 schůzek.");
		if (!FitsWithBuffer(startUtc, endUtc, existing.Select(x => (x.startUtc, x.endUtc))))
			return (false, "Termín koliduje s jinou schůzkou nebo povinnou pauzou.");

		db.Appointments.Add(new Appointment
		{
			OwnerUserId = ownerUserId,
			StartUtc = startUtc,
			EndUtc = endUtc,
			GuestFirstName = guestFirst.Trim(),
			GuestLastName = guestLast.Trim()
		});

		// zamknout odkaz (lze rezervovat pouze jednu schůzku tímto linkem)
		link.IsUsed = true;

		try
		{
			await db.SaveChangesAsync();
			await tx.CommitAsync();
			return (true, null);
		}
		catch (DbUpdateException)
		{
			await tx.RollbackAsync();
			return (false, "Termín byl právě obsazen někým jiným. Zkuste jiný.");
		}
	}
}
