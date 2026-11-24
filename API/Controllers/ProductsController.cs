
using DAL;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace API.Controllers
{
    // Controller שאחראי על ניהול המוצרים במערכת.
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        // אובייקט שמאפשר לבצע פעולות על המוצרים במסד הנתונים.
        private readonly ProductDB productDB;

        // Constructor שמקבל את ProductDB ומאתחל אותו.
        public ProductsController(ProductDB productDB)
        {
            this.productDB = productDB;
        }

        // שליפת כל המוצרים ממסד הנתונים.
        // GET: api/products
        [HttpGet]
        public async Task<ActionResult<List<Product>>> GetAllProducts()
        {
            try
            {
                List<Product> products = await productDB.GetAllProductsAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                // במקרה של שגיאה, החזרת קוד 500 והודעת שגיאה.
                return StatusCode(500, new
                {
                    message = "Error retrieving products",
                    error = ex.GetBaseException().Message
                });
            }
        }

        // הוספת מוצר חדש למערכת.
        // GET: api/products
        [HttpPost]
        public async Task<ActionResult<Product>> AddProduct(Product product)
        {
            try
            {
                // בדיקה ששם המוצר אינו ריק.
                if (string.IsNullOrWhiteSpace(product.ProductName))
                {
                    return BadRequest("Product name is required.");
                }

                // בדיקה שמחיר המוצר גדול מאפס.
                if (product.Price <= 0)
                {
                    return BadRequest("Product price must be greater than zero.");
                }

                // בדיקה שהקטגוריה שהתקבלה קיימת ומותרת במערכת.
                if (product.CategoryID != 1 && product.CategoryID != 2)
                {
                    return BadRequest("Invalid product category.");
                }

                // הוספת המוצר למסד הנתונים וקבלת המזהה שנוצר עבורו.
                product.ProductID = await productDB.AddProductAsync(product);

                return Ok(product);
            }
            catch (Exception ex)
            {
                // במקרה של שגיאה, החזרת קוד 500 והודעת שגיאה.
                return StatusCode(500, new
                {
                    message = "Error adding product",
                    error = ex.GetBaseException().Message
                });
            }
        }

        // עדכון מוצר קיים לפי המזהה שלו.
        // PUT: api/products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, Product product)
        {
            try
            {
                // בדיקה שמזהה המוצר תקין וחיובי.
                if (id <= 0)
                {
                    return BadRequest("Invalid product ID.");
                }

                // בדיקה ששם המוצר אינו ריק.
                if (string.IsNullOrWhiteSpace(product.ProductName))
                {
                    return BadRequest("Product name is required.");
                }

                // בדיקה שמחיר המוצר גדול מאפס.
                if (product.Price <= 0)
                {
                    return BadRequest("Product price must be greater than zero.");
                }

                // בדיקה שהקטגוריה שהתקבלה תקינה.
                if (product.CategoryID != 1 && product.CategoryID != 2)
                {
                    return BadRequest("Invalid product category.");
                }

                // הצבת המזהה שהתקבל בכתובת בתוך אובייקט המוצר.
                product.ProductID = id;

                // ניסיון לעדכן את המוצר במסד הנתונים.
                bool updated = await productDB.UpdateProductAsync(product);

                // אם המוצר לא נמצא, החזרת קוד 404.
                if (!updated)
                {
                    return NotFound("Product not found.");
                }

                return Ok(product);
            }
            catch (Exception ex)
            {
                // במקרה של שגיאה, החזרת קוד 500 והודעת שגיאה.
                return StatusCode(500, new
                {
                    message = "Error updating product",
                    error = ex.GetBaseException().Message
                });
            }
        }

        // מחיקת מוצר ממסד הנתונים לפי המזהה שלו.
        // DELETE: api/products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                // ניסיון למחוק את המוצר ממסד הנתונים.
                bool deleted = await productDB.DeleteProductAsync(id);

                // אם המוצר לא נמצא או לא נמחק, החזרת קוד 404.
                if (!deleted)
                {
                    return NotFound("Product not found.");
                }

                // החזרת הודעה המאשרת שהמוצר נמחק בהצלחה.
                return Ok(new
                {
                    message = "Product deleted successfully."
                });
            }
            catch (Exception ex)
            {
                // במקרה של שגיאה, החזרת קוד 500 והודעת שגיאה.
                return StatusCode(500, new
                {
                    message = "Error deleting product",
                    error = ex.GetBaseException().Message
                });
            }
        }
    }
}