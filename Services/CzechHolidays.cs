namespace Meetify.Services;

public static class CzechHolidays
{
	/// <summary>
	/// Returns dictionary of Czech public holidays for the given year.
	/// Key = date, Value = holiday name (in Czech).
	/// </summary>
	public static Dictionary<DateOnly, string> ForYear(int year)
	{
		var dict = new Dictionary<DateOnly, string>();

		// Fixní svátky
		dict[new DateOnly(year, 1, 1)] = "Nový rok, Den obnovy samostatného českého státu";
		dict[new DateOnly(year, 5, 1)] = "Svátek práce";
		dict[new DateOnly(year, 5, 8)] = "Den vítězství";
		dict[new DateOnly(year, 7, 5)] = "Den slovanských věrozvěstů Cyrila a Metoděje";
		dict[new DateOnly(year, 7, 6)] = "Den upálení mistra Jana Husa";
		dict[new DateOnly(year, 9, 28)] = "Den české státnosti";
		dict[new DateOnly(year, 10, 28)] = "Den vzniku samostatného československého státu";
		dict[new DateOnly(year, 11, 17)] = "Den boje za svobodu a demokracii";
		dict[new DateOnly(year, 12, 24)] = "Štědrý den";
		dict[new DateOnly(year, 12, 25)] = "1. svátek vánoční";
		dict[new DateOnly(year, 12, 26)] = "2. svátek vánoční";

		// Pohyblivé: Velikonoce (západní)
		var easter = WesternEaster(year);
		var goodFriday = DateOnly.FromDateTime(easter.AddDays(-2));
		var easterMonday = DateOnly.FromDateTime(easter.AddDays(1));

		dict[goodFriday] = "Velký pátek";
		dict[easterMonday] = "Velikonoční pondělí";

		return dict;
	}

	/// <summary>
	/// Returns true if given date is a holiday.
	/// </summary>
	public static bool IsHoliday(DateOnly date) =>
		ForYear(date.Year).ContainsKey(date);

	/// <summary>
	/// Returns holiday name or null if not a holiday.
	/// </summary>
	public static string? GetHolidayName(DateOnly date)
	{
		var dict = ForYear(date.Year);
		return dict.TryGetValue(date, out var name) ? name : null;
	}

	// Butcher–Meeus algorithm for western Easter Sunday
	private static DateTime WesternEaster(int year)
	{
		int a = year % 19;
		int b = year / 100;
		int c = year % 100;
		int d = b / 4;
		int e = b % 4;
		int f = (b + 8) / 25;
		int g = (b - f + 1) / 3;
		int h = (19 * a + b - d - g + 15) % 30;
		int i = c / 4;
		int k = c % 4;
		int l = (32 + 2 * e + 2 * i - h - k) % 7;
		int m = (a + 11 * h + 22 * l) / 451;
		int month = (h + l - 7 * m + 114) / 31;
		int day = ((h + l - 7 * m + 114) % 31) + 1;
		return new DateTime(year, month, day);
	}
}
