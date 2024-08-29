using MeloStats.Data;
using MeloStats.Models;
using MeloStats.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using MeloStats.Controllers;
using System.Security.Claims;
using System.Web.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace MeloStatsTest
{
    public class ListeningHistoriesControllerTest
    {
        [Fact]
        public async Task Stats()
        {
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);

            var mockSpotifyApiService = new Mock<SpotifyApiService>(null, null, null, null, null, null);
            var mockContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            var mockContext = new ApplicationDbContext(mockContextOptions);
            var user = new ApplicationUser { Id = "user-id" };

            mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            //mockSpotifyApiService.Setup(s => s.GetRecentlyPlayedTracksAsync(user)).Returns((Task<List<ListeningHistory>>)Task.CompletedTask);
            mockSpotifyApiService.Setup(s => s.GetTrackFeaturesAsync(It.IsAny<int>())).ReturnsAsync(new Feature
            {
                Danceability = 0.5F,
                Energy = 0.8F,
                Tempo = 120.0F,
                Valence = 0.6F,
                Instrumentalness = 0.02F
            });

            var sampleTrack = new Track
            {
                Id = 1,
                SpotifyTrackId = "track-id",
                Name = "sample-track",
                Duration = 200,
                Popularity = 50,
                Language = "EN"
            };

            var listeningHistory = new ListeningHistory
            {
                UserId = user.Id,
                TrackId = sampleTrack.Id
            };

            await mockContext.Tracks.AddAsync(sampleTrack);
            await mockContext.ListeningHistories.AddAsync(listeningHistory);
            await mockContext.SaveChangesAsync();

            var controller = new ListeningHistoriesController(mockContext, mockUserManager.Object, mockSpotifyApiService.Object,null);

            var result = await controller.Stats();

            var viewResult = Assert.IsType<Microsoft.AspNetCore.Mvc.ViewResult>(result);
            Assert.Equal("Stats", viewResult.ViewName);
            Assert.Equal(50, Convert.ToInt32(viewResult.ViewData["Popularity"])); 
            Assert.NotNull(viewResult.ViewData["Languages"]);
            Assert.NotNull(viewResult.ViewData["Danceability"]);
            Assert.NotNull(viewResult.ViewData["Energy"]);
            Assert.NotNull(viewResult.ViewData["Tempo"]);
            Assert.NotNull(viewResult.ViewData["Valence"]);
            Assert.NotNull(viewResult.ViewData["Instrumentalness"]);

            mockSpotifyApiService.Verify(s => s.GetRecentlyPlayedTracksAsync(user), Times.Once);
            mockSpotifyApiService.Verify(s => s.GetTrackFeaturesAsync(It.IsAny<int>()), Times.Once);
        }

    }
}
