using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RetailInventoryApp.Models;
using RetailInventoryApp.Repositories;
using RetailInventoryApp.Services;

namespace RetailInventoryApp.Views;

public partial class SalesView : UserControl
{
    private readonly ProductRepository _productRepository = new();
    private readonly SalesService _salesService = new();
    private List<Product> _allProducts = new();
    private readonly List<TransactionItem> _cart = new();

    public SalesView()
    {
        InitializeComponent();
        LoadCatalog();
        RefreshCartGrid();
    }

    private void LoadCatalog()
    {
        _allProducts = _productRepository.GetAll().Where(p => p.StockQuantity > 0).ToList();
        GridCatalog.ItemsSource = _allProducts;
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        var term = TxtSearch.Text.Trim().ToLowerInvariant();
        GridCatalog.ItemsSource = string.IsNullOrEmpty(term)
            ? _allProducts
            : _allProducts.Where(p => p.Name.ToLowerInvariant().Contains(term) || p.Sku.ToLowerInvariant().Contains(term)).ToList();
    }

    private void BtnAddToCart_Click(object sender, RoutedEventArgs e)
    {
        TxtSalesMessage.Text = string.Empty;

        if (GridCatalog.SelectedItem is not Product product)
        {
            TxtSalesMessage.Text = "Select a product from the catalog first.";
            return;
        }

        if (!int.TryParse(TxtQty.Text.Trim(), out var qty) || qty <= 0)
        {
            TxtSalesMessage.Text = "Enter a valid quantity (whole number greater than zero).";
            return;
        }

        var existingLine = _cart.FirstOrDefault(l => l.ProductId == product.Id);
        var quantityAlreadyInCart = existingLine != null ? existingLine.Quantity : 0;

        if (quantityAlreadyInCart + qty > product.StockQuantity)
        {
            TxtSalesMessage.Text = "Only " + product.StockQuantity + " unit(s) of '" + product.Name + "' are in stock.";
            return;
        }

        if (existingLine != null)
        {
            existingLine.Quantity += qty;
            existingLine.LineTotal = existingLine.Quantity * existingLine.UnitPrice;
        }
        else
        {
            _cart.Add(new TransactionItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = qty,
                UnitPrice = product.UnitPrice,
                LineTotal = product.UnitPrice * qty
            });
        }

        TxtQty.Text = "1";
        RefreshCartGrid();
    }

    private void BtnRemoveLine_Click(object sender, RoutedEventArgs e)
    {
        if (GridCart.SelectedItem is TransactionItem line)
        {
            _cart.Remove(line);
            RefreshCartGrid();
        }
    }

    private void BtnClearCart_Click(object sender, RoutedEventArgs e)
    {
        _cart.Clear();
        RefreshCartGrid();
    }

    private void RefreshCartGrid()
    {
        GridCart.ItemsSource = null;
        GridCart.ItemsSource = _cart;
        TxtEmptyCart.Visibility = _cart.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        var total = _cart.Sum(l => l.LineTotal);
        TxtGrandTotal.Text = "Total: Rs. " + total.ToString("N2");
    }

    private void BtnCompleteSale_Click(object sender, RoutedEventArgs e)
    {
        TxtSalesMessage.Text = string.Empty;

        if (_cart.Count == 0)
        {
            TxtSalesMessage.Text = "The cart is empty. Add at least one product before completing the sale.";
            return;
        }

        var lines = _cart.Select(c => new SaleLineInput
        {
            Product = _allProducts.First(p => p.Id == c.ProductId),
            Quantity = c.Quantity
        }).ToList();

        var result = _salesService.CompleteSale(lines);

        if (!result.Success)
        {
            TxtSalesMessage.Text = result.Message;
            return;
        }

        MessageBox.Show("Sale #" + result.TransactionId + " completed successfully." + System.Environment.NewLine + "Total: Rs. " + _cart.Sum(l => l.LineTotal).ToString("N2"),
            "Sale Complete", MessageBoxButton.OK, MessageBoxImage.Information);

        _cart.Clear();
        RefreshCartGrid();
        LoadCatalog();
    }
}
