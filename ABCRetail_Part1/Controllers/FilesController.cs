using ABCRetail_Part1.Models;
using ABCRetail_Part1.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ABCRetail_Part1.Controllers
{
    [Authorize]
    public class FilesController : Controller
    {
        private readonly AzureFileShareService _fileShareService;
        private readonly HttpClient _httpClient;

        //constructor to initialise fileServices and httpClient
        public FilesController(AzureFileShareService fileShareService, HttpClient httpClient)
        {
            _fileShareService = fileShareService;
            _httpClient = httpClient;
        }

        //display the files view
        public async Task<IActionResult> Index()
        {
            List<FileModel> files;
            string currentUserEmail = User.Identity.Name; 
            bool isAdmin = User.IsInRole("Admin");

            try
            {
                //retrieve list of files from 'uploads' directory
                files = await _fileShareService.ListFilesAsync("uploads");

                //filter files based on user role and identity
                if (!isAdmin)
                {
                    //allow the user to see only their own files and the public contract file
                    files = files.Where(f =>
                        f.Name.Equals("ABCRetail Customer Order Contract.pdf", StringComparison.OrdinalIgnoreCase) ||
                        f.Name.StartsWith(currentUserEmail, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"Failed to load files: {ex.Message}";
                files = new List<FileModel>();
            }

            return View(files);
        }

        //handle file upload
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            //check if a file is selected for upload
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("file", "Please select a file to upload.");
                List<FileModel> files = await _fileShareService.ListFilesAsync("uploads");
                return View("Index", files);
            }

            //validate file size (4 MB limit)
            const long maxFileSize = 4 * 1024 * 1024; 
            if (file.Length > maxFileSize)
            {
                ModelState.AddModelError("file", "File size cannot exceed 4 MB.");
                List<FileModel> existingFiles = await _fileShareService.ListFilesAsync("uploads");
                return View("Index", existingFiles);
            }

            try
            {
                string directoryName = "uploads";
                string fileName = file.FileName;

                //check for duplicate file names
                List<FileModel> existingFiles = await _fileShareService.ListFilesAsync(directoryName);
                if (existingFiles.Any(f => f.Name.Equals(fileName, System.StringComparison.OrdinalIgnoreCase)))
                {
                    ModelState.AddModelError("file", "A file with the same name already exists. Please choose a different file or rename the file.");
                    return View("Index", existingFiles);
                }

                //upload the file using the Azure Function
                using (var fileContent = new MultipartFormDataContent())
                {
                    using (var stream = file.OpenReadStream())
                    {
                        fileContent.Add(new StreamContent(stream), "file", file.FileName);

                        //call the Azure Function to handle the file upload
                        var response = await _httpClient.PostAsync("https://abcretailfilestoragefunction.azurewebsites.net/api/UploadFileFunction?code=6H755EorOBbnOSclI9aVsOkpVbUuJlT_UqBZ4lBNHBHzAzFug_lVCA%3D%3D", fileContent);

                        if (response.IsSuccessStatusCode)
                        {
                            TempData["Message"] = $"File '{file.FileName}' uploaded successfully!";
                        }
                        else
                        {
                            TempData["Message"] = $"File upload failed with status code {response.StatusCode}.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"File upload failed: {ex.Message}";
            }

            return RedirectToAction("Index");
        }


        // Download File
        [HttpGet]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            //handle case where file name is null or empty
            if (string.IsNullOrEmpty(fileName))
            {
                return BadRequest("File name cannot be null or empty.");
            }

            try
            {
                //download the file from the 'uploads' directory
                var fileStream = await _fileShareService.DownloadFileAsync("uploads", fileName);

                //file is not found validation
                if (fileStream == null)
                {

                    return NotFound($"File '{fileName}' not found.");
                }

                //return file as a downloadable attachment
                return File(fileStream, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                //handle errors during file download
                return BadRequest($"Error downloading file: {ex.Message}");
            }
        }
    }
}