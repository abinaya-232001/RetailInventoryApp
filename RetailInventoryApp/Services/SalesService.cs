using RetailInventoryApp.Data;
using RetailInventoryApp.Models;

namespace RetailInventoryApp.Services;

public class SaleLineInput
{
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
}

public class SalesService
{
    public class SaleResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TransactionId { get; set; }
    }

    public SaleResult CompleteSale(List<SaleLineInput> lines)
    {
        if (lines == null || lines.Count == 0)
            return new SaleResult { Success = false, Message = "No items in the cart." };

        using var connection = DatabaseHelper.GetConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            foreach (var line in lines)
            {
                using var checkCmd = connection.CreateCommand();
                checkCmd.Transaction = transaction;
                checkCmd.CommandText = "SELECT StockQuantity FROM Products WHERE Id = $id";
                checkCmd.Parameters.AddWithValue("$id", line.Product.Id);
                var currentStockObj = checkCmd.ExecuteScalar();

                if (currentStockObj == null)
                {
                    transaction.Rollback();
                    return new SaleResult { Success = false, Message = "Product '" + line.Product.Name + "' no longer exists." };
                }

                var currentStock = Convert.ToInt32(currentStockObj);

                if (line.Quantity <= 0)
                {
                    transaction.Rollback();
                    return new SaleResult { Success = false, Message = "Quantity for '" + line.Product.Name + "' must be greater than zero." };
                }

                if (line.Quantity > currentStock)
                {
                    transaction.Rollback();
                    return new SaleResult { Success = false, Message = "Not enough stock for '" + line.Product.Name + "'. Available: " + currentStock + "." };
                }
            }

            var total = lines.Sum(l => l.Product.UnitPrice * l.Quantity);
            var itemCount = lines.Sum(l => l.Quantity);

            int transactionId;
            using (var insertTxCmd = connection.CreateCommand())
            {
                insertTxCmd.Transaction = transaction;
                insertTxCmd.CommandText = @"
                    INSERT INTO Transactions (TransactionDate, TotalAmount, ItemCount)
                    VALUES ($date, $total, $itemCount);
                    SELECT last_insert_rowid();";
                insertTxCmd.Parameters.AddWithValue("$date", DateTime.Now.ToString(DatabaseHelper.DateTimeFormat));
                insertTxCmd.Parameters.AddWithValue("$total", Convert.ToDouble(total));
                insertTxCmd.Parameters.AddWithValue("$itemCount", itemCount);
                transactionId = Convert.ToInt32(insertTxCmd.ExecuteScalar());
            }

            foreach (var line in lines)
            {
                var lineTotal = line.Product.UnitPrice * line.Quantity;

                using (var itemCmd = connection.CreateCommand())
                {
                    itemCmd.Transaction = transaction;
                    itemCmd.CommandText = @"
                        INSERT INTO TransactionItems (TransactionId, ProductId, ProductName, Quantity, UnitPrice, LineTotal)
                        VALUES ($txId, $productId, $productName, $qty, $unitPrice, $lineTotal)";
                    itemCmd.Parameters.AddWithValue("$txId", transactionId);
                    itemCmd.Parameters.AddWithValue("$productId", line.Product.Id);
                    itemCmd.Parameters.AddWithValue("$productName", line.Product.Name);
                    itemCmd.Parameters.AddWithValue("$qty", line.Quantity);
                    itemCmd.Parameters.AddWithValue("$unitPrice", Convert.ToDouble(line.Product.UnitPrice));
                    itemCmd.Parameters.AddWithValue("$lineTotal", Convert.ToDouble(lineTotal));
                    itemCmd.ExecuteNonQuery();
                }

                using (var updateStockCmd = connection.CreateCommand())
                {
                    updateStockCmd.Transaction = transaction;
                    updateStockCmd.CommandText = "UPDATE Products SET StockQuantity = StockQuantity - $qty WHERE Id = $id";
                    updateStockCmd.Parameters.AddWithValue("$qty", line.Quantity);
                    updateStockCmd.Parameters.AddWithValue("$id", line.Product.Id);
                    updateStockCmd.ExecuteNonQuery();
                }

                using (var movementCmd = connection.CreateCommand())
                {
                    movementCmd.Transaction = transaction;
                    movementCmd.CommandText = @"
                        INSERT INTO StockMovements (ProductId, ProductName, MovementType, Quantity, Reason, MovementDate)
                        VALUES ($productId, $productName, $type, $qty, $reason, $date)";
                    movementCmd.Parameters.AddWithValue("$productId", line.Product.Id);
                    movementCmd.Parameters.AddWithValue("$productName", line.Product.Name);
                    movementCmd.Parameters.AddWithValue("$type", StockMovementType.Sale.ToString());
                    movementCmd.Parameters.AddWithValue("$qty", -line.Quantity);
                    movementCmd.Parameters.AddWithValue("$reason", "Sale #" + transactionId);
                    movementCmd.Parameters.AddWithValue("$date", DateTime.Now.ToString(DatabaseHelper.DateTimeFormat));
                    movementCmd.ExecuteNonQuery();
                }
            }

            transaction.Commit();
            return new SaleResult { Success = true, Message = "Sale completed successfully.", TransactionId = transactionId };
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            return new SaleResult { Success = false, Message = "Sale failed: " + ex.Message };
        }
    }
}
