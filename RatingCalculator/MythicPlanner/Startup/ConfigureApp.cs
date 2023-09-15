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
        app.MapControllerRoute(
              name: "default",
              pattern: "{controller=Home}/{action=Index}/{id?}");

        return app;
    }
}
