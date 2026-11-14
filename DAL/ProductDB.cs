
using MySql.Data.MySqlClient;
using Model;

namespace DAL
{
    public class ProductDB
    {
        private readonly string connectionString;

        public ProductDB(string connectionString)
        {
            this.connectionString = connectionString;
        }

        // Get all products
        public async Task<List<Product>> GetAllProductsAsync()
        {
            List<Product> products = new List<Product>();

            string sql = @"
                SELECT ProductID, ProductName, Description,
                       Price, ProductImageBlob, ImageUrl, CategoryID
                FROM Products
                ORDER BY ProductID DESC;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                Product product = new Product();

                product.ProductID = Convert.ToInt32(reader["ProductID"]);
                product.ProductName = reader["ProductName"].ToString() ?? "";

                product.Description = reader["Description"] == DBNull.Value
                    ? null
                    : reader["Description"].ToString();

                product.Price = Convert.ToDecimal(reader["Price"]);

                product.ProductImageBlob = reader["ProductImageBlob"] == DBNull.Value
                    ? null
                    : (byte[])reader["ProductImageBlob"];

                product.ImageUrl = reader["ImageUrl"] == DBNull.Value
                    ? null
                    : reader["ImageUrl"].ToString();

                product.CategoryID = Convert.ToInt32(reader["CategoryID"]);

                products.Add(product);
            }

            return products;
        }

        // Add product
        public async Task<int> AddProductAsync(Product product)
        {
            string sql = @"
                INSERT INTO Products
                (ProductName, Description, Price, ProductImageBlob, ImageUrl, CategoryID)
                VALUES
                (@ProductName, @Description, @Price, @ProductImageBlob, @ImageUrl, @CategoryID);";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@ProductName", product.ProductName);
            command.Parameters.AddWithValue("@Description",
                (object?)product.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@Price", product.Price);
            command.Parameters.AddWithValue("@ProductImageBlob",
                (object?)product.ProductImageBlob ?? DBNull.Value);
            command.Parameters.AddWithValue("@ImageUrl",
                (object?)product.ImageUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@CategoryID", product.CategoryID);

            await command.ExecuteNonQueryAsync();

            return Convert.ToInt32(command.LastInsertedId);
        }

        // Update product
        public async Task<bool> UpdateProductAsync(Product product)
        {
            string sql = @"
                UPDATE Products
                SET ProductName = @ProductName,
                    Description = @Description,
                    Price = @Price,
                    ProductImageBlob = @ProductImageBlob,
                    ImageUrl = @ImageUrl,
                    CategoryID = @CategoryID
                WHERE ProductID = @ProductID;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            command.Parameters.AddWithValue("@ProductID", product.ProductID);
            command.Parameters.AddWithValue("@ProductName", product.ProductName);
            command.Parameters.AddWithValue("@Description",
                (object?)product.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@Price", product.Price);
            command.Parameters.AddWithValue("@ProductImageBlob",
                (object?)product.ProductImageBlob ?? DBNull.Value);
            command.Parameters.AddWithValue("@ImageUrl",
                (object?)product.ImageUrl ?? DBNull.Value);
            command.Parameters.AddWithValue("@CategoryID", product.CategoryID);

            int affectedRows = await command.ExecuteNonQueryAsync();

            if (affectedRows > 0)
            {
                return true;
            }

            // An unchanged product can also return 0 affected rows.
            string checkSql = "SELECT COUNT(*) FROM Products WHERE ProductID = @ProductID;";

            using var checkCommand = new MySqlCommand(checkSql, connection);
            checkCommand.Parameters.AddWithValue("@ProductID", product.ProductID);

            int count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

            return count > 0;
        }

        // Delete product
        public async Task<bool> DeleteProductAsync(int productID)
        {
            string sql = "DELETE FROM Products WHERE ProductID = @ProductID;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ProductID", productID);

            int affectedRows = await command.ExecuteNonQueryAsync();

            return affectedRows > 0;
        }
    }
}
