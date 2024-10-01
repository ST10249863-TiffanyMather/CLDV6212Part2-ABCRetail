using Azure;
using Azure.Data.Tables;
using ABCRetail_Part1.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABCRetail_Part1.Services
{
    public class TableStorageService
    {
        //table clients for different entities
        private readonly TableClient _productTableClient;
        private readonly TableClient _userTableClient;
        private readonly TableClient _transactionTableClient;

        //constructor initializes TableClient instances with provided connection string
        public TableStorageService(string connectionString)
        {
            _productTableClient = new TableClient(connectionString, "Products");
            _userTableClient = new TableClient(connectionString, "Users");
            _transactionTableClient = new TableClient(connectionString, "Transactions");
        }

        //method to retrieve all products from Products table
        public async Task<List<Product>> GetAllProductsAsync()
        {
            var products = new List<Product>();

            await foreach (var product in _productTableClient.QueryAsync<Product>())
            {
                products.Add(product); 
            }

            return products;
        }

        //method to add or update product in Products table
        public async Task AddProductAsync(Product product)
        {
            if (string.IsNullOrEmpty(product.PartitionKey) || string.IsNullOrEmpty(product.RowKey))
            {
                throw new ArgumentException("PartitionKey and RowKey must be set."); 
            }

            try
            {
                await _productTableClient.UpsertEntityAsync(product); //UpsertEntityAsync to handle updates
            }
            catch (RequestFailedException ex)
            {
                throw new InvalidOperationException("Error adding or updating entity in Table Storage", ex);
            }
        }

        //method to delete a product from the Products table
        public async Task DeleteProductAsync(string partitionKey, string rowKey)
        {
            await _productTableClient.DeleteEntityAsync(partitionKey, rowKey); 
        }

        //method to retrieve a single product by its partition key and row key
        public async Task<Product?> GetProductAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _productTableClient.GetEntityAsync<Product>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null; 
            }
        }

        //method to retrieve all users from Users table
        public async Task<List<User>> GetAllUsersAsync()
        {
            var users = new List<User>();

            await foreach (var user in _userTableClient.QueryAsync<User>())
            {
                users.Add(user);
            }

            return users;
        }

        //method to add or update user in Users table
        public async Task AddUserAsync(User user)
        {
            if (string.IsNullOrEmpty(user.PartitionKey) || string.IsNullOrEmpty(user.RowKey))
            {
                throw new ArgumentException("PartitionKey and RowKey must be set."); 
            }

            try
            {
                await _userTableClient.UpsertEntityAsync(user); //UpsertEntityAsync handles updates
            }
            catch (RequestFailedException ex)
            {
                throw new InvalidOperationException("Error adding or updating entity in Table Storage", ex);
            }
        }

        //method to delete user from Users table
        public async Task DeleteUserAsync(string partitionKey, string rowKey)
        {
            await _userTableClient.DeleteEntityAsync(partitionKey, rowKey); 
        }

        //method to retrieve single user by its partition key and row key
        public async Task<User?> GetUserAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _userTableClient.GetEntityAsync<User>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null; 
            }
        }

        //method to add transaction to Transactions table
        public async Task AddTransactionAsync(Transaction transaction)
        {
            if (string.IsNullOrEmpty(transaction.PartitionKey) || string.IsNullOrEmpty(transaction.RowKey))
            {
                throw new ArgumentException("PartitionKey and RowKey must be set."); 
            }

            try
            {
                await _transactionTableClient.UpsertEntityAsync(transaction, TableUpdateMode.Replace);
            }
            catch (RequestFailedException ex)
            {
                throw new InvalidOperationException("Error adding or updating transaction in Table Storage", ex);
            }
        }

        //method to update  transaction in Transactions table
        public async Task UpdateTransactionAsync(Transaction transaction)
        {
            if (string.IsNullOrEmpty(transaction.PartitionKey) || string.IsNullOrEmpty(transaction.RowKey))
            {
                throw new ArgumentException("PartitionKey and RowKey must be set."); 
            }

            try
            {
                await _transactionTableClient.UpsertEntityAsync(transaction, TableUpdateMode.Replace);
            }
            catch (RequestFailedException ex)
            {
                throw new InvalidOperationException("Error updating transaction in Table Storage", ex);
            }
        }

        //method to retrieve all transactions from Transactions table
        public async Task<List<Transaction>> GetAllTransactionsAsync()
        {
            var transactions = new List<Transaction>();

            await foreach (var transaction in _transactionTableClient.QueryAsync<Transaction>())
            {
                transactions.Add(transaction); 
            }

            return transactions;
        }

        //method to delete transaction from the Transactions table
        public async Task DeleteTransactionAsync(string partitionKey, string rowKey)
        {
            await _transactionTableClient.DeleteEntityAsync(partitionKey, rowKey); 
        }

        //method to retrieve single transaction by its partition key and row key
        public async Task<Transaction?> GetTransactionAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _transactionTableClient.GetEntityAsync<Transaction>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null; 
            }
        }
    }
}
