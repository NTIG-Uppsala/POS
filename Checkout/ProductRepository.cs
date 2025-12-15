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
        private static string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Sqlite\Database.db");

        public static void SetDatabasePath(string path)
        {
            dbPath = path;
        }
    
    // Använd dbPath som innan i GetProductsByCategory och GetAllCategories


        public static List<Product> GetProductsByCategory(string categoryName)
        {
            var products = new List<Product>();

            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            string sql = @"
                SELECT p.Id, p.Name, p.Price, c.Name AS Category
                FROM Products p
                JOIN Categories c ON p.CategoryId = c.Id
                WHERE c.Name = @category
                ORDER BY p.Id;";

            using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@category", categoryName);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                products.Add(new Product
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Price = (decimal)reader.GetDouble(2),
                    Category = reader.GetString(3)
                });
            }

            return products;
        }

        public static List<string> GetAllCategories()
        {
            var categories = new List<string>();

            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            string sql = "SELECT Name FROM Categories ORDER BY Id;";
            using var cmd = new SqliteCommand(sql, connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                categories.Add(reader.GetString(0));
            }

            return categories;
        }
    }
}