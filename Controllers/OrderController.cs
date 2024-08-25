using BestStoreApi.Models;
using BestStoreApi.Models.Dto;
using BestStoreApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BestStoreApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Authorize]
        [HttpPost]
        public IActionResult CreateOrder(OrderDto orderDto)
        {
            //check  payment method is valid or not
            if (!OrderHelper.PaymentMethods.ContainsKey(orderDto.PaymentMethod))
            {
                ModelState.AddModelError("Payment Method", "Please select a valid payment method");
                return BadRequest(ModelState);
            }
            //find login user
            var userId = JwtReader.GetUserId(User);
            var user = _context.Users.Find(userId);

            if (user == null)
            {
                ModelState.AddModelError("User", "unable to create the order ");
                return BadRequest(ModelState);
            }
            //convert products Identifiers to dictionary
            var productDic = OrderHelper.GetProductDictionary(orderDto.ProductIdentifiers);

            //create new order
            Order order = new()
            {
                UserID = userId,
                CreateAt = DateTime.Now,
                DeliveryAddress = orderDto.DeliverAddress,
                ShippingFee = OrderHelper.ShippingFee,
                PaymentMethod = orderDto.PaymentMethod,
                PaymentStatus = OrderHelper.PaymentStatus[0],//=pending
                OrderStatus = OrderHelper.OrderStatus[0],//=Created

            };
            //var  (productId, quantity) in productDic == var pair(dictionary) in productDic
            foreach (var (productId, quantity) in productDic)
            {
                var product = _context.Products.Find(productId);
                if (product == null)
                {
                    ModelState.AddModelError("Product", $"product with {productId} is not available ");
                    return BadRequest(ModelState);
                }
                OrderItem orderItem = new()
                {

                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = product.Price,

                };
                order.OrderItems.Add(orderItem);
            }

            //if no order Item entered
            if (order.OrderItems.Count < 1)
            {
                ModelState.AddModelError("Product", "unable to create the order");
                return BadRequest(ModelState);

            }

            //save the order in the Db
            _context.Orders.Add(order);
            _context.SaveChanges();

            //get rig of the order cycle
            foreach (var item in order.OrderItems)
            {
                item.Order = null;
            }

            //hide user password
            order.User.Password = "*******";

            return Ok(order);
        }

        [Authorize]
        [HttpGet]
        public IActionResult GetOrder(int? page)
        {
            var userId = JwtReader.GetUserId(User);
            var userRole = JwtReader.GetUserRole(User);



            //find user is admin or not 
            IQueryable<Order> query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(or => or.Product);

            if (userRole != "admin")
            {
                query = query.Where(o => o.UserID == userId);
            }


            query = query.OrderByDescending(o => o.Id);

            //implement pagination
            if (page < 0 || page == null)
                page = 1;

            int pagesize = 5;
            int totalPage = 0;

            decimal userCount = query.Count();
            totalPage = (int)Math.Ceiling(userCount / pagesize);

            query = query.Skip((int)(page - 1) * pagesize)
                .Take(pagesize);

            //read Order
            var orders = query
                .ToList();

            foreach (var order in orders)
            {
                //get rid of object cycle
                foreach (var item in order.OrderItems)
                {
                    item.Order = null;

                }

                order.User.Password = "*******";
            }

            var response = new
            {

                Order = orders,
                TotalPage = totalPage,
                pagesize = pagesize,
                PageNumber = page,
            };

            return Ok(response);
        }

        [Authorize]
        [HttpGet("{id}")]
        public IActionResult GetUser(int id)
        {
            //find current user
            var userId = JwtReader.GetUserId(User);
            var role = JwtReader.GetUserRole(User);



            Order order = new();

            //admin seen all orders
            if (role == "admin")
            {
                order = _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefault(order => order.Id == id);
            }

            //user can see only user order
            else
            {
                order = _context.Orders
                   .Include(o => o.User)
                   .Include(o => o.OrderItems)
                   .ThenInclude(oi => oi.Product)
                   .FirstOrDefault(order => order.Id == id && order.UserID == userId);
            }

            if (order == null)
            {
                ModelState.AddModelError("Order", "order not found!");
                return BadRequest(ModelState);
            }

            //get rid of object cycle
            foreach (var item in order.OrderItems)
            {
                item.Order = null;

            }
            //hide user password    
            order.User.Password = "******";




            return Ok(order);

        }

        [Authorize]
        [HttpPut("{id}")]
        public IActionResult UpdateOrder(int id, string? orderStatus, string? paymentStatus)
        {

            if (orderStatus == null && paymentStatus == null)
            {
                ModelState.AddModelError("Update Order", "there is Nothing to Update");
                return BadRequest(ModelState);
            }

            if (orderStatus != null && !OrderHelper.OrderStatus.Contains(orderStatus))
            {
                ModelState.AddModelError("order status ", "the order status is not valid");
                return BadRequest(ModelState);
            }

            if (paymentStatus != null && !OrderHelper.PaymentStatus.Contains(paymentStatus))
            {
                ModelState.AddModelError("payment status ", "the payment status is not valid");
                return BadRequest(ModelState);
            }

            var order = _context.Orders.Find(id);
            if (order == null)
            {
                NotFound();
            }
            if (orderStatus != null)
            {
                order.OrderStatus = orderStatus;
            }

            if (paymentStatus != null)
            {
                order.PaymentStatus = paymentStatus;
            }
            _context.SaveChanges();
            return Ok(order);

        }


        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public IActionResult DeleteOrder(int id)
        {
            var order = _context.Orders.Find(id);
            if (order == null)
            {
              return  NotFound();
            }
            _context.Remove(order);
            _context.SaveChanges();

            return Ok($"delete successfuly:  order {order.Id} is deleted");
        }

    }
}
