using System.Reflection;
using PlanningPoker;
using PlanningPoker.Server;

var builder = WebApplication.CreateBuilder(
    new WebApplicationOptions
    {
        Args = args,
        ContentRootPath = "./"
    }
);

if (Environment.GetEnvironmentVariable("PORT") is not null and { Length: > 0 } portVar && int.TryParse(portVar, out int port))
{
    builder.WebHost.ConfigureKestrel(
        options =>
        {
            options.ListenAnyIP(port);
        }
    );
}

builder.Services.AddSignalR();

builder.Services.AddRazorPages();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapHub<SessionHub>("/sessions/hub");
app.MapFallbackToFile("index.html");

app.Run();
