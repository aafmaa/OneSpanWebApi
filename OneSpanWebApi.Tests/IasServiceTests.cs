﻿using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using OneSpanWebApi.Models;
using OneSpanWebApi.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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

            //var logger = NullLogger<IasService>.Instance;
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Debug); // or LogLevel.Trace for even more detail
            });
            var logger = loggerFactory.CreateLogger<IasService>();


            // Create a partial mock to override NatServJCall  
            var serviceMock = new Mock<IasService>(mockOptions.Object, logger) { CallBase = true };
            //var expectedResponse = new StringBuilder("mocked response");

            // Act
            var result = serviceMock.Object.DesignationStatusUpdate(12345, DesignationStatus.Final);

            // Assert  
            Assert.True(!string.IsNullOrEmpty(result.ToString()));
        }
    }
}
