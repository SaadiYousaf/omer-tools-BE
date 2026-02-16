using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.Domain.Entites.EmailsModel
{
	public class EmailMessage
	{
		public string ToEmail { get; set; } = string.Empty;
		public string Subject { get; set; } = string.Empty;
		public string Body { get; set; } = string.Empty;
		public bool IsBodyHtml { get; set; } = true;

		// For basic testing
		public Dictionary<string, string> Placeholders { get; set; } = new();
	}

	// Simple models for testing
	public class OrderTestModel
	{
		public string CustomerName { get; set; } = "John Doe";
		public string CustomerEmail { get; set; } = "customer@example.com";
		public string OrderNumber { get; set; } = "ORD-001";
		public DateTime OrderDate { get; set; } = DateTime.Now;
		public string OrderStatus { get; set; } = "Processing";
		public decimal TotalAmount { get; set; } = 99.99m;
		public string ShippingAddress { get; set; } = "123 Main St, City, Country";
		public List<OrderItemTest> Items { get; set; } = new();
	}

	public class OrderItemTest
	{
		public string Name { get; set; } = string.Empty;
		public int Quantity { get; set; }
		public decimal Price { get; set; }
	}

	public class ClaimTestModel
	{
		public string CustomerName { get; set; } = "John Doe";
		public string CustomerEmail { get; set; } = "customer@example.com";
		public string ClaimId { get; set; } = "CLM-001";
		public DateTime ClaimDate { get; set; } = DateTime.Now;
		public string ClaimStatus { get; set; } = "Under Review";
		public string ClaimType { get; set; } = "Return";
		public string OrderNumber { get; set; } = "ORD-001";
		public string ClaimDescription { get; set; } = "Received damaged product";
		public string NextSteps { get; set; } = "Our team will review your claim within 2 business days.";
		public string ProcessingTime { get; set; } = "2-3";
	}
}
