using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Caching;

namespace CustomDataServiceDemo
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }

    public interface ICachedProductRepository
    {
        List<Product> GetAllProducts();
        Product GetById(int id);
        Product Create(Product product);
        bool Update(Product product);
        bool Delete(int id);
        void InvalidateCache();
    }

    public class CachedAdoProductRepository : ICachedProductRepository
    {
        private const string AllProductsCacheKey = "AdoProducts_All";
        private static readonly object CacheLock = new object();
        private readonly string _connectionString;
        private readonly ObjectCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

        public CachedAdoProductRepository()
            : this(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString, MemoryCache.Default)
        {
        }

        public CachedAdoProductRepository(string connectionString, ObjectCache cache)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public List<Product> GetAllProducts()
        {
            if (_cache.Get(AllProductsCacheKey) is List<Product> cached)
            {
                return cached;
            }

            lock (CacheLock)
            {
                if (_cache.Get(AllProductsCacheKey) is List<Product> doubleChecked)
                {
                    return doubleChecked;
                }

                var products = LoadProductsFromDatabase();
                var policy = new CacheItemPolicy
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow.Add(_cacheDuration)
                };

                _cache.Set(AllProductsCacheKey, products, policy);
                return products;
            }
        }

        public Product GetById(int id)
        {
            return GetAllProducts().FirstOrDefault(p => p.Id == id);
        }

        public Product Create(Product product)
        {
            const string sql = @"
                INSERT INTO dbo.Products (Name, Category, Price, Stock)
                OUTPUT INSERTED.Id
                VALUES (@Name, @Category, @Price, @Stock);";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 150).Value = product.Name;
                cmd.Parameters.Add("@Category", SqlDbType.NVarChar, 100).Value = product.Category;
                cmd.Parameters.Add("@Price", SqlDbType.Decimal).Value = product.Price;
                cmd.Parameters.Add("@Stock", SqlDbType.Int).Value = product.Stock;

                conn.Open();
                product.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }

            InvalidateCache();
            return product;
        }

        public bool Update(Product product)
        {
            const string sql = @"
                UPDATE dbo.Products
                SET Name = @Name,
                    Category = @Category,
                    Price = @Price,
                    Stock = @Stock
                WHERE Id = @Id;";

            int rowsAffected;
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = product.Id;
                cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 150).Value = product.Name;
                cmd.Parameters.Add("@Category", SqlDbType.NVarChar, 100).Value = product.Category;
                cmd.Parameters.Add("@Price", SqlDbType.Decimal).Value = product.Price;
                cmd.Parameters.Add("@Stock", SqlDbType.Int).Value = product.Stock;

                conn.Open();
                rowsAffected = cmd.ExecuteNonQuery();
            }

            if (rowsAffected > 0)
            {
                InvalidateCache();
                return true;
            }

            return false;
        }

        public bool Delete(int id)
        {
            const string sql = "DELETE FROM dbo.Products WHERE Id = @Id;";

            int rowsAffected;
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                conn.Open();
                rowsAffected = cmd.ExecuteNonQuery();
            }

            if (rowsAffected > 0)
            {
                InvalidateCache();
                return true;
            }

            return false;
        }

        public void InvalidateCache()
        {
            _cache.Remove(AllProductsCacheKey);
        }

        private List<Product> LoadProductsFromDatabase()
        {
            const string sql = "SELECT Id, Name, Category, Price, Stock FROM dbo.Products ORDER BY Id;";
            var result = new List<Product>();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    int idOrd = reader.GetOrdinal("Id");
                    int nameOrd = reader.GetOrdinal("Name");
                    int catOrd = reader.GetOrdinal("Category");
                    int priceOrd = reader.GetOrdinal("Price");
                    int stockOrd = reader.GetOrdinal("Stock");

                    while (reader.Read())
                    {
                        result.Add(new Product
                        {
                            Id = reader.GetInt32(idOrd),
                            Name = reader.GetString(nameOrd),
                            Category = reader.GetString(catOrd),
                            Price = reader.GetDecimal(priceOrd),
                            Stock = reader.GetInt32(stockOrd)
                        });
                    }
                }
            }

            return result;
        }
    }
}
