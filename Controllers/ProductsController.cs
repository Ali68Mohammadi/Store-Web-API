using BestStoreApi.Models;
using BestStoreApi.Models.Dto;
using BestStoreApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BestStoreApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private static readonly List<string> categorylist = new()
        {
            "Phones","Computers","Accessories","Printers","Cameras","Other",

        };


        public ProductsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;

        }

        [HttpGet("categories")]
        public IActionResult GetCategories()
        {
            return Ok(categorylist);
        }

        [HttpGet]
        public IActionResult GetProducts(string? search, string? category,
            int? minPrice, int? maxPrice,
            string? sortBy, string? orderBy, int? page)
        {
            #region seraching
            //search functionality
            IQueryable<Product> query = _context.Products;

            if (search != null)
            {
                query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
            }

            if (category != null)
            {
                query = query.Where(p => p.Category == category);
            }

            if (minPrice != null)
            {
                query = query.Where(p => p.Price >= minPrice);
            }

            if (maxPrice != null)
            {
                query = query.Where(p => p.Price <= maxPrice);
            }

            #endregion 

            #region sort functionality
            //sort functionality
            // if (sort == null) sort = "id";
            // =
            sortBy ??= "id";
            if (orderBy == null || orderBy != "asc") orderBy = "desc";

            if (sortBy.ToLower() == "name")
            {
                if (orderBy == "asc")
                {
                    query = query.OrderBy(p => p.Name);
                }
                query = query.OrderByDescending(p => p.Name);
            }

            else if (sortBy.ToLower() == "brand")
            {
                if (orderBy == "asc")
                {
                    query = query.OrderBy(p => p.Brand);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Brand);
                }
            }

            else if (sortBy.ToLower() == "category")
            {
                if (orderBy == "asc")
                {
                    query = query.OrderBy(p => p.Category);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Category);
                }
            }
            else if (sortBy.ToLower() == "price")
            {
                if (orderBy == "asc")
                {
                    query = query.OrderBy(p => p.Price);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Price);
                }
            }
            else if (sortBy.ToLower() == "date")
            {
                if (orderBy == "asc")
                {
                    query = query.OrderBy(p => p.CreatedAt);
                }
                else
                {
                    query = query.OrderByDescending(p => p.CreatedAt);
                }
            }
            else
            {
                if (orderBy == "asc")
                {
                    query = query.OrderBy(p => p.Id);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Id);
                }
            }

            #endregion

            #region paging

            decimal productCount = query.Count();
            int pagesize = 5;

            int totalPage = (int)Math.Ceiling(productCount / pagesize);
            if (page <= 0 || page > totalPage || page == null) page = 1;
            query = query.Skip((int)(page - 1) * pagesize)
                         .Take(pagesize);


            #endregion

            var product = query.ToList();
            var response = new
            {
                TotalPage = totalPage,
                Pagesize = pagesize,
                Product = product,
                Page = page,
            };
            return Ok(response);
        }

        [HttpGet("GetAllProduct")]
        public IActionResult GetProducts()
        {
            var products = _context.Products.ToList();
            return Ok(products);
        }

        [HttpGet("{id}")]
        public IActionResult GetProduct(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }


        [Authorize(Roles = "admin")]
        [HttpPost]
        public IActionResult CreateProduct([FromForm] ProductDto productDto)
        {
            if (!categorylist.Contains(productDto.Category))
            {
                ModelState.AddModelError("Category", "please select a valid category ");
                return BadRequest(ModelState);

            }

            if (productDto.ImageFile == null)
            {
                ModelState.AddModelError("ImageFile", "The Image File Is Requierd");
                return BadRequest(ModelState);
            }
            //Save image in the Server
            string ImageFileName = DateTime.Now.ToString("yyyymmddhhmmssfff");
            ImageFileName += Path.GetExtension(productDto.ImageFile.FileName);
            var imageFolderpath = _environment.WebRootPath + "/images/products/";

            using (var stream = System.IO.File.Create(imageFolderpath + ImageFileName))
            {
                productDto.ImageFile.CopyTo(stream);
            }



            //Save product to DB
            Product product = new()
            {
                Name = productDto.Name,
                Category = productDto.Category,
                Brand = productDto.Brand,
                Price = productDto.Price,
                Description = productDto.Description ?? "",
                ImageFileName = ImageFileName,
                CreatedAt = DateTime.Now,
            };
            _context.Add(product);
            _context.SaveChanges();
            return Ok(product);

        }


        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public IActionResult UpdateProduct([FromForm] ProductDto productDto, int id)
        {
            if (!categorylist.Contains(productDto.Category))
            {
                ModelState.AddModelError("Category", "please select a valid category ");
                return BadRequest(ModelState);

            }

            var product = _context.Products.Find(id);

            if (product == null)
            {
                return BadRequest(ModelState);
            }

            var ImageFileName = product.ImageFileName;
            if (productDto.ImageFile != null)
            {

                //save new product  image
                ImageFileName = DateTime.Now.ToString("yyyymmddhhmmssfff");
                ImageFileName += Path.GetExtension(productDto.ImageFile.FileName);

                var imageFolderpath = _environment.WebRootPath + "/images/products/";
                using (var stream = System.IO.File.Create(imageFolderpath + ImageFileName))
                {
                    productDto.ImageFile.CopyTo(stream);
                }

                //delet the old product image
                System.IO.File.Delete(imageFolderpath + product.ImageFileName);
            }

            //Update Product

            product.Name = productDto.Name;
            product.Price = productDto.Price;
            product.Category = productDto.Category;
            product.Brand = productDto.Brand;
            product.Description = productDto.Description ?? "";
            product.ImageFileName = ImageFileName;

            _context.SaveChanges();
            return Ok(product);
        }

        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public IActionResult DeleteProduct(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            //Delete product image from server
            var imagepath = _environment.WebRootPath + "/images/products/";
            System.IO.File.Delete(imagepath + product.ImageFileName);

            //delete product from db
            _context.Products.Remove(product);
            _context.SaveChanges();
            return Ok(product);
        }

    }
}
