using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ReversiRestApi.Hubs;
using ReversiRestApi.Repository;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;

namespace ReversiRestApi.Controllers
{
    [Route("api/player")]
    [ApiController]
    public class PlayerController : ControllerBase
    {
        private readonly ISpelRepository _spelRepository;
        private readonly IHubContext<DefaultHub> _theHub;
        private readonly IConfiguration _configuration;

        public PlayerController(ISpelRepository repository, IHubContext<DefaultHub> defaultHub, IConfiguration configuration)
        {
            _spelRepository = repository;
            _theHub = defaultHub;
            _configuration = configuration;
        }

        // GET api/player
        [HttpGet("delete")]
        public ActionResult Delete(string apiKey, string playerToken)
        {
            if (!_configuration.GetValue<string>("StatsApiKey").Equals(apiKey)) {
                return BadRequest(new {message = "API key is invalid."});
            }

            if (string.IsNullOrWhiteSpace(playerToken)) {
                return BadRequest(new {message = "Player token is invalid."});
            }

            // Get all games
            var games = _spelRepository.GetSpellen()
                .Where(x => x.Player1Token == playerToken || x.Player2Token == playerToken)
                .ToList();
            
            games.ForEach(x => {
                if (x.Player1Token.Equals(playerToken)) x.Player1Token = x.GameFinished ? "DELETED_USER" : null;
                else if(x.Player2Token.Equals(playerToken)) x.Player2Token = x.GameFinished ? "DELETED_USER" : null;

                if (x.GameFinished && x.GameWinnerPlayerToken.Equals(playerToken))
                {
                    x.GameWinnerPlayerToken = "DELETED_USER";
                }

                if (!x.GameFinished)
                {
                    _theHub.Clients.All.SendAsync("LeavePlayerUpdate", x.Token);
                }
            });
            
            _spelRepository.Save();

            return Ok(new { message = "User deleted."});
        }
    }
}
  