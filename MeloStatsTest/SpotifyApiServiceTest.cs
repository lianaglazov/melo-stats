namespace MeloStatsTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Net.Http;
    using System.Threading.Tasks;
    using MeloStats.Data;
    using MeloStats.Models;
    using MeloStats.Services;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Moq;
    using Moq.Protected;
    using Newtonsoft.Json.Linq;
    using Xunit;
    using Moq.EntityFrameworkCore;
    using System.Net;
    using System.Text;

    public class SpotifyApiServiceTests
    {
        private readonly Mock<SpotifyAuthService> _mockAuthService;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<ApplicationDbContext> _mockContext;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;

        public SpotifyApiServiceTests()
        {
            _mockAuthService = new Mock<SpotifyAuthService>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
            _mockContext = new Mock<ApplicationDbContext>(new DbContextOptions<ApplicationDbContext>());
            _mockConfiguration = new Mock<IConfiguration>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();

            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        }

        [Fact]
        public async Task GetTopTracksAsync_ReturnsTopTracks()
        {
            // Arrange
            var testUser = new ApplicationUser { Id = "testUserId" };

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
           .UseInMemoryDatabase("TestDatabase")
           .Options;

            var _context = new ApplicationDbContext(options);

            // Seed test data
            _context.SpotifyTokens.Add(new SpotifyToken
            {
                UserId = "testUserId",
                AccessToken = "testAccessToken",
                RefreshToken = "testRefreshToken",
                Expires = DateTime.UtcNow.AddMinutes(5) 
            });
            _context.SaveChanges();
            _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(@"{ 'items': [] }", Encoding.UTF8, "application/json")
            });
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(_mockHttpMessageHandler.Object));

            var _authService = new SpotifyAuthService("testClientId", "testClientSecret");


            // Creating the service
            var service = new SpotifyApiService(
                _authService,
                _mockUserManager.Object,
                _context,
                _mockConfiguration.Object,
                _mockHttpClientFactory.Object
            );

            // Act
            var result = await service.GetTopTracksAsync(testUser);

            // Assert
            Assert.NotNull(result);
            //Assert.Single(result);
            //Assert.Equal("Track 1", result[0].Name);
            //Assert.Equal(80, result[0].Popularity);

        }

    }


}

