using System.Globalization;
using RetailInventoryApp.Data;
using RetailInventoryApp.Models;

namespace RetailInventoryApp.Repositories;

public class StockRepository
{
    public List<StockMovement> GetMovementsForProduct(int productId)
    {
        var list = new List<StockMovement>();
        using var connection = DatabaseHelper.GetConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM StockMovements WHERE ProductId = $id ORDER BY MovementDate DESC";
        cmd.Parameters.AddWithValue("$id", productId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new StockMovement
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                MovementType = Enum.Parse<StockMovementType>(reader.GetString(reader.GetOrdinal("MovementType"))),
                Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                Reason = reader.GetString(reader.GetOrdinal("Reason")),
                MovementDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("MovementDate")), CultureInfo.InvariantCulture)
            });
        }
        return list;
    }

    public void AdjustStock(int productId, string productName, int changeQuantity, StockMovementType type, string reason)
    {
        using var connection = DatabaseHelper.GetConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            using (var updateCmd = connection.CreateCommand())
            {
                updateCmd.Transaction = transaction;
                updateCmd.CommandText = "UPDATE Products SET StockQuantity = StockQuantity + $change WHERE Id = $id";
                updateCmd.Parameters.AddWithValue("$change", changeQuantity);
                updateCmd.Parameters.AddWithValue("$id", productId);
                updateCmd.ExecuteNonQuery();
            }

            using (var logCmd = connection.CreateCommand())
            {
                logCmd.Transaction = transaction;
                logCmd.CommandText = @"
                    INSERT INTO StockMovements (ProductId, ProductName, MovementType, Quantity, Reason, MovementDate)
                    VALUES ($productId, $productName, $type, $qty, $reason, $date)";
                logCmd.Parameters.AddWithValue("$productId", productId);
                logCmd.Parameters.AddWithValue("$productName", productName);
                logCmd.Parameters.AddWithValue("$type", type.ToString());
                logCmd.Parameters.AddWithValue("$qty", changeQuantity);
                logCmd.Parameters.AddWithValue("$reason", reason);
                logCmd.Parameters.AddWithValue("$date", DateTime.Now.ToString(DatabaseHelper.DateTimeFormat));
                logCmd.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
