using System.Windows;
using RetailInventoryApp.Data;

namespace RetailInventoryApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            DatabaseInitializer.Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to initialize the database:" + Environment.NewLine + ex.Message, "Startup Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }
}
