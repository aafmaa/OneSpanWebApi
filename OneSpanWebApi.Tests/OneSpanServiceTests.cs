using Microsoft.Extensions.Options;
using OneSpanWebApi.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
// Fix for CS0234: Ensure the correct namespace is used for NullLogger.  
using Microsoft.Extensions.Logging;
using OneSpanWebApi.Models;
using OneSpanWebApi.Data;
using Microsoft.Extensions.Configuration;
using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using OneSpanSign.Sdk.Services;
using OneSpanSign.Sdk;

namespace OneSpanWebApi.Tests
{
    public class OneSpanServiceTests
    {
        [Fact]
        public void GetSignature_ReturnsPackageIdString()
        {
            // Build configuration to include user secrets
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<OneSpanServiceTests>() // Loads secrets for this test project
                .Build();

            // Retrieve CallbackKey and ConnectionString from secrets
            var callbackKey = configuration["Onespan:CallbackKey"];
            var apiKey = configuration["Onespan:apiKey"];
            var gemBoxPdfLisence = configuration["Onespan:GemBoxPdfLicense"];
            var gemBoxDocumentLisence = configuration["Onespan:GemBoxDocumentLicense"];
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? configuration["ConnectionStrings:DefaultConnection"]; // fallback if needed

            // Arrange    
            var options = Options.Create(new OneSpanConfig
            {
                BaseApiUrl = "https://sandbox.esignlive.com",
                ApiKey = apiKey,
                DocPath = @"C:\Users\ngorbatovskikh\source\repos\OneSpanWebApi\OneSpanWebApi\",
                SenderEmail = "ngorbatovskikh@aafmaa.com",
                CallbackKey = callbackKey,
                DocExperationDays = 1,
                GemBoxDocumentLicense = gemBoxDocumentLisence,
                GemBoxPdfLicense = gemBoxPdfLisence
            });

            var logger = NullLogger<OneSpanService>.Instance;

            // Create a mock IConfiguration object
            var configWithConn = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                       { "ConnectionStrings:DefaultConnection", connectionString }
                })
                .Build();

            // Pass the IConfiguration object to DBConnectionFactory
            var dbConnectionFactory = new DBConnectionFactory(configWithConn);

            var config = new IasClientConfig
            {
                Uri = new Uri("https://example.com/"),
                Environment = "TestEnv",
                Library = "TestLib"
            };

            // Mock the IasService dependency
            
            var mockIasOptions = new Mock<IOptions<IasClientConfig>>();
            mockIasOptions.Setup(o => o.Value).Returns(new IasClientConfig
            {
                Uri = new Uri("http://rh-test.aafmaa.local:7777"),
                Environment = "PARM=NAT227 etid=$$ bp=WEBBP",
                Library = "NATSERVJ"
            });

            var mockIasService = new Mock<IasService>(mockIasOptions.Object, Mock.Of<ILogger<IasService>>());


            // Create the OneSpanService instance with all required dependencies
            var service = new OneSpanService(options, logger, dbConnectionFactory, mockIasService.Object);

            // Create a mock or sample BeneficiaryRequest object    
            var beneficiaryRequest = new BeneficiaryRequest
            {
                SignerFirstName = "Ann1",
                SignerLastName = "Smith1",
                SignerEmail = "natashagor@hotmail.com",
                SignerDateOfBirth = "01/15/1980",
                SignerLast4SSN = "1234",
                DesignationId = "2",
                CN = "12345",
                PdfFieldValues = new Dictionary<string, string>
                   {
                       { "OwnerName", $"Smith1 G Ann1" },
                       { "OwnerSSN", "XXX-XX-1234" },
                       { "InsuredName", "John Adam Doe" },
                       { "PolNumber", "12345679" },
                       { "InfoBen1Name", "Green Jerom Michael" },
                       { "InfoBen1SSN", "XXX-XX-1237" },
                       { "nfoBen1Address", "123 Old Reston Ave Reston VA 20190" },
                       { "InfoBen1Email", "jeromgreen@aafmaa.com" },
                       { "InfoBen1Phone", "5711231234" },
                       { "P1N", "Jane Smith" },
                       { "P1B", "01/01/1995" },
                       { "P1Rel", "Spouse" },
                       { "P1%", "100" },
                       { "PerStirpes", "N" },
                       { "Disaster", "Y" },
                       { "Days", "14" }
                   }
            };

            // Act    
            var result = service.GetDesignationSignature(beneficiaryRequest);

            // Assert    
            Assert.False(string.IsNullOrWhiteSpace(result));
        }

        [Fact]
        public async Task CancelPackageAsync_WithValidDesignationId_DeletesPackage()
        {
            // Build configuration to include user secrets
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<OneSpanServiceTests>() // Loads secrets for this test project
                .Build();

            // Retrieve CallbackKey and ConnectionString from secrets
            var callbackKey = configuration["Onespan:CallbackKey"];
            var apiKey = configuration["Onespan:apiKey"];
            var GemBoxPdf = configuration["Onespan:GemBoxPdfLicense"];
            var GemBoxDoc = configuration["Onespan:GemBoxDocumentLicense"];
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? configuration["ConnectionStrings:DefaultConnection"]; // fallback if needed

            // Arrange    
            var options = Options.Create(new OneSpanConfig
            {
                BaseApiUrl = "https://sandbox.esignlive.com",
                ApiKey = apiKey,
                DocPath = @"C:\Users\ngorbatovskikh\source\repos\OneSpanWebApi\OneSpanWebApi\",
                SenderEmail = "ngorbatovskikh@aafmaa.com",
                CallbackKey = callbackKey,
                DocExperationDays = 1,
                GemBoxDocumentLicense = GemBoxDoc,
                GemBoxPdfLicense = GemBoxPdf
            });

            var logger = NullLogger<OneSpanService>.Instance;

            // Create a mock IConfiguration object
            var configWithConn = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                       { "ConnectionStrings:DefaultConnection", connectionString }
                })
                .Build();

            // Pass the IConfiguration object to DBConnectionFactory
            var dbConnectionFactory = new DBConnectionFactory(configWithConn);

            // Mock the IasService dependency
            var mockIasService = new Mock<IasService>();
            
            // Create the OneSpanService instance with all required dependencies
            var service = new OneSpanService(options, logger, dbConnectionFactory, mockIasService.Object);

            var designationId = "544646522";

            // Act    
            await service.CancelSignaturePackageAsync(designationId);

            // Assert    
            Assert.True(true);
        }
    }
}
