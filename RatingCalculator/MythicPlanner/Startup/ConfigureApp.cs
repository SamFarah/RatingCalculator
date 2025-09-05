namespace MythicPlanner.Startup;

public static class WebAppExtensions
{
    public static WebApplication ConfigureApp(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();
        
        // Shareable URL route
        app.MapControllerRoute(
            name: "share",
            pattern: "share/{region}/{realm}/{character}/{targetScore}",
            defaults: new { controller = "Home", action = "Share" });
            
        app.MapControllerRoute(
              name: "default",
              pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Logger.LogInformation("Starting application in {env} environment", app.Environment.EnvironmentName);

        return app;
    }
}
