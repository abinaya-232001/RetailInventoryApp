using System.IO;
using Microsoft.Data.Sqlite;

namespace RetailInventoryApp.Data;

public static class DatabaseHelper
{
    private static readonly string DbFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RetailInventoryApp");

    private static readonly string DbPath = Path.Combine(DbFolder, "retail_inventory.db");

    public static string ConnectionString => "Data Source=" + DbPath;

    public static SqliteConnection GetConnection()
    {
        Directory.CreateDirectory(DbFolder);
        var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        return connection;
    }

    public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
}
