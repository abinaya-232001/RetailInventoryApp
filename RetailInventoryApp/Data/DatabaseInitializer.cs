using Microsoft.Data.Sqlite;

namespace RetailInventoryApp.Data;

public static class DatabaseInitializer
{
    public static void Initialize()
    {
        using var connection = DatabaseHelper.GetConnection();

        const string createTables = @"
            CREATE TABLE IF NOT EXISTS Products (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Sku TEXT NOT NULL UNIQUE,
                Name TEXT NOT NULL,
                Category TEXT NOT NULL DEFAULT '',
                UnitPrice REAL NOT NULL DEFAULT 0,
                StockQuantity INTEGER NOT NULL DEFAULT 0,
                ReorderLevel INTEGER NOT NULL DEFAULT 0,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Transactions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TransactionDate TEXT NOT NULL,
                TotalAmount REAL NOT NULL,
                ItemCount INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS TransactionItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TransactionId INTEGER NOT NULL,
                ProductId INTEGER NOT NULL,
                ProductName TEXT NOT NULL,
                Quantity INTEGER NOT NULL,
                UnitPrice REAL NOT NULL,
                LineTotal REAL NOT NULL,
                FOREIGN KEY (TransactionId) REFERENCES Transactions(Id),
                FOREIGN KEY (ProductId) REFERENCES Products(Id)
            );

            CREATE TABLE IF NOT EXISTS StockMovements (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ProductId INTEGER NOT NULL,
                ProductName TEXT NOT NULL,
                MovementType TEXT NOT NULL,
                Quantity INTEGER NOT NULL,
                Reason TEXT NOT NULL DEFAULT '',
                MovementDate TEXT NOT NULL,
                FOREIGN KEY (ProductId) REFERENCES Products(Id)
            );";

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = createTables;
            cmd.ExecuteNonQuery();
        }

        SeedSampleData(connection);
    }

    private static void SeedSampleData(SqliteConnection connection)
    {
        using (var checkCmd = connection.CreateCommand())
        {
            checkCmd.CommandText = "SELECT COUNT(*) FROM Products";
            var count = (long)checkCmd.ExecuteScalar()!;
            if (count > 0) return;
        }

        var sampleProducts = new (string Sku, string Name, string Category, decimal Price, int Stock, int Reorder)[]
        {
            ("SKU-1001", "Ballpoint Pen (Blue)", "Stationery", 45.00m, 150, 20),
            ("SKU-1002", "A4 Exercise Book", "Stationery", 120.00m, 80, 15),
            ("SKU-1003", "500ml Drinking Water", "Beverages", 60.00m, 200, 30),
            ("SKU-1004", "Milk Powder 400g", "Grocery", 850.00m, 40, 10),
            ("SKU-1005", "Instant Noodles Pack", "Grocery", 95.00m, 100, 20),
            ("SKU-1006", "USB Flash Drive 32GB", "Electronics", 1450.00m, 12, 5),
        };

        foreach (var p in sampleProducts)
        {
            using var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = @"
                INSERT INTO Products (Sku, Name, Category, UnitPrice, StockQuantity, ReorderLevel, IsActive, CreatedAt)
                VALUES ($sku, $name, $category, $price, $stock, $reorder, 1, $createdAt)";
            insertCmd.Parameters.AddWithValue("$sku", p.Sku);
            insertCmd.Parameters.AddWithValue("$name", p.Name);
            insertCmd.Parameters.AddWithValue("$category", p.Category);
            insertCmd.Parameters.AddWithValue("$price", Convert.ToDouble(p.Price));
            insertCmd.Parameters.AddWithValue("$stock", p.Stock);
            insertCmd.Parameters.AddWithValue("$reorder", p.Reorder);
            insertCmd.Parameters.AddWithValue("$createdAt", DateTime.Now.ToString(DatabaseHelper.DateTimeFormat));
            insertCmd.ExecuteNonQuery();
        }
    }
}
