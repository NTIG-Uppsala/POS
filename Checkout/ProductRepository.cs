using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace Checkout
{
    public static class ProductRepository
    {
        private static string _dbPath = Database.DbPath;

        public static string CurrentDatabasePath => _dbPath;

        public static void SetDatabasePath(string path)
        {
            _dbPath = path;
        }

        private static string DbPath => _dbPath;

        public static List<string> GetAllCategories()
        {
            var categories = new List<string>();

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            using var cmd = new SqliteCommand("SELECT Name FROM Categories", connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
                categories.Add(reader.GetString(0));

            return categories;
        }

        public static List<Product> GetProductsByCategory(string category)
        {
            var list = new List<Product>();

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var cmd = new SqliteCommand(@"
                SELECT p.Id, p.Name, p.Price, c.Name
                FROM Products p
                JOIN Categories c ON p.CategoryId = c.Id
                WHERE c.Name = @cat", connection);

            cmd.Parameters.AddWithValue("@cat", category);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Product
                {
                    Id = r.GetInt32(0),
                    Name = r.GetString(1),
                    Price = (decimal)r.GetDouble(2),
                    Category = r.GetString(3)
                });
            }

            return list;
        }

        public static List<InventoryItem> GetInventory()
        {
            var list = new List<InventoryItem>();

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            string sql = @"
                SELECT 
                    p.Id,
                    c.Name AS Category,
                    p.Name,
                    p.Inventory,
                    p.Sold
                FROM Products p
                JOIN Categories c ON p.CategoryId = c.Id
                ORDER BY c.Name, p.Name;
            ";

            using var cmd = new SqliteCommand(sql, connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new InventoryItem
                {
                    Id = reader.GetInt32(0),
                    Category = reader.GetString(1),
                    Name = reader.GetString(2),
                    Inventory = reader.GetInt32(3),
                    Sold = reader.GetInt32(4)
                });
            }

            return list;
        }

        public static bool TrySellProduct(int productId, int quantity)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            using var transaction = connection.BeginTransaction();

            // Kolla lager
            var checkCmd = new SqliteCommand(
                "SELECT Inventory FROM Products WHERE Id = @id",
                connection, transaction);

            checkCmd.Parameters.AddWithValue("@id", productId);
            var inventory = Convert.ToInt32(checkCmd.ExecuteScalar());

            if (inventory < quantity)
            {
                transaction.Rollback();
                return false;
            }

            // Uppdatera lager & sold
            var updateCmd = new SqliteCommand(@"
        UPDATE Products
        SET 
            Inventory = Inventory - @q,
            Sold = Sold + @q
        WHERE Id = @id",
                connection, transaction);

            updateCmd.Parameters.AddWithValue("@q", quantity);
            updateCmd.Parameters.AddWithValue("@id", productId);
            updateCmd.ExecuteNonQuery();

            transaction.Commit();
            return true;
        }
    }
}
