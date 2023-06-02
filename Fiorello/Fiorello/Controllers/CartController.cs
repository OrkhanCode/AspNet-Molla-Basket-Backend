using Fiorello.Data;
using Fiorello.Models;
using Fiorello.Services.Interfaces;
using Fiorello.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.ContentModel;
using System.Text.Json;

namespace Fiorello.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _accessor;
        private readonly IProductService _productService;

        public CartController(AppDbContext context, 
                              IHttpContextAccessor accessor, 
                              IProductService productService)
        {
            _context = context;
            _accessor = accessor;
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            List<BasketDetailVM> basketList = new();

            if (_accessor.HttpContext.Session.GetString("basket") != null)
            {
                List<BasketVM> basketDatas = JsonSerializer.Deserialize<List<BasketVM>>(_accessor.HttpContext.Session.GetString("basket"));

                foreach (var item in basketDatas)
                {
                    var product = await _context.Products.Include("Images").FirstOrDefaultAsync(m => m.Id == item.Id);

                    if(product != null)
                    {
                        BasketDetailVM basketDetail = new()
                        {
                            Id = product.Id,
                            Name = product.Name,
                            Image = product.Images.Where(m => !m.SoftDelete).FirstOrDefault().Image,
                            Count = item.Count,
                            Price = product.Price,
                            TotalPrice = item.Count * product.Price
                        };

                        basketList.Add(basketDetail);
                    }
                }
            }

            return View(basketList);
        }

        [HttpPost]
        public async Task<IActionResult> AddBasket(int? id)
        {
            if (id is null) return BadRequest();

            Product product = await _productService.GetByIdAsync(id);

            if (product is null) return NotFound();

            List<BasketVM> basket = GetBasketDatas();

            AddProductToBasket(basket, product);

            int basketCount = GetBasketCount();

            return Ok(basketCount);
        }

        private List<BasketVM> GetBasketDatas()
        {
            List<BasketVM> basket;

            if (_accessor.HttpContext.Session.GetString("basket") != null)
            {
                basket = JsonSerializer.Deserialize<List<BasketVM>>(_accessor.HttpContext.Session.GetString("basket"));
            }
            else
            {
                basket = new List<BasketVM>();
            }

            return basket;
        }

        private void AddProductToBasket(List<BasketVM> basket, Product product)
        {
            BasketVM existProduct = basket.FirstOrDefault(m => m.Id == product.Id);

            if (existProduct is null)
            {
                basket.Add(new BasketVM
                {
                    Id = product.Id,
                    Count = 1
                });
            }
            else
            {
                existProduct.Count++;
            }

            _accessor.HttpContext.Session.SetString("basket", JsonSerializer.Serialize(basket));
        }

        private int GetBasketCount()
        {
            int count = 0;

            var products = JsonSerializer.Deserialize<List<BasketDetailVM>>(_accessor.HttpContext.Session.GetString("basket"));

            if(products != null)
            {
                count = products.Sum(m => m.Count);
            }

            return count;
        }

        [HttpPost]
        [ActionName("Delete")]
        public IActionResult DeleteProductFromBasket(int? id)
        {
            if (id is null) return BadRequest();

            var products = JsonSerializer.Deserialize<List<BasketDetailVM>>(_accessor.HttpContext.Session.GetString("basket"));

            var deleteProduct = products.FirstOrDefault(m => m.Id == id);

            int deleteIndex = products.IndexOf(deleteProduct);

            products.RemoveAt(deleteIndex);

            _accessor.HttpContext.Session.SetString("basket", JsonSerializer.Serialize(products));

            return Ok();
        }
    }
}
