using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using ABCRetail_Part1.Models;
using ABCRetail_Part1.Services;
using Newtonsoft.Json;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace ABCRetail_Part1.Controllers
{
    [Authorize]
    public class OurStoreController : Controller
    {
        private readonly TableStorageService _tableStorageService; 
        private readonly QueueService _queueService;
        private readonly HttpClient _httpClient;

        //constructor to initialise TableStorage and QueueStorage
        public OurStoreController(TableStorageService tableStorageService, QueueService queueService, HttpClient httpClient)
        {
            _tableStorageService = tableStorageService;
            _queueService = queueService;
            _httpClient = httpClient;
        }

        //display our store view
        public async Task<IActionResult> Index(string searchString)
        {
            //fetch products from Table Storage
            var products = await _tableStorageService.GetAllProductsAsync();

            //filter the products by name using searchstring
            if (!string.IsNullOrEmpty(searchString))
            {
                products = products
                    .Where(p => p.ProductName != null && p.ProductName.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return View(products);
        }

        //display place order view 
        public async Task<IActionResult> PlaceOrder(string partitionKey, string rowKey)
        {
            //validate input parameters
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            //fetch the product 
            var product = await _tableStorageService.GetProductAsync(partitionKey, rowKey);
            if (product == null)
            {
                return NotFound();
            }

            //fetch all users (even if null)
            var users = await _tableStorageService.GetAllUsersAsync();
            if (users == null)
            {
                users = new List<User>();
            }

            //pass product details, shipping fee, and users list to view 
            ViewData["Product"] = product;
            ViewData["ShippingFee"] = product.Price.GetValueOrDefault() * 0.10; //calculate 10% of product price 
            ViewData["Users"] = users;

            return View();
        }

        //handle place order form submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(string streetAddress, string city, string country, string paymentMethod, string partitionKey, string rowKey)
        {
            if (string.IsNullOrEmpty(partitionKey) || string.IsNullOrEmpty(rowKey))
            {
                return NotFound();
            }

            //fetch product
            var product = await _tableStorageService.GetProductAsync(partitionKey, rowKey);
            if (product == null)
            {
                return NotFound();
            }

            if (product.StockQuantity <= 0)
            {
                return BadRequest("Product is out of stock.");
            }

            //fetch the logged-in user ID
            var loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(loggedInUserId))
            {
                return Unauthorized();
            }

            //decrement product stock
            product.StockQuantity--;
            await _tableStorageService.AddProductAsync(product);

            //determine next transaction ID
            var transactions = await _tableStorageService.GetAllTransactionsAsync();
            int maxTransactionId = transactions.Any() ? transactions.Max(t => t.TransactionId) : 0;

            //****************
            //Code Attribution
            //The following coode was taken from StackOverflow:
            //Author: Rayn Avery
            //Link: https://stackoverflow.com/questions/46909990/how-to-work-out-a-percentage-for-total-sales
            //****************

            //create new transaction object
            var transaction = new Transaction
            {
                PartitionKey = "TransactionsPartition",
                RowKey = Guid.NewGuid().ToString(),
                UserId = int.Parse(loggedInUserId), //logged-in user's ID
                ProductId = product.ProductId,
                TransactionDate = DateTime.UtcNow.AddHours(2), //South African time
                TransactionId = maxTransactionId + 1,
                TransactionTotalPrice = Math.Round(product.Price.GetValueOrDefault() * 1.10, 2), //total price with 10% shipping
                TransactionPaymentMethod = paymentMethod,
                TransactionStatus = "Pending"
            };

            //save new transaction
            await _tableStorageService.AddTransactionAsync(transaction);

            //call the Azure Function to send the message to the queue
            string functionUrl = "https://abcretailqueuestoragefunction.azurewebsites.net/api/SendMessage?code=tMcGjiPAuo4-2NhZheSDEuWXE0E-8CQHa8PlyG9XLKZHAzFuy7lERQ%3D%3D";

            var transactionJson = JsonConvert.SerializeObject(transaction);
            var content = new StringContent(transactionJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(functionUrl, content);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("OrderConfirmation", new { transactionId = transaction.RowKey });
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Failed to send transaction to queue.");
                return View(product); 
            }
        }

        // display order confirmation view
        public IActionResult OrderConfirmation()
        {
            return View();
        }
    }
}
