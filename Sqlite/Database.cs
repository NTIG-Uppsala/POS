// See https://aka.ms/new-console-template for more information
using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace Checkout
{
    public static class Database
    {
        public static string AppFolder =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Checkout"
            );

        private static string? _overrideDbPath;

        public static void UseDatabase(string path)
        {
            _overrideDbPath = path;
        }

        public static string DbPath =>
            _overrideDbPath ??
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Checkout",
                "Database.db"
            );


        public static void EnsureCreated()
        {
            Directory.CreateDirectory(AppFolder);

            using var connection = new SqliteConnection($"Data Source={DbPath}");
            connection.Open();

            // Slå på foreign keys
            using var pragma = new SqliteCommand("PRAGMA foreign_keys = ON;", connection);
            pragma.ExecuteNonQuery();

            // Skapa tabeller
            string createCategories = @"
        CREATE TABLE IF NOT EXISTS Categories (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL UNIQUE
        );";
            new SqliteCommand(createCategories, connection).ExecuteNonQuery();

            string createProducts = @"
        CREATE TABLE IF NOT EXISTS Products (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CategoryId INTEGER NOT NULL,
            Name TEXT NOT NULL UNIQUE,
            Price REAL NOT NULL,
            Inventory INTEGER NOT NULL,
            Sold INTEGER NOT NULL DEFAULT 0,
            FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
        );";
            new SqliteCommand(createProducts, connection).ExecuteNonQuery();

            // Kolla om produkterna redan finns
            using var checkCmd = new SqliteCommand("SELECT COUNT(*) FROM Products;", connection);
            long count = (long)checkCmd.ExecuteScalar();

            if (count == 0)
            {
                // Endast seed om tabellen är tom
                SeedData(connection);
            }
        } 

        private static void SeedData(SqliteConnection connection)
        {
            string[] categories = { "Tobak", "Godis", "Enkel mat", "Tidningar" };

            foreach (var cat in categories)
            {
                var cmd = new SqliteCommand(
                    "INSERT OR IGNORE INTO Categories (Name) VALUES (@name)",
                    connection);
                cmd.Parameters.AddWithValue("@name", cat);
                cmd.ExecuteNonQuery();
            }

            var products = new (string Cat, string Name, double Price, int Inventory, int Sold)[]
                {
                    ("Tobak", "Marlboro Red (20-pack)", 89, 100, 0),
                    ("Tobak", "Camel Blue (20-pack)", 85, 100, 0),
                    ("Tobak", "L&M Filter (20-pack)", 79, 100, 0),
                    ("Tobak", "Skruf Original Portion", 62, 100, 0),
                    ("Tobak", "Göteborgs Rapé White Portion", 67, 100, 0),

                    ("Godis", "Marabou Mjölkchoklad 100 g", 25, 100, 0),
                    ("Godis", "Daim dubbel", 15, 100, 0),
                    ("Godis", "Kexchoklad", 12, 100, 0),
                    ("Godis", "Malaco Gott & Blandat 160 g", 28, 100, 0),

                    ("Enkel mat", "Korv med bröd", 25, 100, 0),
                    ("Enkel mat", "Varm toast (ost & skinka)", 30, 100, 0),
                    ("Enkel mat", "Pirog (Köttfärs)", 22, 100, 0),
                    ("Enkel mat", "Färdig sallad (kyckling)", 49, 100, 0),
                    ("Enkel mat", "Panini (mozzarella & pesto)", 45, 100, 0),

                    ("Tidningar", "Aftonbladet (dagens)", 28, 100, 0),
                    ("Tidningar", "Expressen (Dagens)", 28, 100, 0),
                    ("Tidningar", "Illustrerad Vetenskap", 79, 100, 0),
                    ("Tidningar", "Kalle Anka & Co", 45, 100, 0),
                    ("Tidningar", "Allt om Mat", 69, 100, 0)
                };

                foreach (var p in products)
                {
                    var cmd = new SqliteCommand(@"
                    INSERT OR IGNORE INTO Products (CategoryId, Name, Price, Inventory, Sold)
                    VALUES ((SELECT Id FROM Categories WHERE Name=@c), @n, @p, @I, @S)",
                        connection);

                    cmd.Parameters.AddWithValue("@c", p.Cat);
                    cmd.Parameters.AddWithValue("@n", p.Name);
                    cmd.Parameters.AddWithValue("@p", p.Price);
                    cmd.Parameters.AddWithValue("@I", p.Inventory);
                    cmd.Parameters.AddWithValue("@S", p.Sold);
                    cmd.ExecuteNonQuery();
                }

            }
    }
}
