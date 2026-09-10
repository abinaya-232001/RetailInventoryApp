using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RetailInventoryApp.Models;
using RetailInventoryApp.Repositories;

namespace RetailInventoryApp.Views;

public partial class ProductManagementView : UserControl
{
    private readonly ProductRepository _productRepository = new();
    private int? _editingProductId;

    public ProductManagementView()
    {
        InitializeComponent();
        LoadProducts();
        ClearForm();
    }

    private void LoadProducts()
    {
        GridProducts.ItemsSource = _productRepository.GetAll(activeOnly: false);
    }

    private void GridProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GridProducts.SelectedItem is Product product)
        {
            _editingProductId = product.Id;
            TxtFormTitle.Text = "Editing: " + product.Name;
            TxtSku.Text = product.Sku;
            TxtName.Text = product.Name;
            TxtCategory.Text = product.Category;
            TxtPrice.Text = product.UnitPrice.ToString("0.00");
            TxtReorderLevel.Text = product.ReorderLevel.ToString();
            TxtOpeningStock.Text = product.StockQuantity.ToString();
            TxtOpeningStock.IsEnabled = false;
            TxtValidationMessage.Text = string.Empty;
        }
    }

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        ClearForm();
    }

    private void ClearForm()
    {
        _editingProductId = null;
        TxtFormTitle.Text = "Add New Product";
        TxtSku.Text = string.Empty;
        TxtName.Text = string.Empty;
        TxtCategory.Text = string.Empty;
        TxtPrice.Text = string.Empty;
        TxtReorderLevel.Text = string.Empty;
        TxtOpeningStock.Text = "0";
        TxtOpeningStock.IsEnabled = true;
        TxtValidationMessage.Text = string.Empty;
        GridProducts.SelectedItem = null;
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        TxtValidationMessage.Foreground = Brushes.Red;

        var sku = TxtSku.Text.Trim();
        var name = TxtName.Text.Trim();
        var category = TxtCategory.Text.Trim();

        if (string.IsNullOrWhiteSpace(sku) || string.IsNullOrWhiteSpace(name))
        {
            TxtValidationMessage.Text = "SKU and Name are required.";
            return;
        }

        if (!decimal.TryParse(TxtPrice.Text.Trim(), out var price) || price <= 0)
        {
            TxtValidationMessage.Text = "Please enter a valid unit price greater than zero.";
            return;
        }

        if (!int.TryParse(TxtReorderLevel.Text.Trim(), out var reorderLevel) || reorderLevel < 0)
        {
            TxtValidationMessage.Text = "Please enter a valid, non-negative reorder level.";
            return;
        }

        if (_productRepository.SkuExists(sku, _editingProductId ?? 0))
        {
            TxtValidationMessage.Text = "A product with this SKU already exists.";
            return;
        }

        if (_editingProductId == null)
        {
            if (!int.TryParse(TxtOpeningStock.Text.Trim(), out var openingStock) || openingStock < 0)
            {
                TxtValidationMessage.Text = "Please enter a valid, non-negative opening stock quantity.";
                return;
            }

            var newProduct = new Product
            {
                Sku = sku,
                Name = name,
                Category = category,
                UnitPrice = price,
                StockQuantity = openingStock,
                ReorderLevel = reorderLevel,
                IsActive = true
            };
            _productRepository.Insert(newProduct);
            TxtValidationMessage.Foreground = Brushes.Green;
            TxtValidationMessage.Text = "Product added successfully.";
        }
        else
        {
            var existing = _productRepository.GetById(_editingProductId.Value);
            if (existing == null)
            {
                TxtValidationMessage.Text = "This product could not be found. It may have been removed.";
                return;
            }
            existing.Sku = sku;
            existing.Name = name;
            existing.Category = category;
            existing.UnitPrice = price;
            existing.ReorderLevel = reorderLevel;
            _productRepository.Update(existing);
            TxtValidationMessage.Foreground = Brushes.Green;
            TxtValidationMessage.Text = "Product updated successfully.";
        }

        LoadProducts();
        ClearForm();
    }

    private void BtnDeactivate_Click(object sender, RoutedEventArgs e)
    {
        if (GridProducts.SelectedItem is not Product product)
        {
            TxtValidationMessage.Foreground = Brushes.OrangeRed;
            TxtValidationMessage.Text = "Select a product from the list first.";
            return;
        }

        var result = MessageBox.Show(
            "Deactivate '" + product.Name + "'? It will no longer appear in Sales but its history is kept.",
            "Confirm Deactivate", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _productRepository.Deactivate(product.Id);
            LoadProducts();
            ClearForm();
        }
    }
}
