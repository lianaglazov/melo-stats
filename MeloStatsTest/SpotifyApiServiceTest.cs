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
            var testUser = new ApplicationUser { Id = "testUserId" };
            var accessToken = "testAccessToken";

            var data = new List<SpotifyToken>
            {
                new SpotifyToken
                {
                    AccessToken = "testAccessToken",
                    Expires = DateTime.UtcNow.AddMinutes(5),
                    UserId = "testUserId"
                }
            }.AsQueryable();

            var mockDbSet = new Mock<DbSet<SpotifyToken>>();
            mockDbSet.As<IQueryable<SpotifyToken>>().Setup(m => m.Provider).Returns(data.Provider);
            mockDbSet.As<IQueryable<SpotifyToken>>().Setup(m => m.Expression).Returns(data.Expression);
            mockDbSet.As<IQueryable<SpotifyToken>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockDbSet.As<IQueryable<SpotifyToken>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

            mockDbSet.Setup(m => m.FirstOrDefaultAsync(It.IsAny<Expression<Func<SpotifyToken, bool>>>(), default))
                .ReturnsAsync(data.FirstOrDefault());

            var mockContext = new Mock<ApplicationDbContext>();
            mockContext.Setup(c => c.SpotifyTokens).Returns(mockDbSet.Object);


            var httpResponse = new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(@"{
                'items': [
                    {
                        'id': 'track1',
                        'name': 'Track 1',
                        'duration_ms': 210000,
                        'popularity': 80,
                        'album': {
                            'id': 'album1',
                            'name': 'Album 1',
                            'release_date': '2022-01-01',
                            'images': [{ 'url': 'http://image.url/album1.jpg' }]
                        },
                        'artists': [
                            {
                                'id': 'artist1',
                                'name': 'Artist 1'
                            }
                        ]
                    }
                ]
            }")
            };

            var mockHttpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(mockHttpClient);

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    It.IsAny<HttpRequestMessage>(),
                    It.IsAny<CancellationToken>()
                )
                .ReturnsAsync(httpResponse);

            var service = new SpotifyApiService(
                _mockAuthService.Object,
                _mockUserManager.Object,
                _mockContext.Object,
                _mockConfiguration.Object,
                _mockHttpClientFactory.Object
            );

            var result = await service.GetTopTracksAsync(testUser);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Track 1", result[0].Name);
            Assert.Equal(80, result[0].Popularity);
        }

    }
}
