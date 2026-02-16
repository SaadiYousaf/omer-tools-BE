using ProductService.Domain.Entites.EmailsModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProductService.Business.Helper
{
	public static class TemplateHelper
	{
		private static readonly string TemplateFolder =
			Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates");

		public static async Task<string> LoadTemplate(string templateName)
		{
			var templatePath = Path.Combine(TemplateFolder, $"{templateName}.html");

			if (!File.Exists(templatePath))
			{
				throw new FileNotFoundException($"Template '{templateName}' not found at {templatePath}");
			}

			return await File.ReadAllTextAsync(templatePath);
		}

		public static string ReplacePlaceholders(string content, Dictionary<string, string> placeholders)
		{
			foreach (var placeholder in placeholders)
			{
				content = content.Replace($"{{{{{placeholder.Key}}}}}", placeholder.Value);
			}

			return content;
		}
		public static string ReplaceComplexPlaceholders(string template, Dictionary<string, string> placeholders)
		{
			var result = template;

			// Handle conditional blocks
			result = HandleConditionalBlocks(result, placeholders);

			// Handle array placeholders
			result = HandleArrayPlaceholders(result, placeholders);

			// Handle regular placeholders
			foreach (var placeholder in placeholders)
			{
				var pattern = $"{{{{{placeholder.Key}}}}}";
				result = result.Replace(pattern, placeholder.Value ?? string.Empty);
			}

			return result;
		}

		private static string HandleConditionalBlocks(string template, Dictionary<string, string> placeholders)
		{
			var result = template;

			// Handle {{#if Condition}}...{{/if}}
			var ifRegex = new Regex(@"\{\{\s*#if\s+(\w+)\s*\}\}(.*?)\{\{\s*/if\s*\}\}", RegexOptions.Singleline);

			foreach (Match match in ifRegex.Matches(result))
			{
				var condition = match.Groups[1].Value;
				var content = match.Groups[2].Value;

				if (placeholders.TryGetValue(condition, out var value) &&
					!string.IsNullOrEmpty(value) &&
					value.ToLower() != "false" &&
					value != "0")
				{
					// Condition is true, keep the content
					result = result.Replace(match.Value, content);
				}
				else
				{
					// Condition is false, remove the block
					result = result.Replace(match.Value, string.Empty);
				}
			}

			return result;
		}

		private static string HandleArrayPlaceholders(string template, Dictionary<string, string> placeholders)
		{
			var result = template;

			// Handle array loops: {{#each Products}}...{{/each}}
			var eachRegex = new Regex(@"\{\{\s*#each\s+(\w+)\s*\}\}(.*?)\{\{\s*/each\s*\}\}", RegexOptions.Singleline);

			foreach (Match match in eachRegex.Matches(result))
			{
				var arrayName = match.Groups[1].Value;
				var loopContent = match.Groups[2].Value;

				// Find all items in this array
				var arrayItems = new List<Dictionary<string, string>>();
				var index = 0;

				while (true)
				{
					var itemKey = $"{arrayName}[{index}].";
					var itemPlaceholders = placeholders
						.Where(p => p.Key.StartsWith(itemKey))
						.ToDictionary(p => p.Key.Substring(itemKey.Length), p => p.Value);

					if (!itemPlaceholders.Any())
						break;

					itemPlaceholders["IncrementIndex"] = (index + 1).ToString();
					arrayItems.Add(itemPlaceholders);
					index++;
				}

				// Generate content for each item
				var generatedContent = new StringBuilder();
				foreach (var item in arrayItems)
				{
					var itemContent = loopContent;
					foreach (var placeholder in item)
					{
						var pattern = $"{{{{{placeholder.Key}}}}}";
						itemContent = itemContent.Replace(pattern, placeholder.Value ?? string.Empty);
					}
					generatedContent.Append(itemContent);
				}

				result = result.Replace(match.Value, generatedContent.ToString());
			}

			return result;
		}
		public static Dictionary<string, string> BuildWelcomePlaceholders(
			string customerName,
			string customerEmail,
			EmailSettings settings)
		{
			return new Dictionary<string, string>
			{
				["CustomerName"] = customerName,
				["CustomerEmail"] = customerEmail,
				["StoreName"] = settings.StoreName,
				["WebsiteUrl"] = settings.WebsiteUrl,
				["SupportUrl"] = settings.SupportUrl,
				["CurrentYear"] = DateTime.Now.Year.ToString(),
				["JoinDate"] = DateTime.Now.ToString("MMMM dd, yyyy"),
				["DashboardUrl"] = $"{settings.WebsiteUrl}/dashboard"
			};
		}

		public static Dictionary<string, string> BuildOrderPlaceholders(
			OrderTestModel order,
			EmailSettings settings)
		{
			var orderItemsHtml = "<ul style='margin-left: 20px;'>";
			foreach (var item in order.Items)
			{
				orderItemsHtml += $@"
                <li style='margin-bottom: 10px; padding: 10px; background: #f8f9fa; border-radius: 5px;'>
                    <strong>{item.Name}</strong><br>
                    Quantity: {item.Quantity} × ${item.Price:F2} = ${item.Quantity * item.Price:F2}
                </li>";
			}
			orderItemsHtml += "</ul>";

			return new Dictionary<string, string>
			{
				["CustomerName"] = order.CustomerName,
				["OrderNumber"] = order.OrderNumber,
				["OrderDate"] = order.OrderDate.ToString("MMMM dd, yyyy hh:mm tt"),
				["OrderStatus"] = order.OrderStatus,
				["TotalAmount"] = order.TotalAmount.ToString("F2"),
				["ShippingAddress"] = order.ShippingAddress,
				["OrderItems"] = orderItemsHtml,
				["StoreName"] = settings.StoreName,
				["WebsiteUrl"] = settings.WebsiteUrl,
				["SupportUrl"] = settings.SupportUrl,
				["CurrentYear"] = DateTime.Now.Year.ToString(),
				["OrderTrackingUrl"] = $"{settings.WebsiteUrl}/orders/{order.OrderNumber}"
			};
		}

		public static Dictionary<string, string> BuildClaimPlaceholders(
			ClaimTestModel claim,
			EmailSettings settings)
		{
			return new Dictionary<string, string>
			{
				["CustomerName"] = claim.CustomerName,
				["ClaimId"] = claim.ClaimId,
				["ClaimDate"] = claim.ClaimDate.ToString("MMMM dd, yyyy"),
				["ClaimStatus"] = claim.ClaimStatus,
				["ClaimType"] = claim.ClaimType,
				["OrderNumber"] = claim.OrderNumber,
				["ClaimDescription"] = claim.ClaimDescription,
				["NextSteps"] = claim.NextSteps,
				["ProcessingTime"] = claim.ProcessingTime,
				["StoreName"] = settings.StoreName,
				["WebsiteUrl"] = settings.WebsiteUrl,
				["SupportUrl"] = settings.SupportUrl,
				["CurrentYear"] = DateTime.Now.Year.ToString(),
				["ClaimTrackingUrl"] = $"{settings.WebsiteUrl}/claims/{claim.ClaimId}"
			};
		}
	}
}
