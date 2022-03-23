﻿
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ReversiRestApi.Hubs;
using ReversiRestApi.Model;
using ReversiRestApi.Repository;
using Microsoft.AspNetCore.SignalR;

namespace ReversiRestApi.Controllers
{
    [Route("api/game")]
    [ApiController]
    public class SpelController : ControllerBase
    {
        private readonly ISpelRepository _spelRepository;
        private readonly IHubContext<DefaultHub> _theHub;

        public SpelController(ISpelRepository repository, IHubContext<DefaultHub> defaultHub)
        {
            _spelRepository = repository;
            _theHub = defaultHub;
        }

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
                if (game.Player1Token == null || game.Player1Token.Equals(playerToken))
                {
                    game.Player1Token = playerToken;
                }
                
                else if (game.Player2Token == null || game.Player2Token.Equals(playerToken))
                {
                    game.Player2Token = playerToken;    
                }
                else
                {
                    return BadRequest(new { message = "Game already has two players"});
                }

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
            
            if (moveInfo.playerToken.Equals(game.Player1Token))
            {
                // player 1 doet zet
                if (!game.AandeBeurt.Equals(Kleur.Wit))
                {
                    return BadRequest(new { message = "wit is niet aan de beurt"});
                }
            }
            else if(moveInfo.playerToken.Equals(game.Player2Token))
            {
                // player 2 doet zet
                if (!game.AandeBeurt.Equals(Kleur.Zwart))
                {
                    return BadRequest(new { message = "zwart is niet aan de beurt"});
                }
            }
            else
            {
                // Speler die niet in het spel zit doet zet
                return BadRequest(new { message = "bruh wie ben jij"});
            }

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

            _theHub.Clients.All.SendAsync("ReceiveMovementUpdate");

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
  