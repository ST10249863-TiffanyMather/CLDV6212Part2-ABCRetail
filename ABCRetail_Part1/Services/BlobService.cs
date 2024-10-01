using System;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ABCRetail_Part1.Services
{
    public class BlobService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName = "products";
        private readonly HttpClient _httpClient;

        //constructor initializes BlobServiceClient and httpClient
        public BlobService(string connectionString, HttpClient httpClient)
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
            _httpClient = httpClient;
        }

        //method to upload file stream to Blob Storage and return URL of uploaded blob
        public async Task<string> UploadAsync(Stream fileStream, string fileName)
        {
            var functionUrl = "https://abcretailblobstoragefunction.azurewebsites.net/api/UploadImageFunction?code=paX4dPdQPcH6H_PaYGzuFhf0Ic6vHV7dBlzDpC-le4PlAzFuscaCdA%3D%3D"; 
            using var content = new StreamContent(fileStream);
            content.Headers.Add("file-name", fileName);  
            var response = await _httpClient.PostAsync(functionUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to upload to Blob Storage");
            }

            //construct the blob URL based on the file name 
            var blobUrl = $"https://st10249863storageaccount.blob.core.windows.net/products/{fileName}";
            return blobUrl;  
        }

        //method to delete blob from Blob Storage given its URI
        public async Task DeleteBlobAsync(string blobUri)
        {
            Uri uri = new Uri(blobUri);
            string blobName = uri.Segments[^1];
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
        }

        //method to check if blob exists in Blob Storage container
        public async Task<bool> BlobExistsAsync(string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            return await blobClient.ExistsAsync();
        }
    }
}