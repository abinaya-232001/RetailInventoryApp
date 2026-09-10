using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RetailInventoryApp.Repositories;

namespace RetailInventoryApp.Views;

public partial class DashboardView : UserControl
{
    private readonly ProductRepository _productRepository = new();
    private readonly TransactionRepository _transactionRepository = new();

    public DashboardView()
    {
        InitializeComponent();
        LoadData();
    }

    private void LoadData()
    {
        var products = _productRepository.GetAll();
        var lowStock = products.Where(p => p.StockQuantity <= p.ReorderLevel).OrderBy(p => p.StockQuantity).ToList();

        TxtTotalProducts.Text = products.Count.ToString();
        TxtLowStock.Text = lowStock.Count.ToString();
        TxtTodayRevenue.Text = "Rs. " + _transactionRepository.GetTodayRevenue().ToString("N2");
        TxtTodayTx.Text = _transactionRepository.GetTodayTransactionCount().ToString();

        GridLowStock.ItemsSource = lowStock;
        TxtNoLowStock.Visibility = lowStock.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        var allTransactions = _transactionRepository.GetAll();
        var topProducts = allTransactions
            .SelectMany(t => _transactionRepository.GetItems(t.Id))
            .GroupBy(i => i.ProductName)
            .Select(g => new TopProductRow { ProductName = g.Key, TotalQuantity = g.Sum(i => i.Quantity) })
            .OrderByDescending(r => r.TotalQuantity)
            .Take(3)
            .ToList();

        GridTopProducts.ItemsSource = topProducts;
        TxtNoTopProducts.Visibility = topProducts.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private class TopProductRow
    {
        public string ProductName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
    }
}
