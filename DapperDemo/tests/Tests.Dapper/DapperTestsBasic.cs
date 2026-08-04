using System.ComponentModel.DataAnnotations.Schema;
using Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Tests.Dapper;

public class DapperTestsBasic
{
    public DapperTestsBasic()
    {
        SQLitePCL.Batteries.Init();
    }

    [Table("Products")] // Optional, can be deleted and Dapper assumes is a table
    private class Product
    {
        public int Id { get; set; }
        public required string ProductName { get; set; }
        public required decimal Price { get; set; }
    }

    [Fact]
    public void Test1()
    {
        try
        {
            var connectionString = new SqliteConnectionStringBuilder()
            {
                Mode = SqliteOpenMode.Memory,
            }.ToString();

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                connection.Execute(
                    sql: """
                            CREATE TABLE Products (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                ProductName VARCHAR(255) NOT NULL,
                                Price DECIMAL(10, 2) NOT NULL
                            );
                         """);

                connection.Execute("INSERT INTO Products (ProductName, Price) VALUES (@ProductName, @Price)",
                    new { ProductName = "Test Product 1", Price = 10.99 });
                connection.Execute("INSERT INTO Products (ProductName, Price) VALUES (@ProductName, @Price)",
                    new { ProductName = "Test Product 2", Price = (decimal)20.50 });

                var products = connection.Query<Product>("SELECT * FROM Products").ToList();

                Assert.Equal(2, products.Count);
                Assert.Contains(products, p => p is { ProductName: "Test Product 1", Price: 10.99m });
                Assert.Contains(products, p => p is { ProductName: "Test Product 2", Price: 20.50m });
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}