using BackEndCore;
using BackEndCore.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MVVM_Core.Services;
using MVVM_Core.ViewModels;
using Roulette_Bot.Helpers;
using System.Windows;

namespace Roulette_Bot;

public partial class App : Application
{
    public static IHost? AppHost { get; private set; }    

    public static string AppVersion { get; } = GetAppVersion();

    public static string FullTitle => $"Roulette Bot {AppVersion}";

    private static string GetAppVersion()
    {
        // Gets the exact ClickOnce publish version when installed via setup.exe
        string? clickOnceVersion = Environment.GetEnvironmentVariable("ClickOnce_CurrentVersion");
        if (!string.IsNullOrEmpty(clickOnceVersion))
            return clickOnceVersion;

        // Fallback when running from Visual Studio (debug)
        var assemblyVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        return assemblyVersion?.ToString(3) ?? "1.2.0";
    }

    // Constructor stays almost empty – no heavy work here
    public App() { }

    protected override async void OnStartup(StartupEventArgs e)
    {
        // 1. Show splash screen IMMEDIATELY (user sees something right away)
        var splash = new SplashWindow();
        splash.Show();

        try
        {
            // 2. Now build the host (CasinoSite constructor will run when the service is first resolved)
            AppHost = Host.CreateDefaultBuilder()
                 .ConfigureAppConfiguration((hostingContext, config) =>
                 {
                     config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                           .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                           .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true);
                 })
                 .ConfigureServices((hostContext, services) =>
                 {
                     // Singleton → same instance for entire app lifetime                     
                     services.AddSingleton<IUIService, WpfUIService>();

                     services.AddSingleton<ICasinoSiteService, CasinoSite>();
                     services.AddSingleton<SettingsService>();

                     services.AddSingleton<SettingsViewModel>();
                     services.AddSingleton<HomeViewModel>();                   

                     services.AddSingleton<MainViewModel>();
                     services.AddSingleton<MainWindow>();
                 })
                 .Build();

            await AppHost.StartAsync();
            var startupForm = AppHost.Services.GetRequiredService<MainWindow>();

            await Task.Delay(TimeSpan.FromSeconds(5));

            // 3. Close splash and show the real window
            splash.Close();

            startupForm.Show();
        }
        catch (Exception ex)
        {
            // 4. Something went wrong → close splash and show friendly error
            splash.Close();

            MessageBox.Show(
                $"Failed to start Roulette Bot.\n\n" +
                $"Error: {ex.Message}\n\n" +
                $"Inner error: {ex.InnerException?.Message ?? "None"}.",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            // Shut down the application cleanly
            Current.Shutdown(1);
        }
       
        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await AppHost!.StopAsync();
        //AppHost.Dispose();

        base.OnExit(e);
    }
}
