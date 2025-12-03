// See https://aka.ms/new-console-template for more information
using System;
using System.IO;
using Microsoft.Data.Sqlite;

try
{
    // Skapa mapp och databasfil
    string folderPath = @"..\..\..\";
    Directory.CreateDirectory(folderPath);

    string dbPath = Path.Combine(folderPath, "Database.db");

    using var connection = new SqliteConnection($"Data Source={dbPath}");
    connection.Open();

    Console.WriteLine("Connected to the SQLite database");

    // Skapa tabell med kolumn för sålda produkter
    string createTableSql = @"
    CREATE TABLE IF NOT EXISTS Products (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        Category TEXT NOT NULL,
        Name TEXT NOT NULL,
        Price REAL NOT NULL,
        Quantity INTEGER NOT NULL,
        Sold INTEGER NOT NULL DEFAULT 0
    );";

    using var cmdCreate = new SqliteCommand(createTableSql, connection);
    cmdCreate.ExecuteNonQuery();

    Console.WriteLine("Table 'Products' checked/created");

    // Kolla om tabellen är tom
    int count = 0;
    using var cmdCount = new SqliteCommand("SELECT COUNT(*) FROM Products;", connection);
    count = Convert.ToInt32(cmdCount.ExecuteScalar());

    if (count > 0)
    {
        Console.WriteLine($"Products already exist ({count} rows). No inserts performed.");
    }
    else
    {
        Console.WriteLine("Table is empty. Inserting products with Sold = 0...");

        // Lägger in produkter med Sold = 0
        string insertSql = @"
        INSERT INTO Products (Category, Name, Price, Quantity, Sold) VALUES ('Tobak', 'Marlboo Red (20-pack)', 89, 100, 0);
        INSERT INTO Products (Category, Name, Price, Quantity, Sold) VALUES ('Tobak', 'Camel Blue (20-pack)', 85, 100, 0);
        INSERT INTO Products (Category, Name, Price, Quantity, Sold) VALUES ('Tobak', 'L&M Filter (20-pack)', 79, 100, 0);
        INSERT INTO Products (Category, Name, Price, Quantity, Sold) VALUES ('Tobak', 'Skruf Original Portion', 62, 100, 0);
        INSERT INTO Products (Category, Name, Price, Quantity, Sold) VALUES ('Tobak', 'Göteborgs Rapé White Portion', 67, 100, 0);
        
        INSERT INTO Products (Category, Name, Price, Quantity, Sold) VALUES ('Godis', 'Marabou Mjölkchoklad 100 g', 25, 100, 0);
        INSERT INTO Products (Category, Name, Price, Quantity, Sold) VALUES ('Godis', 'Daim dubbel', 15, 100, 0);
        INSERT INTO Products (Category, Name, Price, Quantity, Sold) VALUES ('Godis', 'Kexchoklad', 12, 100, 0);
        INSERT INTO Products (Category, Name, Price, Quantity, Sold) VALUES ('Godis', 'Malaco Gott & Blandat 160 g', 28, 100, 0);

        INSERT INTO Products (Category, Name, Price, Quantity, Sold) VALUES ('Enkel mat', 'Korv med bröd', 25, 100, 0);
        INSERT INTO Products (Category, Name, Price, Quantity, Sold) VALUES ('Enkel mat', 'Varm toast (ost & skinka)', 30, 100, 0);
        INSERT INTO Products (Category, Name, Price, Quantity, Sold) VALUES ('Enkel mat', 'Pirog (Köttfärs)', 22, 100, 0);
        INSERT INTO Products (Category, Name, Price, Quantity, Sold) VALUES ('Enkel mat', 'Färdig sallad (kyckling)', 49, 100, 0);
        INSERT INTO Products (Category, Name, Price, Quantity, Sold) VALUES ('Enkel mat', 'Panini (mozzarella & pesto)', 45, 100, 0); 

        INSERT INTO Products (Category, Name, Price, Quantity, Sold) VALUES ('Tidningar', 'Aftonbladet (dagens)', 28, 100, 0);
        INSERT INTO Products (Category, Name, Price, Quantity, Sold) VALUES ('Tidningar', 'Expressen (Dagens)', 28, 100, 0);
        INSERT INTO Products (Category, Name, Price, Quantity, Sold) VALUES ('Tidningar', 'Illustrerad Vetenskap', 79, 100, 0);
        INSERT INTO Products (Category, Name, Price, Quantity, Sold) VALUES ('Tidningar', 'Kalle Anka & Co', 45, 100, 0);
        INSERT INTO Products (Category, Name, Price, Quantity, Sold) VALUES ('Tidningar', 'Allt om Mat', 69, 100, 0);
        ";

        using var cmdInsert = new SqliteCommand(insertSql, connection);
        cmdInsert.ExecuteNonQuery();

        Console.WriteLine("Products inserted successfully with Sold = 0!");
    }

    // Visa hur många produkter som har sålts
    Console.WriteLine("\nAntal produkter sålda:");
    using var cmdSelect = new SqliteCommand("SELECT Name, Sold FROM Products;", connection);
    using (var reader = cmdSelect.ExecuteReader())
    {
        while (reader.Read())
        {
            string name = reader.GetString(0);
            int sold = reader.GetInt32(1);
            Console.WriteLine($"{name} sold: {sold}");
        }
    }
}
catch (SqliteException ex)
{
    Console.WriteLine("SQLite Error: " + ex.Message);
}
