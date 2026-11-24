using MySql.Data.MySqlClient;
using Model;

namespace DAL
{
    // מחלקה שאחראית על ביצוע פעולות מול טבלת המוצרים במסד הנתונים.
    public class ProductDB
    {
        // שמירת מחרוזת החיבור למסד הנתונים.
        private readonly string connectionString;

        // הדפסת הודעות לוג למסוף.
        private static void Log(string message)
        {
            Console.WriteLine($"[PRODUCT DB] {DateTime.Now:HH:mm:ss} - {message}");
        }

        // Constructor שמקבל את מחרוזת החיבור ושומר אותה.
        public ProductDB(string connectionString)
        {
            this.connectionString = connectionString;
        }

        // שליפת כל המוצרים ממסד הנתונים.
        public async Task<List<Product>> GetAllProductsAsync()
        {
            Log("Retrieving all products...");

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

            Log($"Products retrieved successfully. Total products: {products.Count}");

            return products;
        }

        // הוספת מוצר חדש למסד הנתונים.
        public async Task<int> AddProductAsync(Product product)
        {
            Log($"Starting product creation. Product name: {product.ProductName}");

            string sql = @"
                INSERT INTO Products
                (ProductName, Description, Price, ProductImageBlob, ImageUrl, CategoryID)
                VALUES
                (@ProductName, @Description, @Price, @ProductImageBlob, @ImageUrl, @CategoryID);";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            Log("Connected to database. Inserting new product...");

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

            int productId = Convert.ToInt32(command.LastInsertedId);

            Log($"Product created successfully. Product ID: {productId}, Name: {product.ProductName}");

            return productId;
        }

        // עדכון פרטים של מוצר קיים לפי המזהה שלו.
        public async Task<bool> UpdateProductAsync(Product product)
        {
            Log($"Starting product update. Product ID: {product.ProductID}");

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
                Log($"Product updated successfully. Product ID: {product.ProductID}, Affected rows: {affectedRows}");
                return true;
            }

            Log($"No rows changed during update. Checking if product exists. Product ID: {product.ProductID}");

            string checkSql = "SELECT COUNT(*) FROM Products WHERE ProductID = @ProductID;";

            using var checkCommand = new MySqlCommand(checkSql, connection);
            checkCommand.Parameters.AddWithValue("@ProductID", product.ProductID);

            int count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

            bool exists = count > 0;

            if (exists)
            {
                Log($"Product exists, but no values changed. Product ID: {product.ProductID}");
            }
            else
            {
                Log($"Product update failed. Product not found. Product ID: {product.ProductID}");
            }

            return exists;
        }

        // מחיקת מוצר ממסד הנתונים לפי המזהה שלו.
        public async Task<bool> DeleteProductAsync(int productID)
        {
            Log($"Starting product deletion. Product ID: {productID}");

            string sql = "DELETE FROM Products WHERE ProductID = @ProductID;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ProductID", productID);

            int affectedRows = await command.ExecuteNonQueryAsync();

            bool success = affectedRows > 0;

            if (success)
            {
                Log($"Product deleted successfully. Product ID: {productID}");
            }
            else
            {
                Log($"Product deletion failed. Product not found. Product ID: {productID}");
            }

            return success;
        }
    }
}
