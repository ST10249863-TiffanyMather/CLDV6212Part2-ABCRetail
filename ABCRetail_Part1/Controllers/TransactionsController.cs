using ABCRetail_Part1.Models;
using ABCRetail_Part1.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ABCRetail_Part1.Controllers
{
    [Authorize]
    public class TransactionsController : Controller
    {
        private readonly TableStorageService _tableStorageService;
        private readonly QueueService _queueService;

        //constructor to initialize TableStorageService and QueueService
        public TransactionsController(TableStorageService tableStorageService, QueueService queueService)
        {
            _tableStorageService = tableStorageService;
            _queueService = queueService;
        }

        //display transactions
        public async Task<IActionResult> Index()
        {
            var currentUserEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            bool isAdmin = User.IsInRole("Admin");

            var transactions = await _tableStorageService.GetAllTransactionsAsync();
            var users = await _tableStorageService.GetAllUsersAsync();
            var products = await _tableStorageService.GetAllProductsAsync();

            //filter transactions based on user role and identity
            var transactionDetails = from transaction in transactions
                                     join user in users on transaction.UserId equals user.UserId
                                     join product in products on transaction.ProductId equals product.ProductId
                                     select new
                                     {
                                         transaction.RowKey,
                                         UserEmail = user.Email,
                                         ProductId = product.ProductId,
                                         ProductName = product.ProductName,
                                         ProductURL = product.ImageURL,
                                         ProductCategory = product.Category,
                                         ProductStockQuantity = product.StockQuantity,
                                         TransactionDate = transaction.TransactionDate,
                                         TransactionTotalPrice = transaction.TransactionTotalPrice,
                                         TransactionStatus = transaction.TransactionStatus
                                     };

            //if user is not admin, filter to show only their own transactions
            if (!isAdmin)
            {
                transactionDetails = transactionDetails.Where(t => t.UserEmail == currentUserEmail);
            }

            return View(transactionDetails.ToList());
        }


        //delete transaction
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            await _tableStorageService.DeleteTransactionAsync(partitionKey, rowKey);
            return RedirectToAction("Index");
        }

        //change status of transaction
        [HttpPost]
        public async Task<IActionResult> ChangeStatus(string partitionKey, string rowKey)
        {
            var transaction = await _tableStorageService.GetTransactionAsync(partitionKey, rowKey);
            if (transaction == null)
            {
                return View("Index");
            }

            //update status
            transaction.TransactionStatus = "Processed";

            try
            {
                await _tableStorageService.UpdateTransactionAsync(transaction);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error updating transaction: {ex.Message}");
                return View(transaction);
            }
        }

        //display form to add new transaction
        public async Task<IActionResult> AddTransaction()
        {
            var users = await _tableStorageService.GetAllUsersAsync();
            var products = await _tableStorageService.GetAllProductsAsync();

            //check for null or empty lists of users and products
            if (users == null || users.Count == 0)
            {
                ModelState.AddModelError("", "No users found. Please add user first.");
                return View();
            }

            if (products == null || products.Count == 0)
            {
                //no products are found
                ModelState.AddModelError("", "No products found. Please add product first.");
                return View();
            }

            //pass list of users and products to view
            ViewData["Users"] = users;
            ViewData["Products"] = products;

            return View();
        }

        //handle \form submission to add new transaction
        [HttpPost]
        public async Task<IActionResult> AddTransaction(Transaction transaction)
        {
            if (ModelState.IsValid)
            {
                //fetch selected product to get the price and stock quantity
                var product = (await _tableStorageService.GetAllProductsAsync()).FirstOrDefault(p => p.ProductId == transaction.ProductId);
                if (product == null)
                {
                    ModelState.AddModelError("", "Selected product does not exist.");
                    return View(transaction);
                }

                //check if product is out of stock
                if (product.StockQuantity <= 0)
                {
                    ModelState.AddModelError("", "The selected product is out of stock.");
                    return View(transaction);
                }

                // decrease the stock quantity by 1 
                product.StockQuantity -= 1;

                //update the product in the Table Storage
                await _tableStorageService.AddProductAsync(product);

                //****************
                //Code Attribution
                //The following coode was taken from StackOverflow:
                //Author: Rayn Avery
                //Link: https://stackoverflow.com/questions/46909990/how-to-work-out-a-percentage-for-total-sales
                //****************

                //calculate total price (10% of the product's price)
                transaction.TransactionTotalPrice = product.Price * 1.1;

                //set status to 'Pending'
                transaction.TransactionStatus = "Pending";

                //set default properties for the new transaction
                var transactions = await _tableStorageService.GetAllTransactionsAsync();
                int maxTransactionId = transactions.Any() ? transactions.Max(t => t.TransactionId) : 0;
                transaction.TransactionId = maxTransactionId + 1;
                transaction.TransactionDate = DateTime.SpecifyKind(transaction.TransactionDate, DateTimeKind.Utc);
                transaction.PartitionKey = "TransactionsPartition";
                transaction.RowKey = Guid.NewGuid().ToString();

                //save transaction to Table Storage
                await _tableStorageService.AddTransactionAsync(transaction);

                //send message to queue 
                string message = $"New transaction by User {transaction.UserId} of Product {transaction.ProductId} on {transaction.TransactionDate}";
                await _queueService.SendMessageAsync(message);

                //redirect to Index after adding transaction
                return RedirectToAction("Index");
            }

            //reload users and products lists if validation fails
            var users = await _tableStorageService.GetAllUsersAsync();
            var products = await _tableStorageService.GetAllProductsAsync();
            ViewData["Users"] = users;
            ViewData["Products"] = products;

            return View(transaction);
        }
    }
}