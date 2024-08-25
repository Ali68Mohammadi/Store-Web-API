using BestStoreApi.Models.Dto;
using BestStoreApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BestStoreApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("PaymentMethods")]
        public IActionResult GetPaymentMethods()
        {
            return Ok(OrderHelper.PaymentMethods);
        }

        [HttpGet]
        public IActionResult GetCart(string productIdentifiers)
        {
            CartDto cartDto = new()
            {
                CartItems = new List<CartItemDto>(),
                ShippingFee = OrderHelper.ShippingFee,
                TotalPrice = 0,
                SubTotal = 0
            };

            Dictionary<int, int> productDic = OrderHelper.GetProductDictionary(productIdentifiers);

            foreach (var pair in productDic)
            {
                var productId = pair.Key;
                var product = _context.Products.Find(productId);
                if (product == null)
                {
                    continue;
                }

                CartItemDto cartItem = new()
                {
                    Product = product,
                    Quantity = pair.Value
                };

                cartDto.CartItems.Add(cartItem);
                cartDto.SubTotal += product.Price * cartItem.Quantity;
                cartDto.TotalPrice = cartDto.SubTotal + cartDto.ShippingFee;
            }
            return Ok(cartDto);
        }
    }
}
