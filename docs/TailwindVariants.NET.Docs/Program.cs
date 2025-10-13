using TailwindVariants.NET.Docs.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

builder.Services.AddTailwindVariants();
builder.Services.AddJSComponents();

builder.Services.AddApplicationState();
builder.Services.AddSingleton<MarkdownRenderer>();

builder.Services.AddHttpClient<GitHubClient>(client =>
{
	client.DefaultRequestHeaders.UserAgent.ParseAdd("TailwindVariants.NET.Docs");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.Run();
