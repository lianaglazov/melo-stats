namespace MeloStatsTest
{
    using Microsoft.AspNetCore.Mvc;
    using Moq;
    using System.Threading.Tasks;
    using Xunit;
    using MeloStats.Controllers;
    using MeloStats.Models;
    using MeloStats.Services;
    using Microsoft.AspNetCore.Identity;
    using System.Security.Claims;
    using System.Collections.Generic;
    using System.Linq;

    public class TracksControllerTest
    {
        private Mock<SpotifyApiService> _spotifyApiServiceMock;
        private Mock<UserManager<ApplicationUser>> _userManagerMock;
        private TracksController _controller;

        public TracksControllerTest()
        {
            _spotifyApiServiceMock = new Mock<SpotifyApiService>(null, null, null, null, null);
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);

            _controller = new TracksController(_spotifyApiServiceMock.Object, _userManagerMock.Object);
        }

        [Fact]
        public async Task TopTracks_UserNotLoggedIn_RedirectsToLogin()
        {
            _userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync((ApplicationUser)null);

            var result = await _controller.TopTracks();

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Equal("Users", redirectResult.ControllerName);
        }

        [Fact]
        public async Task TopTracks_UserLoggedIn_ReturnsViewWithTopTracks()
        {
            var user = new ApplicationUser { Id = "test-user-id" };
            _userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            var topTracks = new List<Track>
        {
            new Track { Name = "Track 1" },
            new Track { Name = "Track 2" }
        };

            _spotifyApiServiceMock.Setup(s => s.GetTopTracksAsync(user, "medium_term"))
                .ReturnsAsync(topTracks);

            var result = await _controller.TopTracks();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Track>>(viewResult.ViewData.Model);
            Assert.Equal(2, model.Count());
        }
    }

}