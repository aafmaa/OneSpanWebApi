using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using OneSpanWebApi.Models;
using OneSpanWebApi.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneSpanWebApi.Tests
{
    public class IasServiceTests
    {
        [Fact]
        public void DesignationStatusUpdate_ReturnsExpectedResponse()
        {
            // Arrange  
            var mockOptions = new Mock<IOptions<IasClientConfig>>();
            mockOptions.Setup(o => o.Value).Returns(new IasClientConfig
            {
                Uri = new Uri("http://rh-test.aafmaa.local:7777"),
                Environment = "PARM=NAT227 etid=$$ bp=WEBBP",
                Library = "NATSERVJ"
            });

            var logger = NullLogger<IasService>.Instance;

            // Create a partial mock to override NatServJCall  
            var serviceMock = new Mock<IasService>(mockOptions.Object, logger) { CallBase = true };
            var expectedResponse = new StringBuilder("mocked response");

            serviceMock
                .Setup(s => s.NatServJCall(
                    It.Is<string>(p => p == "FinalizeDesignation"),
                    It.IsAny<string>(),
                    out expectedResponse))
                .Callback((string p, string req, out StringBuilder resp) =>
                {
                    resp = expectedResponse;
                });

            // Act  
            var result = serviceMock.Object.DesignationStatusUpdate(12345);

            // Assert  
            Assert.Equal(expectedResponse.ToString(), result.ToString());
        }
    }
}
