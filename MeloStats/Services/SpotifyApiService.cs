namespace MeloStats.Services
{
    using MeloStats.Data;
    using MeloStats.Models;
    using Microsoft.AspNetCore.Identity;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;
    using System.Text;
    using Azure.Core;
    using MeloStats.Data.Migrations;

    public class SpotifyApiService
    {
        private static readonly string BaseUrl = "https://api.spotify.com/v1/";
        private readonly SpotifyAuthService _authService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public SpotifyApiService(SpotifyAuthService authService, UserManager<ApplicationUser> userManager, ApplicationDbContext context, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _authService = authService;
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<TrackInfo> GetTrackAsync(string trackId)
        {
            var accessToken = await _authService.GetAccessTokenAsync();

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(BaseUrl);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await client.GetAsync($"tracks/{trackId}");
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(responseBody);

                var trackInfo = new TrackInfo
                {
                    Name = json["name"].ToString(),
                    Artist = json["artists"][0]["name"].ToString(),
                    Album = json["album"]["name"].ToString()
                };

                return trackInfo;
            }
        }

        private async Task<JObject> FetchWebApi(ApplicationUser user, string endpoint, HttpMethod method, object body = null)
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(method, $"https://api.spotify.com/{endpoint}");

            var userToken = await _context.SpotifyTokens.FirstOrDefaultAsync(t => t.UserId == user.Id);

            var accessToken = await RefreshAccessToken(userToken.RefreshToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            if (body != null)
            {
                var json = JsonConvert.SerializeObject(body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var response = client.Send(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error fetching data from Spotify API: {response.StatusCode}, {errorContent}");
            }
            //_context.SaveChanges();
            var responseContent = await response.Content.ReadAsStringAsync();
            return JObject.Parse(responseContent);
        }

        public async Task<List<Track>> GetTopTracksAsync(ApplicationUser user, string timeRange = "medium_term")
        {
            var endpoint = $"v1/me/top/tracks?time_range={timeRange}&limit=20";

            var response = await FetchWebApi(user, endpoint, HttpMethod.Get);
            var items = response["items"];
            var topTracks = new List<Track>();

            foreach (var item in items)
            {
                var spotifyTrackId = item["id"].ToString();
                var trackName = item["name"].ToString();
                var duration = int.Parse(item["duration_ms"].ToString()) / 1000; // Convert from milliseconds to seconds
                var popularity = int.Parse(item["popularity"].ToString());

                var albumItem = item["album"];
                var spotifyAlbumId = albumItem["id"].ToString();
                var albumName = albumItem["name"].ToString();
                var releaseDateString = albumItem["release_date"].ToString();
                var albumImg = albumItem["images"].FirstOrDefault()?["url"]?.ToString();

                DateTime releaseDate;
                // sometimes we don't have the exact release date - will set on the Jan 1st of that year
                if (!DateTime.TryParseExact(releaseDateString, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out releaseDate))
                {
                    if (int.TryParse(releaseDateString, out int year))
                    {
                        releaseDate = new DateTime(year, 1, 1);
                    }
                    else
                    {
                        releaseDate = DateTime.Parse(releaseDateString);
                    }
                }

                var artistItem = item["artists"].First();
                var spotifyArtistId = artistItem["id"].ToString();
                var artistName = artistItem["name"].ToString();

                // api-ul pt artisti este apelat doar in cazul in care nu avem toate informatiile in db
                var artist = await _context.Artists.FirstOrDefaultAsync(a => a.SpotifyArtistId == spotifyArtistId);
                if (artist == null || artist.Popularity == 0)
                {
                    var artistEndpoint = $"v1/artists/{spotifyArtistId}";
                    var artistResponse = await FetchWebApi(user, artistEndpoint, HttpMethod.Get);

                    if (artistResponse == null)
                    {
                        throw new Exception($"Failed to fetch artist details for artist ID {spotifyArtistId}.");
                    }

                    var artistPopularity = int.Parse(artistResponse["popularity"].ToString());
                    var artistGenres = artistResponse["genres"].Select(g => g.ToString()).ToArray();
                    var artistImg = artistResponse["images"].FirstOrDefault()?["url"]?.ToString();
                    var genres = string.Join(", ", artistGenres);

                    // Add artist to the database if not exists

                    if (artist == null)
                    {
                        artist = new Artist
                        {
                            SpotifyArtistId = spotifyArtistId,
                            Name = artistName,
                            Popularity = artistPopularity,
                            Genres = genres,
                            ImageUrl = artistImg,
                            Tracks = new List<Track>(),
                            Albums = new List<Album>()
                        };
                        _context.Artists.Add(artist);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        // update genres popularity and image
                        artist.Genres = genres;
                        artist.Popularity = artistPopularity;
                        artist.ImageUrl = artistImg;
                        _context.Artists.Update(artist);
                        _context.SaveChanges();
                    }
                }

                // adding the albumin the db if it doesn't exists
                var album = await _context.Albums.FirstOrDefaultAsync(a => a.SpotifyAlbumId == spotifyAlbumId);
                if (album == null)
                {
                    album = new Album
                    {
                        SpotifyAlbumId = spotifyAlbumId,
                        Name = albumName,
                        ReleaseDate = releaseDate,
                        ArtistId = artist.Id,
                        ImageUrl = albumImg,
                        Tracks = new List<Track>()
                    };
                    _context.Albums.Add(album);
                    if (!artist.Albums.Any(t => t.SpotifyAlbumId == spotifyAlbumId))
                    {
                        artist.Albums.Add(album);
                    }
                    artist.Albums.Add(album);
                    _context.SaveChanges();
                }
                else
                {
                    // update the image of an album if it is different
                    if (album.ImageUrl != albumImg)
                    {
                        album.ImageUrl = albumImg;
                        _context.Albums.Update(album);
                        _context.SaveChanges();
                    }
                    
                }

                // add the track to the db
                var track = await _context.Tracks.FirstOrDefaultAsync(t => t.SpotifyTrackId == spotifyTrackId);
                if (track == null)
                {
                    track = new Track
                    {
                        SpotifyTrackId = spotifyTrackId,
                        Name = trackName,
                        Duration = duration,
                        Popularity = popularity,
                        ArtistId = artist.Id,
                        AlbumId = album.Id
                    };

                    _context.Tracks.Add(track);
                    _context.SaveChanges();
                }
                else
                {
                    //check if the popularity updated
                    if (track.Popularity != popularity)
                    {
                        track.Popularity = popularity;
                        _context.Tracks.Update(track);
                        _context.SaveChanges();
                    }
                }
                // add the track to the tracks list in album and artist tables

                if (!album.Tracks.Any(t => t.SpotifyTrackId == spotifyTrackId))
                {
                    album.Tracks.Add(track);
                }

                if (!artist.Tracks.Any(t => t.SpotifyTrackId == spotifyTrackId))
                {
                    artist.Tracks.Add(track);
                }

                topTracks.Add(track);
            }

            return topTracks;
        }
        public async Task<List<Artist>> GetTopArtistsAsync(ApplicationUser user, string timeRange = "medium_term")
        {
            var endpoint = $"v1/me/top/artists?time_range={timeRange}&limit=20";

            var response = await FetchWebApi(user, endpoint, HttpMethod.Get);
            var items = response["items"];
            var topArtists = new List<Artist>();

            foreach (var item in items)
            {
                var spotifyArtistId = item["id"].ToString();
                var artistName = item["name"].ToString();
                var imageUrl = item["images"].FirstOrDefault()?["url"]?.ToString();
                var popularity = int.Parse(item["popularity"].ToString());
                var artistGenres = item["genres"].Select(g => g.ToString()).ToArray();
                var genres = string.Join(", ", artistGenres);
                // adding the artist of the track in the db if it doesn't exists
                var artist = await _context.Artists.FirstOrDefaultAsync(a => a.SpotifyArtistId == spotifyArtistId);
                if (artist == null)
                {
                    artist = new Artist
                    {
                        SpotifyArtistId = spotifyArtistId,
                        Name = artistName,
                        ImageUrl = imageUrl,
                        Popularity = popularity,
                        Genres = genres,
                        Tracks = new List<Track>(),
                        Albums = new List<Album>()
                    };
                    _context.Artists.Add(artist);
                    _context.SaveChanges();
                }
                else
                {
                    // update the image, popularity and genres of an artist
                    artist.Popularity = popularity;
                    artist.Genres = genres;
                    artist.ImageUrl = imageUrl;
                    _context.Artists.Update(artist);
                    _context.SaveChanges();
                }
                topArtists.Add(artist);

            }

            return topArtists;
        }

        public async Task<List<ListeningHistory>> GetRecentlyPlayedTracksAsync(ApplicationUser user)
        {
            var endpoint = "v1/me/player/recently-played?limit=50";
            var response = await FetchWebApi(user, endpoint, HttpMethod.Get);

            var items = response["items"];
            var recentTracks = new List<ListeningHistory>();

            foreach (var item in items)
            {
                var spotifyTrackId = item["track"]["id"].ToString();
                var trackName = item["track"]["name"].ToString();
                var duration = int.Parse(item["track"]["duration_ms"].ToString()) / 1000;
                var playedAt = DateTime.Parse(item["played_at"].ToString());

                var albumItem = item["track"]["album"];
                var spotifyAlbumId = albumItem["id"].ToString();
                var albumName = albumItem["name"].ToString();
                var releaseDateString = albumItem["release_date"].ToString();

                DateTime releaseDate;
                // sometimes we don't have the exact release date - will set on the Jan 1st of that year
                if (!DateTime.TryParseExact(releaseDateString, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out releaseDate))
                {
                    if (int.TryParse(releaseDateString, out int year))
                    {
                        releaseDate = new DateTime(year, 1, 1);
                    }
                    else
                    {
                        releaseDate = DateTime.Parse(releaseDateString);
                    }
                }

                var artistItem = item["track"]["artists"].First();
                var spotifyArtistId = artistItem["id"].ToString();
                var artistName = artistItem["name"].ToString();
                var artist = await _context.Artists.FirstOrDefaultAsync(a => a.SpotifyArtistId == spotifyArtistId);
                // calling the api for the artist only if have missing information in the db
                if (artist == null || artist.Popularity == 0)
                {

                    var artistEndpoint = $"v1/artists/{spotifyArtistId}";
                    var artistResponse = await FetchWebApi(user, artistEndpoint, HttpMethod.Get);

                    if (artistResponse == null)
                    {
                        throw new Exception($"Failed to fetch artist details for artist ID {spotifyArtistId}.");
                    }

                    var artistPopularity = int.Parse(artistResponse["popularity"].ToString());
                    var artistGenres = artistResponse["genres"].Select(g => g.ToString()).ToArray();
                    var artistImg = artistResponse["images"].FirstOrDefault()?["url"]?.ToString();
                    var genres = string.Join(", ", artistGenres);

                    // Add artist to the database if not exists
                    if (artist == null)
                    {
                        artist = new Artist
                        {
                            SpotifyArtistId = spotifyArtistId,
                            Name = artistName,
                            Popularity = artistPopularity,
                            Genres = genres,
                            ImageUrl = artistImg,
                            Tracks = new List<Track>(),
                            Albums = new List<Album>()
                        };
                        _context.Artists.Add(artist);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        // update genres and popularity
                        artist.Genres = genres;
                        artist.Popularity = artistPopularity;
                        artist.ImageUrl = artistImg;
                        _context.Artists.Update(artist);
                        _context.SaveChanges();
                    }
                }

                // adding the albumin the db if it doesn't exists
                var album = await _context.Albums.FirstOrDefaultAsync(a => a.SpotifyAlbumId == spotifyAlbumId);
                if (album == null)
                {
                    album = new Album
                    {
                        SpotifyAlbumId = spotifyAlbumId,
                        Name = albumName,
                        ReleaseDate = releaseDate,
                        ArtistId = artist.Id,
                        Tracks = new List<Track>()
                    };
                    _context.Albums.Add(album);
                    if (!artist.Albums.Any(t => t.SpotifyAlbumId == spotifyAlbumId))
                    {
                        artist.Albums.Add(album);
                    }
                    artist.Albums.Add(album);
                    await _context.SaveChangesAsync();
                }

                // add the track to the db
                var track = await _context.Tracks.FirstOrDefaultAsync(t => t.SpotifyTrackId == spotifyTrackId);
                if (track == null)
                {
                    track = new Track
                    {
                        SpotifyTrackId = spotifyTrackId,
                        Name = trackName,
                        Duration = duration,
                        ArtistId = artist.Id,
                        AlbumId = album.Id
                    };

                    _context.Tracks.Add(track);
                    _context.SaveChanges();
                }
                // add the track to the tracks list in album and artist tables

                if (!album.Tracks.Any(t => t.SpotifyTrackId == spotifyTrackId))
                {
                    album.Tracks.Add(track);
                }

                if (!artist.Tracks.Any(t => t.SpotifyTrackId == spotifyTrackId))
                {
                    artist.Tracks.Add(track);
                }

                var listeningHistory = await _context.ListeningHistories.FirstOrDefaultAsync(lh => lh.PlayedAt == playedAt);
                if (listeningHistory == null)
                {
                    listeningHistory = new ListeningHistory
                    {
                        UserId = user.Id,
                        TrackId = track.Id,
                        PlayedAt = playedAt
                    };
                    _context.ListeningHistories.Add(listeningHistory);
                    _context.SaveChanges();

                }
                recentTracks.Add(listeningHistory);
            }
            return recentTracks;
        }

        public async Task<Feature> GetTrackFeaturesAsync(int trackId)
        {
            var accessToken = await _authService.GetAccessTokenAsync();
            var client = new HttpClient();
            client.BaseAddress = new Uri(BaseUrl);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var track = await _context.Tracks.FirstOrDefaultAsync(t => t.Id == trackId);
            if (track == null)
            {
                throw new Exception($"Track {trackId} not found.");
            }
            var response = await client.GetAsync($"audio-features/{track.SpotifyTrackId}");
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to fetch track features. Status Code: {response.StatusCode}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(responseBody);
            var features = await _context.Features.FirstOrDefaultAsync(f => f.TrackId == trackId);
            if (features == null)
            {
                features = new Feature
                {
                    Danceability = float.Parse(json["danceability"].ToString()),
                    Energy = float.Parse(json["energy"].ToString()),
                    Tempo = float.Parse(json["tempo"].ToString()),
                    Valence = float.Parse(json["valence"].ToString()),
                    Instrumentalness = float.Parse(json["instrumentalness"].ToString()),
                    TrackId = trackId
                };
                _context.Features.Add(features);
                _context.SaveChanges();
            }

            return features;
        }



        private async Task<string> RefreshAccessToken(string refreshToken)
        {
            try
            {
                var tokenEndpoint = "https://accounts.spotify.com/api/token";

                var clientId = _configuration["Spotify:ClientId"];
                var clientSecret = _configuration["Spotify:ClientSecret"];

                using (var client = new HttpClient())
                {
                    var requestBody = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("grant_type", "refresh_token"),
                        new KeyValuePair<string, string>("refresh_token", refreshToken),
                        new KeyValuePair<string, string>("client_id", clientId),
                        new KeyValuePair<string, string>("client_secret", clientSecret)
                    };

                    var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
                    {
                        Content = new FormUrlEncodedContent(requestBody)
                    };

                    var response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var jsonResponse = JObject.Parse(responseContent);

                        // Extract the access token from the JSON response
                        var accessToken = jsonResponse["access_token"].ToString();
                        var userToken = await _context.SpotifyTokens.FirstOrDefaultAsync(t => t.RefreshToken == refreshToken);
                        if (userToken != null)
                        {
                            userToken.AccessToken = accessToken;
                            userToken.CreatedAt = DateTime.UtcNow;
                            _context.SpotifyTokens.Update(userToken);
                            _context.SaveChanges();
                        }

                        return accessToken;
                    }
                    else
                    {
                        throw new Exception($"Failed to refresh access token. Status code: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions here, log them, or rethrow as needed
                throw new Exception("Error refreshing access token", ex);
            }
        }


    }



    public class TrackInfo
    {
        public string Name { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
    }

}
