using System.Globalization;
using Microsoft.Data.Sqlite;
using RetailInventoryApp.Data;
using RetailInventoryApp.Models;

namespace RetailInventoryApp.Repositories;

public class ProductRepository
{
    public List<Product> GetAll(bool activeOnly = true)
    {
        var products = new List<Product>();
        using var connection = DatabaseHelper.GetConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = activeOnly
            ? "SELECT * FROM Products WHERE IsActive = 1 ORDER BY Name"
            : "SELECT * FROM Products ORDER BY Name";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            products.Add(MapProduct(reader));
        }
        return products;
    }

    public Product? GetById(int id)
    {
        using var connection = DatabaseHelper.GetConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM Products WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();
        return reader.Read() ? MapProduct(reader) : null;
    }

    public bool SkuExists(string sku, int excludeId = 0)
    {
        using var connection = DatabaseHelper.GetConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Products WHERE Sku = $sku AND Id != $excludeId";
        cmd.Parameters.AddWithValue("$sku", sku);
        cmd.Parameters.AddWithValue("$excludeId", excludeId);
        var count = (long)cmd.ExecuteScalar()!;
        return count > 0;
    }

    public int Insert(Product product)
    {
        using var connection = DatabaseHelper.GetConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Products (Sku, Name, Category, UnitPrice, StockQuantity, ReorderLevel, IsActive, CreatedAt)
            VALUES ($sku, $name, $category, $price, $stock, $reorder, $active, $createdAt);
            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$sku", product.Sku);
        cmd.Parameters.AddWithValue("$name", product.Name);
        cmd.Parameters.AddWithValue("$category", product.Category);
        cmd.Parameters.AddWithValue("$price", Convert.ToDouble(product.UnitPrice));
        cmd.Parameters.AddWithValue("$stock", product.StockQuantity);
        cmd.Parameters.AddWithValue("$reorder", product.ReorderLevel);
        cmd.Parameters.AddWithValue("$active", product.IsActive ? 1 : 0);
        cmd.Parameters.AddWithValue("$createdAt", DateTime.Now.ToString(DatabaseHelper.DateTimeFormat));

        var newId = (long)cmd.ExecuteScalar()!;
        return (int)newId;
    }

    public void Update(Product product)
    {
        using var connection = DatabaseHelper.GetConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            UPDATE Products
            SET Sku = $sku, Name = $name, Category = $category, UnitPrice = $price,
                ReorderLevel = $reorder, IsActive = $active
            WHERE Id = $id";
        cmd.Parameters.AddWithValue("$sku", product.Sku);
        cmd.Parameters.AddWithValue("$name", product.Name);
        cmd.Parameters.AddWithValue("$category", product.Category);
        cmd.Parameters.AddWithValue("$price", Convert.ToDouble(product.UnitPrice));
        cmd.Parameters.AddWithValue("$reorder", product.ReorderLevel);
        cmd.Parameters.AddWithValue("$active", product.IsActive ? 1 : 0);
        cmd.Parameters.AddWithValue("$id", product.Id);
        cmd.ExecuteNonQuery();
    }

    public void Deactivate(int id)
    {
        using var connection = DatabaseHelper.GetConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE Products SET IsActive = 0 WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    private static Product MapProduct(SqliteDataReader reader)
    {
        return new Product
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Sku = reader.GetString(reader.GetOrdinal("Sku")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            Category = reader.GetString(reader.GetOrdinal("Category")),
            UnitPrice = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("UnitPrice"))),
            StockQuantity = reader.GetInt32(reader.GetOrdinal("StockQuantity")),
            ReorderLevel = reader.GetInt32(reader.GetOrdinal("ReorderLevel")),
            IsActive = reader.GetInt32(reader.GetOrdinal("IsActive")) == 1,
            CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt")), CultureInfo.InvariantCulture)
        };
    }
}
