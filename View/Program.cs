
using View.Components;

namespace View
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            string apiBaseUrl = builder.Configuration["ApiBaseUrl"]
                ?? throw new InvalidOperationException(
                    "ApiBaseUrl not found in appsettings.json.");

            builder.Services.AddScoped(sp =>
                new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

            builder.Services.AddScoped<View.Services.UserSession>();

            builder.Services.AddScoped<View.Services.ShoppingCartService>();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStatusCodePagesWithReExecute(
                "/not-found",
                createScopeForStatusCodePages: true);

            app.UseAntiforgery();

            app.MapStaticAssets();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}

