using Meetify.Data;
using Meetify.Services;
using Meetify.Hubs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(connectionString));
builder.Services.AddDbContextFactory<ApplicationDbContext>(opt =>
	opt.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, EmailUserIdProvider>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<SlotService>();
builder.Services.AddScoped<GoogleUserService>();

// Auth
builder.Services
	.AddAuthentication(options =>
	{
		options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
		options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
		options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
	})
	.AddCookie(options =>
	{
		options.LoginPath = "/auth/login";
		options.LogoutPath = "/auth/logout";
		options.SlidingExpiration = true;
	})
	.AddGoogle(options =>
	{
		options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
		options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
		options.Scope.Add("email");
		options.SaveTokens = true;
		options.Events.OnCreatingTicket = async ctx =>
		{
			var email = ctx.Principal?.FindFirst(ClaimTypes.Email)?.Value
				?? ctx.Principal?.FindFirst("email")?.Value;
			var given = ctx.Principal?.FindFirst(ClaimTypes.GivenName)?.Value
				?? (ctx.User.TryGetProperty("given_name", out var g) ? g.GetString() : null);
			var family = ctx.Principal?.FindFirst(ClaimTypes.Surname)?.Value
				?? (ctx.User.TryGetProperty("family_name", out var f) ? f.GetString() : null);

			if (!string.IsNullOrWhiteSpace(email))
			{
				var svc = ctx.HttpContext.RequestServices.GetRequiredService<GoogleUserService>();
				await svc.UpsertFromClaimsAsync(email!, given, family);
			}
		};
	});
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<AppointmentHub>("/hubs/appointments");
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.MapGet("/diag", (HttpContext c) =>
{
	var baseUrl = $"{c.Request.Scheme}://{c.Request.Host}{c.Request.PathBase}";
	return Results.Text($"Expected redirect: {baseUrl}/signin-google");
});

app.MapGet("/diag-id", (IConfiguration cfg) =>
{
	var cid = cfg["Authentication:Google:ClientId"] ?? "(null)";
	var sec = cfg["Authentication:Google:ClientSecret"] ?? "(null)";
	// mask middle for safety
	var maskedCid = cid.Length > 10 ? $"{cid[..6]}…{cid[^6..]}" : cid;
	var maskedSecret = cid.Length > 10 ? $"{sec[..6]}…{sec[^6..]}" : sec;

	var sb = new StringBuilder();
	sb.AppendLine($"Google ClientId in use: {maskedCid}");
	sb.AppendLine($"Google ClientSecret in use: {maskedSecret}");

	return Results.Text(sb.ToString());
});

app.Run();
