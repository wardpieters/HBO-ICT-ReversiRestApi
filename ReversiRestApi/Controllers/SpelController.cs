
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ReversiRestApi.Model;
using ReversiRestApi.Repository;

namespace ReversiRestApi.Controllers
{
    [Route("api/game")]
    [ApiController]
    public class SpelController : ControllerBase
    {
        private readonly ISpelRepository _spelRepository;
        public SpelController(ISpelRepository repository) => _spelRepository = repository;

        // GET api/spel
        [HttpGet]
        public ActionResult<IEnumerable<object>> GetSpelDescriptionenVanSpellenMetWachtendeSpeler()
        {
            return _spelRepository.GetSpellen()
                .Where(s => string.IsNullOrEmpty(s.Player2Token))
                .Select(s => new {s.Description, s.Token, s.Player1Token, s.Player2Token}).ToList();
        }

        // GET api/spel
        [HttpGet("{token}")]
        public ActionResult<Spel> GetGame(string token)
        {
            var game = _spelRepository.GetSpel(token);

            if (game == null)
            {
                return NotFound();
            }

            return Ok(game);
        }

        // GET api/spel/player-token/
        [HttpGet("player-token/{token}")]
        public ActionResult<Spel> GetGameByPlayerToken(string token)
        {
            var game = _spelRepository.GetSpelByPlayerToken(token);

            if (game == null)
            {
                return NotFound();
            }

            return Ok(game);
        }

        [HttpPost]
        public ActionResult CreateGame([BindRequired, FromBody] GameInfoApi gameInfo)
        {
            if (_spelRepository.IsInGame(gameInfo.Player1Token)) return BadRequest(new { message = "You're already in an active game."});

            Spel spel = new Spel();
            spel.Player1Token = gameInfo.Player1Token;
            spel.Description = gameInfo.Description;

            _spelRepository.AddSpel(spel);

            return Created(spel.Token, spel);
        }
        
        [HttpPut("{token}/join")]
        public ActionResult<Spel> JoinGame(string token, [FromBody] string playerToken)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(playerToken)) 
                return BadRequest(new { message = "Missing fields"});

            Spel game = _spelRepository.GetSpel(token);

            if (game == null) return NotFound(new { message = "Game not found"});

            if (!game.HasPlayer(playerToken) && _spelRepository.IsInGame(playerToken))
                return BadRequest(new { message = "Already in game"});

            if (!game.HasPlayer(playerToken))
            {
                game.Player1Token = playerToken;
                _spelRepository.Save();   
            }

            return Ok(game);
        }

        [HttpPut("{token}/leave")]
        public ActionResult<Spel> LeaveGame(string token, [FromBody] string playerToken)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(playerToken))
            {
                return BadRequest(new { message = "Missing fields"});
            }
            
            Spel spel = _spelRepository.GetSpel(token);
            
            if (!spel.HasPlayer(playerToken))
            {
                return BadRequest(new { message = "Not in game"});
            }

            if (spel.Player1Token.Equals(playerToken)) 
                spel.Player1Token = spel.Player2Token;

            spel.Player2Token = null;
            _spelRepository.Save();

            return Ok(spel);
        }

        [HttpPost("{token}/move")]
        public ActionResult<Spel> PlaceMove(string token, [FromBody] MoveApi moveInfo)
        {
            if (string.IsNullOrEmpty(token)) return BadRequest(new { message = "Token not provided."});
            var game = _spelRepository.GetSpel(token);

            if (game == null) return NotFound(new { message = "Game not found"});
            if (string.IsNullOrEmpty(moveInfo.playerToken) || !game.HasPlayer(moveInfo.playerToken)) return BadRequest(new { message = "Invalid player provided"});

            try
            {
                game.DoeZet(moveInfo.x, moveInfo.y);
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.Message});
            }
            
            string unixTimestamp = Convert.ToString((int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
            game.Description = unixTimestamp;
            
            _spelRepository.Save(game);
            
            return Ok(game);
        }

        [HttpGet("{token}/{playerToken}/skip")]
        public ActionResult<Spel> SkipMove(string token, string playerToken)
        {
            if (string.IsNullOrEmpty(token)) return BadRequest(new { message = "Token not provided."});
            var game = _spelRepository.GetSpel(token);

            if (game == null) return NotFound(new { message = "Game not found"});
            if (string.IsNullOrEmpty(playerToken) || !game.HasPlayer(playerToken)) return BadRequest(new { message = "Invalid token"});

            try
            {
                game.Pas();
                return Ok(game);
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.Message});
            }
        }
    }
}
  