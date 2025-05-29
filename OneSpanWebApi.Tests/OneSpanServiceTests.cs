using Microsoft.Extensions.Options;
using OneSpanWebApi.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;

namespace OneSpanWebApi.Tests
{
    public class OneSpanServiceTests
    {
        [Fact]
        public void GetSignature_ReturnsPackageIdString()
        {
            // Arrange
            var options = Options.Create(new OneSpanOptions
            {
                BaseApiUrl = "https://sandbox.esignlive.com",
                ApiKey = "SFZtWUpiS1h3SGNIOmw2azJrMllyOGFJWg==",
                DocPath = @"C:\Users\ngorbatovskikh\source\repos\OneSpanApiService\OneSpanApiService\Docs"
            });

            var logger = NullLogger<OneSpanService>.Instance;
            var service = new OneSpanService(options, logger);

            // Act
            var result = service.GetSignature();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(result));
        }
    }
}
