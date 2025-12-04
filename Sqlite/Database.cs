// See https://aka.ms/new-console-template for more information
using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main()
    {
        try
        {
            string folderPath = @"..\..\..\";
            Directory.CreateDirectory(folderPath);
            string dbPath = Path.Combine(folderPath, "Database.db");

            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            using (var pragma = new SqliteCommand("PRAGMA foreign_keys = ON;", connection))
                pragma.ExecuteNonQuery();

            Console.WriteLine("Connected to SQLite database.");

            string createCategories = @"
                CREATE TABLE IF NOT EXISTS Categories (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE
                );";

            using var cmdCreateCats = new SqliteCommand(createCategories, connection);
            cmdCreateCats.ExecuteNonQuery();

            string createProducts = @"
                CREATE TABLE IF NOT EXISTS Products (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CategoryId INTEGER NOT NULL,
                    Name TEXT NOT NULL,
                    Price REAL NOT NULL,
                    Sold INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE RESTRICT
                );";

            using var cmdCreateProducts = new SqliteCommand(createProducts, connection);
            cmdCreateProducts.ExecuteNonQuery();

            string[] categories = { "Tobak", "Godis", "Enkel mat", "Tidningar" };

            using (var tran = connection.BeginTransaction())
            {
                foreach (var cat in categories)
                {
                    using var cmd = new SqliteCommand("INSERT OR IGNORE INTO Categories (Name) VALUES (@name);", connection, tran);
                    cmd.Parameters.AddWithValue("@name", cat);
                    cmd.ExecuteNonQuery();
                }
                tran.Commit();
            }

            Console.WriteLine("Categories ensured.");

            int count;
            using (var cmdCount = new SqliteCommand("SELECT COUNT(*) FROM Products;", connection))
                count = Convert.ToInt32(cmdCount.ExecuteScalar());

            if (count > 0)
            {
                Console.WriteLine($"Products already exist ({count} rows). No inserts done.");
            }
            else
            {
                Console.WriteLine("Table is empty. Inserting products...");

                var products = new List<(string Category, string Name, double Price)>
                {
                    ("Tobak", "Marlboo Red (20-pack)", 89),
                    ("Tobak", "Camel Blue (20-pack)", 85),
                    ("Tobak", "L&M Filter (20-pack)", 79),
                    ("Tobak", "Skruf Original Portion", 62),
                    ("Tobak", "Göteborgs Rapé White Portion", 67),

                    ("Godis", "Marabou Mjölkchoklad 100 g", 25),
                    ("Godis", "Daim dubbel", 15),
                    ("Godis", "Kexchoklad", 12),
                    ("Godis", "Malaco Gott & Blandat 160 g", 28),

                    ("Enkel mat", "Korv med bröd", 25),
                    ("Enkel mat", "Varm toast (ost & skinka)", 30),
                    ("Enkel mat", "Pirog (Köttfärs)", 22),
                    ("Enkel mat", "Färdig sallad (kyckling)", 49),
                    ("Enkel mat", "Panini (mozzarella & pesto)", 45),

                    ("Tidningar", "Aftonbladet (dagens)", 28),
                    ("Tidningar", "Expressen (Dagens)", 28),
                    ("Tidningar", "Illustrerad Vetenskap", 79),
                    ("Tidningar", "Kalle Anka & Co", 45),
                    ("Tidningar", "Allt om Mat", 69)
                };

                using var tran2 = connection.BeginTransaction();
                using var insertCmd = new SqliteCommand(@"
                    INSERT INTO Products (CategoryId, Name, Price, Sold)
                    VALUES ((SELECT Id FROM Categories WHERE Name = @cat), @name, @price, 0);",
                    connection, tran2);

                var pCat = insertCmd.Parameters.Add("@cat", SqliteType.Text);
                var pName = insertCmd.Parameters.Add("@name", SqliteType.Text);
                var pPrice = insertCmd.Parameters.Add("@price", SqliteType.Real);

                foreach (var p in products)
                {
                    pCat.Value = p.Category;
                    pName.Value = p.Name;
                    pPrice.Value = p.Price;

                    insertCmd.ExecuteNonQuery();
                }

                tran2.Commit();
                Console.WriteLine("Products inserted successfully!");
            }

            Console.WriteLine("\nProducts in database:");
            using var cmdSelect = new SqliteCommand(@"
                SELECT p.Id, c.Name AS Category, p.Name, p.Price, p.Sold
                FROM Products p
                JOIN Categories c ON p.CategoryId = c.Id
                ORDER BY p.Id;", connection);

            using var reader = cmdSelect.ExecuteReader();
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                string category = reader.GetString(1);
                string name = reader.GetString(2);
                double price = reader.GetDouble(3);
                int sold = reader.GetInt32(4);

                Console.WriteLine($"{id}: [{category}] {name} — price: {price} kr — sold: {sold}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}
