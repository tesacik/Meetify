using Meetify.Areas.Identity;
using Meetify.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();

//builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();
builder.Services.AddSingleton<WeatherForecastService>();

// Auth
builder.Services
	.AddAuthentication(options =>
	{
		options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
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
		options.Scope.Add("email");   // belt & suspenders; email is usually included
		options.SaveTokens = true;    // handy if you’ll call Google APIs later
		// options.CallbackPath = "/signin-google"; // default; uncomment only if you change it
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
