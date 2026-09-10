using System.Windows;
using RetailInventoryApp.Views;

namespace RetailInventoryApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        MainContent.Content = new DashboardView();
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender == BtnDashboard) MainContent.Content = new DashboardView();
        else if (sender == BtnProducts) MainContent.Content = new ProductManagementView();
        else if (sender == BtnSales) MainContent.Content = new SalesView();
        else if (sender == BtnStock) MainContent.Content = new StockManagementView();
        else if (sender == BtnHistory) MainContent.Content = new TransactionHistoryView();
    }
}
