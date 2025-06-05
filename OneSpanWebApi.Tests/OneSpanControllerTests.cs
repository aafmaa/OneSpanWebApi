using Microsoft.VisualStudio.TestPlatform.TestHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
//using Microsoft.AspNetCore.Mvc.Testing;

namespace OneSpanWebApi.Tests
{
    public class OneSpanControllerTests 
    {
        private readonly HttpClient _client; // Add this field to define _client

        public OneSpanControllerTests()
        {
            // Initialize _client with a test server or mock HttpClient
            //var factory = new WebApplicationFactory<Program>(); // Assuming Program is the entry point of your application
            //_client = factory.CreateClient();
        }

        [Fact]
        public async Task CancelPackage_ReturnsOk()
        {
            // Arrange
            var designationId = "123"; // Use a test or mock designationId

            // Act
            var response = await _client.PostAsync($"/Signature/cancel/{designationId}", null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Package canceled", content);
        }
    }
}
