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
    using RichardSzalay.MockHttp;
    using MeloStats.Data.Migrations;

    public class SpotifyApiServiceTests
    {
        [Fact]
        public async Task FetchWebApi()
        {
            var mockHttp = new MockHttpMessageHandler();

            mockHttp.When("https://api.spotify.com/v1/me/top/tracks")
                    .Respond("application/json", "{'success': true}"); 

            var client = mockHttp.ToHttpClient();

            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null
            );

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

            var mockDbContext = new ApplicationDbContext(options);

            var sampleToken = new SpotifyToken
            {
                UserId = "test-user-id",
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                Expires = DateTime.UtcNow.AddMinutes(10) 
            };
            await mockDbContext.SpotifyTokens.AddAsync(sampleToken);
            await mockDbContext.SaveChangesAsync();

            var mockConfiguration = new Mock<IConfiguration>();
            var mockAuthService = new Mock<SpotifyAuthService>(null, null);
            var mockGeniusApiService = new Mock<GeniusApiService>(client);

            var spotifyApiService = new SpotifyApiService(
                mockAuthService.Object,
                mockUserManager.Object,
                mockDbContext,
                mockConfiguration.Object,
                mockHttpClientFactory.Object,
                mockGeniusApiService.Object
            );

            var user = new ApplicationUser { Id = "test-user-id" };
            string endpoint = "v1/me/top/tracks";
            HttpMethod method = HttpMethod.Get;

            var result = await spotifyApiService.FetchWebApi(user, endpoint, method);

            Assert.NotNull(result);
            Assert.True(result["success"].Value<bool>());
        }

        [Fact]
        public async Task GetTopTracksAsync()
        {
            var mockHttp = new MockHttpMessageHandler();

           
            mockHttp.When("https://api.spotify.com/v1/me/top/tracks*")
                    .Respond("application/json", @"{
                'items': [
                    {
                        'id': 'track123',
                        'name': 'Sample Track',
                        'duration_ms': 300000,
                        'popularity': 80,
                        'album': {
                            'id': 'album123',
                            'name': 'Sample Album',
                            'release_date': '2022-01-01',
                            'images': [{'url': 'http://example.com/album.jpg'}]
                        },
                        'artists': [
                            {
                                'id': 'artist123',
                                'name': 'Sample Artist'
                            }
                        ]
                    }
                ]
            }");

            mockHttp.When("https://api.spotify.com/v1/artists/artist123")
                    .Respond("application/json", @"{
                'id': 'artist123',
                'name': 'Sample Artist',
                'popularity': 90,
                'genres': ['pop', 'rock'],
                'images': [{'url': 'http://example.com/artist.jpg'}]
            }");

            var client = mockHttp.ToHttpClient();
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);

            var mockConfiguration = new Mock<IConfiguration>();
            var mockAuthService = new Mock<SpotifyAuthService>(null, null);

            var mockGeniusApiService = new Mock<GeniusApiService>(client);
            mockGeniusApiService.Setup(s => s.GetSongLanguageAsync(It.IsAny<string>(), It.IsAny<string>()))
                                .ReturnsAsync("English");

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            using var context = new ApplicationDbContext(options);

            var sampleToken = new SpotifyToken
            {
                UserId = "test-user-id",
                AccessToken = "access-token",
                RefreshToken = "refresh-token",
                Expires = DateTime.UtcNow.AddMinutes(10)
            };
            await context.SpotifyTokens.AddAsync(sampleToken);
            await context.SaveChangesAsync();

            var spotifyApiService = new SpotifyApiService(
                mockAuthService.Object,
                mockUserManager.Object,
                context,
                mockConfiguration.Object,
                mockHttpClientFactory.Object,
                mockGeniusApiService.Object
            );

            var user = new ApplicationUser { Id = "test-user-id" };

            var result = await spotifyApiService.GetTopTracksAsync(user);

            Assert.NotNull(result);
            Assert.Equal(1, result.Count);
            Assert.Equal("track123", result[0].SpotifyTrackId);
            Assert.Equal("Sample Track", result[0].Name);
            Assert.Equal(300, result[0].Duration);
            Assert.Equal(80, result[0].Popularity);
            Assert.Equal("English", result[0].Language);

            var artist = await context.Artists.FirstOrDefaultAsync(a => a.SpotifyArtistId == "artist123");
            Assert.NotNull(artist);
            Assert.Equal("Sample Artist", artist.Name);
            Assert.Equal(90, artist.Popularity);

            var album = await context.Albums.FirstOrDefaultAsync(a => a.SpotifyAlbumId == "album123");
            Assert.NotNull(album);
            Assert.Equal("Sample Album", album.Name);
            Assert.Equal("http://example.com/album.jpg", album.ImageUrl);
        }
       

    }


}

