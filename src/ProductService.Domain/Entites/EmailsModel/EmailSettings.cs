using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.Domain.Entites.EmailsModel
{
	public class EmailSettings
	{
		public string SmtpServer { get; set; } =string.Empty;
		public int Port { get; set; } = 587;
		public string SenderName { get; set; } = string.Empty;
		public string SenderEmail { get; set; } = string.Empty;
		public string Username { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
		public bool EnableSsl { get; set; } = true;

		// Store info for templates
		public string StoreName { get; set; } = string.Empty;
		public string WebsiteUrl { get; set; } = string.Empty;
		public string SupportUrl { get; set; } = string.Empty;
		public string SupportEmail { get; set; } = string.Empty;
	}
}
