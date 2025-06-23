using Microsoft.AspNetCore.Http.Extensions;
using Serilog;
using System.Net;
using System.Text;

namespace OneSpanWebApi.Models
{
    public class IasClientConfig
    {
        public required Uri Uri { get; set; }
        public required string Environment { get; set; }
        public required string Library { get; set; }
    }
}
