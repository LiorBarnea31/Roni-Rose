
using DAL;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ProductDB productDB;

        public ProductsController(ProductDB productDB)
        {
            this.productDB = productDB;
        }

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
                return StatusCode(500, new
                {
                    message = "Error retrieving products",
                    error = ex.GetBaseException().Message
                });
            }
        }

        // POST: api/products
        [HttpPost]
        public async Task<ActionResult<Product>> AddProduct(Product product)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(product.ProductName))
                {
                    return BadRequest("Product name is required.");
                }

                if (product.Price <= 0)
                {
                    return BadRequest("Product price must be greater than zero.");
                }

                if (product.CategoryID != 1 && product.CategoryID != 2)
                {
                    return BadRequest("Invalid product category.");
                }

                product.ProductID = await productDB.AddProductAsync(product);

                return Ok(product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error adding product",
                    error = ex.GetBaseException().Message
                });
            }
        }

        // PUT: api/products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, Product product)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Invalid product ID.");
                }

                if (string.IsNullOrWhiteSpace(product.ProductName))
                {
                    return BadRequest("Product name is required.");
                }

                if (product.Price <= 0)
                {
                    return BadRequest("Product price must be greater than zero.");
                }

                if (product.CategoryID != 1 && product.CategoryID != 2)
                {
                    return BadRequest("Invalid product category.");
                }

                product.ProductID = id;

                bool updated = await productDB.UpdateProductAsync(product);

                if (!updated)
                {
                    return NotFound("Product not found.");
                }

                return Ok(product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error updating product",
                    error = ex.GetBaseException().Message
                });
            }
        }

        // DELETE: api/products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                bool deleted = await productDB.DeleteProductAsync(id);

                if (!deleted)
                {
                    return NotFound("Product not found.");
                }

                return Ok(new
                {
                    message = "Product deleted successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error deleting product",
                    error = ex.GetBaseException().Message
                });
            }
        }
    }
}
