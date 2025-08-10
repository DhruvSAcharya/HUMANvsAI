using WebUi.Business;
using WebUi.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<RoomManager>();
builder.Services.AddSingleton<PlayerManager>();
builder.Services.AddSingleton<VoteManager>();
builder.Services.AddSingleton<BotManager>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapHub<ChatHub>("/chathub");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
