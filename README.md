# Retail Inventory & Sales Manager

A small retail Inventory & Sales desktop application built with **.NET 8 (WPF)** and **SQLite**, developed as a technical assignment for the Software Engineering Internship at Hayleys Aventura (Pvt) Ltd.

## Features

- **Dashboard** - live overview of total products, low-stock alerts, today's revenue, and today's transaction count.
- **Product Management** - add, edit, and deactivate products (soft delete to preserve transaction history).
- **Sales** - search the catalog, build a cart, and complete a sale. Stock is deducted atomically inside a database transaction.
- **Stock Management** - record Stock In, Stock Out, and Adjustment movements per product, with a movement history log.
- **Transaction History** - browse past sales, view line-item detail, and export all transactions to CSV.

## Tech Stack

- **.NET 8 / WPF** - native Windows desktop UI
- **C# 12**
- **SQLite** (via Microsoft.Data.Sqlite) - file-based, zero-configuration database, ideal for a single-user desktop app
- **ADO.NET** (raw SQL) - kept dependency-light and easy to read, instead of an ORM

## Project Structure

    RetailInventoryApp/
    |-- Models/          Product, Transaction, StockMovement
    |-- Data/            DatabaseHelper, DatabaseInitializer (schema + seed data)
    |-- Repositories/    Data-access classes per entity
    |-- Services/        SalesService (atomic sale transaction logic)
    `-- Views/           WPF UserControls for each screen

## How to Run

1. Install the .NET 8 SDK or newer: https://dotnet.microsoft.com/download/dotnet/8.0
2. Clone this repository.
3. From the RetailInventoryApp folder (the one containing the .csproj), run:

       dotnet restore
       dotnet run

4. On first launch, the app creates a local SQLite database at %LocalAppData%\RetailInventoryApp\retail_inventory.db and seeds it with sample products.

## Design Notes

- Deleting a product is a soft delete (IsActive = 0), not a hard delete, since past transactions and stock movements reference the product by ID.
- Every completed sale runs inside a single database transaction: stock is re-validated, the sale and its line items are recorded, stock is deducted, and a stock movement is logged - all committed together or rolled back together on failure.
- SQLite has no native DECIMAL type; money values are stored as REAL and converted to/from decimal at the data-access layer.

## Author

Abinaya Rajasekara
