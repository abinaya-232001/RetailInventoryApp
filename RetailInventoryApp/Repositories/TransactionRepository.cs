using System.Globalization;
using RetailInventoryApp.Data;
using RetailInventoryApp.Models;

namespace RetailInventoryApp.Repositories;

public class TransactionRepository
{
    public List<SaleTransaction> GetAll()
    {
        var list = new List<SaleTransaction>();
        using var connection = DatabaseHelper.GetConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Transactions ORDER BY TransactionDate DESC";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new SaleTransaction
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                TransactionDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("TransactionDate")), CultureInfo.InvariantCulture),
                TotalAmount = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("TotalAmount"))),
                ItemCount = reader.GetInt32(reader.GetOrdinal("ItemCount"))
            });
        }
        return list;
    }

    public List<TransactionItem> GetItems(int transactionId)
    {
        var list = new List<TransactionItem>();
        using var connection = DatabaseHelper.GetConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM TransactionItems WHERE TransactionId = $txId";
        cmd.Parameters.AddWithValue("$txId", transactionId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new TransactionItem
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                TransactionId = reader.GetInt32(reader.GetOrdinal("TransactionId")),
                ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                UnitPrice = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("UnitPrice"))),
                LineTotal = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("LineTotal")))
            });
        }
        return list;
    }

    public decimal GetTodayRevenue()
    {
        using var connection = DatabaseHelper.GetConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(SUM(TotalAmount), 0) FROM Transactions WHERE date(TransactionDate) = date('now', 'localtime')";
        var result = cmd.ExecuteScalar();
        return result == null || result is DBNull ? 0m : Convert.ToDecimal(Convert.ToDouble(result));
    }

    public int GetTodayTransactionCount()
    {
        using var connection = DatabaseHelper.GetConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Transactions WHERE date(TransactionDate) = date('now', 'localtime')";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
}
