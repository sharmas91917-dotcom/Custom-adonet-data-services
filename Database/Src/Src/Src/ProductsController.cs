using System;
using System.Net;
using System.Web.Http;

namespace CustomDataServiceDemo
{
    [RoutePrefix("api/products")]
    public class ProductsController : ApiController
    {
        private readonly ICachedProductRepository _repository;

        public ProductsController()
            : this(new CachedAdoProductRepository())
        {
        }

        public ProductsController(ICachedAdoProductRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAll()
        {
            var products = _repository.GetAllProducts();
            return Ok(products);
        }

        [HttpGet]
        [Route("{id:int}", Name = "GetProductById")]
        public IHttpActionResult GetById(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Product ID must be greater than zero.");
            }

            var product = _repository.GetById(id);
            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        [HttpPost]
        [Route("")]
        public IHttpActionResult Create([FromBody] Product product)
        {
            if (product == null || string.IsNullOrWhiteSpace(product.Name) || product.Price < 0)
            {
                return BadRequest("Valid product Name and non-negative Price are required.");
            }

            var created = _repository.Create(product);
            return CreatedAtRoute("GetProductById", new { id = created.Id }, created);
        }

        [HttpPut]
        [Route("{id:int}")]
        public IHttpActionResult Update(int id, [FromBody] Product product)
        {
            if (product == null || id != product.Id)
            {
                return BadRequest("Route ID and payload ID must match.");
            }

            if (string.IsNullOrWhiteSpace(product.Name) || product.Price < 0)
            {
                return BadRequest("Valid product Name and non-negative Price are required.");
            }

            bool updated = _repository.Update(product);
            if (!updated)
            {
                return NotFound();
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpDelete]
        [Route("{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            if (id <= 0)
            {
                return BadRequest("Product ID must be greater than zero.");
            }

            bool deleted = _repository.Delete(id);
            if (!deleted)
            {
                return NotFound();
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpPost]
        [Route("cache/refresh")]
        public IHttpActionResult RefreshCache()
        {
            _repository.InvalidateCache();
            var refreshed = _repository.GetAllProducts();
            return Ok(new { Message = "Cache refreshed.", Count = refreshed.Count, RefreshedAtUtc = DateTime.UtcNow });
        }
    }
}
