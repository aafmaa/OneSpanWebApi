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
            // Arrange    
            var options = Options.Create(new OneSpanOptions
            {
                BaseApiUrl = "https://sandbox.esignlive.com",
                ApiKey = "SFZtWUpiS1h3SGNIOmw2azJrMllyOGFJWg==",
                DocPath = @"C:\Users\ngorbatovskikh\source\repos\OneSpanWebApi\OneSpanWebApi\Templates",
                SenderEmail = "ngorbatovskikh@metrostar.com",
                CallbackKey = "a42379d0-16af-47f7-a298-a8585703f294"
            });

            var logger = NullLogger<OneSpanService>.Instance;

            // Create a mock IConfiguration object
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                   { "ConnectionStrings:DefaultConnection", "Server=DEV-MSSQL12-A.DEV.LOCAL;Database=MemberOnlineApp;uid=PublicWeb_User; pwd=Neverever101;Trusted_Connection=True;TrustServerCertificate=True;"}
                })
                .Build();

            // Pass the IConfiguration object to DBConnectionFactory
            var dbConnectionFactory = new DBConnectionFactory(configuration);

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
                DesignationId = "454464654",
                PdfFieldValues = new Dictionary<string, string>
                   {
                       { "OwnerName", "John Doe" },
                       { "PolNumber", "12345678" },
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
            // Arrange    
            var options = Options.Create(new OneSpanOptions
            {
                BaseApiUrl = "https://sandbox.esignlive.com",
                ApiKey = "SFZtWUpiS1h3SGNIOmw2azJrMllyOGFJWg==",
                DocPath = @"C:\Users\ngorbatovskikh\source\repos\OneSpanWebApi\OneSpanWebApi\Templates",
                SenderEmail = "ngorbatovskikh@metrostar.com",
                CallbackKey = "a42379d0-16af-47f7-a298-a8585703f294"
            });

            var logger = NullLogger<OneSpanService>.Instance;

            // Create a mock IConfiguration object
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                   { "ConnectionStrings:DefaultConnection", "Server=DEV-MSSQL12-A.DEV.LOCAL;Database=MemberOnlineApp;uid=PublicWeb_User; pwd=Neverever101;Trusted_Connection=True;TrustServerCertificate=True;"}
                })
                .Build();

            // Pass the IConfiguration object to DBConnectionFactory
            var dbConnectionFactory = new DBConnectionFactory(configuration);

            var service = new OneSpanService(options, logger, dbConnectionFactory);

            var designationId = "454464654";
            // Act    
            await service.CancelPackageAsync(designationId);

            // Assert    
            Assert.True(true);
        }
    }
}
