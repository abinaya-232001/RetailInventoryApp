using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RetailInventoryApp.Models;
using RetailInventoryApp.Repositories;

namespace RetailInventoryApp.Views;

public partial class StockManagementView : UserControl
{
    private readonly ProductRepository _productRepository = new();
    private readonly StockRepository _stockRepository = new();
    private Product? _selectedProduct;
    private List<Product> _allProducts = new();

    public StockManagementView()
    {
        InitializeComponent();
        LoadProducts();
    }

    private void LoadProducts()
    {
        _allProducts = _productRepository.GetAll(activeOnly: false);
        GridStock.ItemsSource = _allProducts;
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        var term = TxtSearch.Text.Trim().ToLowerInvariant();
        GridStock.ItemsSource = string.IsNullOrEmpty(term)
            ? _allProducts
            : _allProducts.Where(p => p.Name.ToLowerInvariant().Contains(term) || p.Sku.ToLowerInvariant().Contains(term)).ToList();
    }

    private void GridStock_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GridStock.SelectedItem is Product product)
        {
            _selectedProduct = product;
            TxtSelectedProduct.Text = product.Name + "  (Current stock: " + product.StockQuantity + ")";
            TxtStockMessage.Text = string.Empty;
            var movements = _stockRepository.GetMovementsForProduct(product.Id);
            GridMovements.ItemsSource = movements;
            TxtNoMovements.Visibility = movements.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void BtnApplyAdjustment_Click(object sender, RoutedEventArgs e)
    {
        TxtStockMessage.Text = string.Empty;

        if (_selectedProduct == null)
        {
            TxtStockMessage.Text = "Select a product from the list first.";
            return;
        }

        if (!int.TryParse(TxtAdjustQty.Text.Trim(), out var qty) || qty <= 0)
        {
            TxtStockMessage.Text = "Enter a valid quantity greater than zero.";
            return;
        }

        var selectedItem = (ComboBoxItem)CmbMovementType.SelectedItem;
        var tag = selectedItem.Tag != null ? selectedItem.Tag.ToString() : "StockIn";

        int changeQuantity;
        StockMovementType movementType;

        switch (tag)
        {
            case "StockOut":
                movementType = StockMovementType.StockOut;
                changeQuantity = -qty;
                break;
            case "Adjustment":
                movementType = StockMovementType.Adjustment;
                changeQuantity = qty;
                break;
            default:
                movementType = StockMovementType.StockIn;
                changeQuantity = qty;
                break;
        }

        if (changeQuantity < 0 && Math.Abs(changeQuantity) > _selectedProduct.StockQuantity)
        {
            TxtStockMessage.Text = "Cannot remove " + qty + " units — only " + _selectedProduct.StockQuantity + " in stock.";
            return;
        }

        var reason = string.IsNullOrWhiteSpace(TxtReason.Text) ? tag : TxtReason.Text.Trim();

        _stockRepository.AdjustStock(_selectedProduct.Id, _selectedProduct.Name, changeQuantity, movementType, reason ?? string.Empty);

        TxtAdjustQty.Text = string.Empty;
        TxtReason.Text = string.Empty;
        LoadProducts();

        var refreshed = _productRepository.GetById(_selectedProduct.Id);
        if (refreshed != null)
        {
            _selectedProduct = refreshed;
            TxtSelectedProduct.Text = refreshed.Name + "  (Current stock: " + refreshed.StockQuantity + ")";
        }
        var movements = _stockRepository.GetMovementsForProduct(_selectedProduct.Id);
        GridMovements.ItemsSource = movements;
        TxtNoMovements.Visibility = movements.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}
