using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using RetailInventoryApp.Models;
using RetailInventoryApp.Repositories;

namespace RetailInventoryApp.Views;

public partial class TransactionHistoryView : UserControl
{
    private readonly TransactionRepository _transactionRepository = new();
    private List<SaleTransaction> _allTransactions = new();
    private bool _isLoaded;

    public TransactionHistoryView()
    {
        InitializeComponent();
        LoadTransactions();
        _isLoaded = true;
    }

    private void LoadTransactions()
    {
        _allTransactions = _transactionRepository.GetAll();
        ApplyFilters();
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void DateRange_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoaded) ApplyFilters();
    }

    private void BtnClearDates_Click(object sender, RoutedEventArgs e)
    {
        DpFrom.SelectedDate = null;
        DpTo.SelectedDate = null;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var term = TxtSearch.Text.Trim().ToLowerInvariant();
        IEnumerable<SaleTransaction> filtered = _allTransactions;

        if (!string.IsNullOrEmpty(term))
        {
            filtered = filtered.Where(t => t.Id.ToString().Contains(term) || t.TransactionDate.ToString("yyyy-MM-dd").Contains(term));
        }

        if (DpFrom.SelectedDate.HasValue)
        {
            var from = DpFrom.SelectedDate.Value.Date;
            filtered = filtered.Where(t => t.TransactionDate.Date >= from);
        }

        if (DpTo.SelectedDate.HasValue)
        {
            var to = DpTo.SelectedDate.Value.Date;
            filtered = filtered.Where(t => t.TransactionDate.Date <= to);
        }

        var result = filtered.ToList();
        GridTransactions.ItemsSource = result;
        TxtNoTransactions.Visibility = result.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void GridTransactions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GridTransactions.SelectedItem is SaleTransaction transaction)
        {
            TxtDetailTitle.Text = "Transaction #" + transaction.Id + " — " + transaction.TransactionDate.ToString("yyyy-MM-dd HH:mm");
            GridDetailItems.ItemsSource = _transactionRepository.GetItems(transaction.Id);
            TxtDetailTotal.Text = "Grand Total: Rs. " + transaction.TotalAmount.ToString("N2");
        }
    }

    private void BtnExportCsv_Click(object sender, RoutedEventArgs e)
    {
        var transactions = _transactionRepository.GetAll();

        if (transactions.Count == 0)
        {
            MessageBox.Show("There are no transactions to export.", "Export CSV", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV file (*.csv)|*.csv",
            FileName = "TransactionHistory_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv"
        };

        if (dialog.ShowDialog() == true)
        {
            var sb = new StringBuilder();
            sb.AppendLine("TransactionId,DateTime,ItemCount,TotalAmount");
            foreach (var t in transactions)
            {
                sb.AppendLine(t.Id + "," + t.TransactionDate.ToString("yyyy-MM-dd HH:mm:ss") + "," + t.ItemCount + "," + t.TotalAmount.ToString("F2"));
            }

            File.WriteAllText(dialog.FileName, sb.ToString());

            MessageBox.Show("Exported " + transactions.Count + " transaction(s) to:" + Environment.NewLine + dialog.FileName,
                "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
