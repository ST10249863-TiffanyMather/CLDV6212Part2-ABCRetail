using ABCRetail_Part1.Models;
using ABCRetail_Part1.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ABCRetail_Part1.Controllers
{
    public class ProductsController : Controller
    {
        private readonly BlobService _blobService;
        private readonly TableStorageService _tableStorageService;
        private readonly HttpClient _httpClient;

        //constructor to initialize BlobService, TableStorageService and HttpClient
        public ProductsController(BlobService blobService, TableStorageService tableStorageService, HttpClient httpClient)
        {
            _blobService = blobService; 
            _tableStorageService = tableStorageService; 
            _httpClient = httpClient;
        }

        //display form for adding new product
        [HttpGet]
        public IActionResult AddProduct()
        {
            return View();
        }

        //handle submission of new product form
        [HttpPost]
        public async Task<IActionResult> AddProduct(Product product, IFormFile file)
        {
            if (file != null)
            {
                //valid image types
                var validImageTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
                var fileType = file.ContentType;

                //image type validation
                if (!validImageTypes.Contains(fileType))
                {
                    TempData["FileError"] = "Please upload a valid image file (JPEG, JPG, PNG, GIF).";
                    return View(product);
                }

                //name already exists in blob check
                if (await _blobService.BlobExistsAsync(file.FileName))
                {
                    TempData["FileError"] = "An image with this name already exists. Please upload a different image.";
                    return View(product);
                }

                //upload file to Blob Storage
                using var stream = file.OpenReadStream();
                var imageURL = await _blobService.UploadAsync(stream, file.FileName);
                product.ImageURL = imageURL;
            }

            if (ModelState.IsValid)
            {
                var jsonProduct = JsonConvert.SerializeObject(product);
                var content = new StringContent(jsonProduct, Encoding.UTF8, "application/json");

                //call the AddProductFunction in Azure Function
                var response = await _httpClient.PostAsync("https://abcretailtablestoragefunction.azurewebsites.net/api/AddProductFunction?code=_gUAF6KTmMDDKKr7_DpGLgUInHEJipxpNWR1YV0KKASIAzFu37hFMA%3D%3D", content);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to add the product.";
                    return View(product);
                }
            }

            return View(product);
        }

        //handle product deletion
        [HttpPost]
        public async Task<IActionResult> DeleteProduct(string partitionKey, string rowKey, Product product)
        {
            //check if the product exists and has an associated image URL
            if (product != null && !string.IsNullOrEmpty(product.ImageURL))
            {
                try
                {
                    // Delete associated blob image using BlobService
                    await _blobService.DeleteBlobAsync(product.ImageURL);
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Failed to delete the associated image. " + ex.Message;
                }
            }

            //delete product entity from Table Storage
            await _tableStorageService.DeleteProductAsync(partitionKey, rowKey);

            return RedirectToAction("Index");
        }

        //display form for editing a product
        [HttpGet]
        public async Task<IActionResult> EditProduct(string partitionKey, string rowKey)
        {
            var product = await _tableStorageService.GetProductAsync(partitionKey, rowKey);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        //handle submission of the edited product form
        [HttpPost]
        public async Task<IActionResult> EditProduct(Product product, IFormFile? file)
        {
            //if a new file is uploaded, update the image URL
            if (file != null)
            {
                var validImageTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
                var fileType = file.ContentType;

                //image type validation
                if (!validImageTypes.Contains(fileType))
                {
                    TempData["FileError"] = "Please upload a valid image file (JPEG, JPG, PNG, GIF).";
                    return View(product);
                }

                //delete the old image if it exists
                if (!string.IsNullOrEmpty(product.ImageURL))
                {
                    await _blobService.DeleteBlobAsync(product.ImageURL);
                }

                //upload new image to Blob Storage
                using var stream = file.OpenReadStream();
                var imageURL = await _blobService.UploadAsync(stream, file.FileName);
                product.ImageURL = imageURL;
            }

            if (ModelState.IsValid)
            {
                //update product in Table Storage
                await _tableStorageService.AddProductAsync(product);

                return RedirectToAction("Index");
            }

            return View(product);
        }

        //display all products
        public async Task<IActionResult> Index()
        {
            //fetch all products from Table Storage
            var products = await _tableStorageService.GetAllProductsAsync();
            return View(products);
        }

    }
}
