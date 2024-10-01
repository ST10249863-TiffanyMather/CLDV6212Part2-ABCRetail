using Azure;
using Azure.Data.Tables;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Tracing;

namespace ABCRetail_Part1.Models
{
    public class Product : ITableEntity
    {
        [Key]
        public int ProductId { get; set; }

        public string? ProductName { get; set; }

        public string? Description { get; set; }

        public string? Category { get; set; }

        public double? Price { get; set; }

        public int? StockQuantity { get; set; }

        public string? ImageURL { get; set; }

        //required by ITableEntity - ITableEntity properties 
        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }
        public ETag ETag { get; set; } = ETag.All;
        public DateTimeOffset? Timestamp { get; set; }
    }
}
