using Azure;
using Azure.Data.Tables;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Tracing;

namespace ABCRetail_Part1.Models
{
    public class Transaction : ITableEntity
    {
        [Key]
        public int TransactionId { get; set; }

        //required by ITableEntity - ITableEntity properties 
        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }
        public ETag ETag { get; set; } = ETag.All;
        public DateTimeOffset? Timestamp { get; set; }

        //validation
        [Required(ErrorMessage = "User is required.")]
        public int UserId { get; set; } //FK to User who made Transaction

        [Required(ErrorMessage = "Product ID is required.")]
        public int ProductId { get; set; } //FK to Product in Transaction

        [Required(ErrorMessage = "Transaction date is required.")]
        public DateTime TransactionDate { get; set; }
        public double? TransactionTotalPrice { get; set; }

        [Required(ErrorMessage = "Payment method is required.")]
        public string? TransactionPaymentMethod { get; set; }

        public string? TransactionStatus { get; set; }

    }
}
