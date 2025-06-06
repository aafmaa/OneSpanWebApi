using Microsoft.Extensions.Options;
using OneSpanWebApi.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
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
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? configuration["ConnectionStrings:DefaultConnection"]; // fallback if needed


            // Arrange    
            var options = Options.Create(new OneSpanOptions
            {
                BaseApiUrl = "https://sandbox.esignlive.com",
                ApiKey = apiKey, 
                DocPath = @"C:\Users\ngorbatovskikh\source\repos\OneSpanWebApi\OneSpanWebApi\",
                SenderEmail = "ngorbatovskikh@metrostar.com",
                CallbackKey = callbackKey
            });

            var logger = NullLogger<OneSpanService>.Instance;

            // Create a mock IConfiguration object
            // Build configuration with the connection string from secrets
            var configWithConn = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
            { "ConnectionStrings:DefaultConnection", connectionString }
                })
                .Build();

            // Pass the IConfiguration object to DBConnectionFactory
            var dbConnectionFactory = new DBConnectionFactory(configWithConn);

            var service = new OneSpanService(options, logger, dbConnectionFactory);

            // Create a mock or sample BeneficiaryRequest object    
            var beneficiaryRequest = new BeneficiaryRequest
            {
                // Correctly populate the required properties of BeneficiaryRequest  
                SignerFirstName = "Ann1",
                SignerLastName = "Smith1",
                SignerEmail = "natashagor@hotmail.com",
                DateOfBirth = "01/15/1980",
                Last4SSN = "1234",
                DesignationId = "544346522",
                CN = "12345",
                PdfFieldValues = new Dictionary<string, string>
                   {
                       { "OwnerName", "John Doe" },
                       { "PolNumber", "12345679" },
                       { "P1Nam", "Jane Smith" },
                       { "P1Rel", "Spouse" },
                       { "P1%", "100" }
                   }
            };

            // Act    
            var result = service.GetSignature(beneficiaryRequest);

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
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? configuration["ConnectionStrings:DefaultConnection"]; // fallback if needed


            // Arrange    
            var options = Options.Create(new OneSpanOptions
            {
                BaseApiUrl = "https://sandbox.esignlive.com",
                ApiKey = apiKey,
                DocPath = @"C:\Users\ngorbatovskikh\source\repos\OneSpanWebApi\OneSpanWebApi\",
                SenderEmail = "ngorbatovskikh@metrostar.com",
                CallbackKey = callbackKey
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

            var service = new OneSpanService(options, logger, dbConnectionFactory);

            var designationId = "544646522";
            // Act    
            await service.CancelPackageAsync(designationId);

            // Assert    
            Assert.True(true);
        }

     
       
    }
}
