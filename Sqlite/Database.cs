// See https://aka.ms/new-console-template for more information
using System;
using Microsoft.Data.Sqlite;

try
{

    string folderPath = @"..\..\..\";
    Directory.CreateDirectory(folderPath);

    string dbPath = Path.Combine(folderPath, "Database.db");

    using var connection = new SqliteConnection($"Data Source={dbPath}");
    connection.Open();

    Console.WriteLine("Connected to the SQLite database");

    // Skappar tabeller //

    string createTableSql = @"
    CREATE TABLE IF NOT EXISTS Products (
    Id  INTEGER PRIMARY KEY AUTOINCREMENT,
    Category TEXT NOT NULL,
    Name TEXT NOT NULL,
    Price REAL NOT NULL,
    Quantity INTEGER NOT NULL
    );";

    using (var cmd = new SqliteCommand(createTableSql, connection))
    {
        cmd.ExecuteNonQuery();
    }

    Console.WriteLine("Table 'Products' checked/created");

    // Checkar ifall tabellerna är tomma //

    int count = 0;
    using(var cmd = new SqliteCommand("SELECT COUNT (*) FROM Products;", connection))
    {
        count = Convert.ToInt32(cmd.ExecuteScalar());
    }

    if (count > 0)
    {
        Console.WriteLine($"Products already exist ({count} rows). No inserts performed.");
        return;
    }

    Console.WriteLine("Table is empty. Inserting products with quantity 100...");

    //Lägger in all Produkter//

    string insertsql = @"
    INSERT INTO Products (Category, Name, Price, Quantity) VALUES ('Tobak', 'Marlboo Red (20-pack)', 89, 100);
    INSERT INTO Products (Category, Name, Price, Quantity) VALUES ('Tobak', 'Camel Blue (20-pack)', 85, 100);
    INSERT INTO Products (Category, Name, Price, Quantity) VALUES ('Tobak', 'L&M Filter (20-pack)', 79, 100);
    INSERT INTO Products (Category, Name, Price, Quantity) VALUES ('Tobak', 'Skruf Original Portion', 62, 100);
    INSERT INTO Products (Category, Name, Price, Quantity) VALUES ('Tobak', 'Göteborgs Rapé White Portion', 67, 100);
    
    INSERT INTO Products (Category, Name, Price, Quantity) VALUES ('Godis', 'Marabou Mjölkchoklad 100 g', 25, 100);
    INSERT INTO Products (Category, Name, Price, Quantity) VALUES ('Godis', 'Daim dubbel', 15, 100);
    INSERT INTO Products (Category, Name, Price, Quantity) VALUES ('Godis', 'Kexchoklad', 12, 100);
    INSERT INTO Products (Category, Name, Price, Quantity) VALUES ('Godis', 'Malaco Gott & Blandat 160 g', 28, 100);

    INSERT INTO Products (Category, Name, Price, Quantity) VALUES ('Enkel mat', 'Korv med bröd', 25, 100);
    INSERT INTO Products (Category, Name, Price, Quantity) VALUES ('Enkel mat', 'Varm toast (ost & skinka', 30, 100);
    INSERT INTO Products (Category, Name, Price, Quantity) VALUES ('Enkel mat', 'Pirog (Köttfärs)', 22, 100);
    INSERT INTO Products (Category, Name, Price, Quantity) VALUES ('Enkel mat', 'Färdig sallad (kyckling)', 49, 100);
    INSERT INTO Products (Category, Name, Price, Quantity) VALUES ('Enkel mat', 'Panini (mozzarella & pesto)', 45, 100); 

    INSERT INTO Products (Category, Name, Price, Quantity) VALUES ('Tidningar', 'Aftonbladet (dagens)', 28, 100);
    INSERT INTO Products (Category, Name, Price, Quantity) VALUES ('Tidningar', 'Expressen (Dagens)', 28, 100);
    INSERT INTO Products (Category, Name, Price, Quantity) VALUES ('Tidningar', 'Illustrerad Vetenskap', 79, 100);
    INSERT INTO Products (Category, Name, Price, Quantity) VALUES ('Tidningar', 'Kalle Anka & Co', 45, 100);
    INSERT INTO Products (Category, Name, Price, Quantity) VALUES ('Tidningar', 'Allt om Mat', 69, 100);
    ";

    using (var cmd = new SqliteCommand(insertsql, connection))
    {
        cmd.ExecuteNonQuery();

        Console.WriteLine("Products inserted successfully with quantity 100!");
    }

    Console.WriteLine("All products inserted");

} catch (SqliteException ex) {

    Console.WriteLine("SqLite Error:" + ex.Message);
}




