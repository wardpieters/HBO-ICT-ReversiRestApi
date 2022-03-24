
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
                .Where(s => String.IsNullOrEmpty(s.Player2Token) || String.IsNullOrEmpty(s.Player1Token))
                .Select(s => new {s.Description, s.Token}).ToList();
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

            return Ok(game.RemoveSensitiveInformation());
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

            return Ok(game.RemoveSensitiveInformation());
        }

        // GET api/spel/player-token/
        [HttpGet("player-token/{token}/active")]
        public ActionResult<Spel> GetActiveGameByPlayer(string token)
        {
            var game = _spelRepository.GetSpelByPlayerToken(token);

            if (game == null || game.GameFinished)
            {
                return NotFound(new { message = "No game with that player token found"});
            }

            return Ok(game.RemoveSensitiveInformation());
        }

        [HttpPost]
        public ActionResult CreateGame([BindRequired, FromBody] GameInfoApi gameInfo)
        {
            if (_spelRepository.IsInActiveGame(gameInfo.Player1Token)) return BadRequest(new { message = "You're already in an active game."});

            Spel spel = new Spel();
            spel.Player1Token = gameInfo.Player1Token;
            spel.Description = gameInfo.Description;

            _spelRepository.AddSpel(spel);

            return Created(spel.Token, spel.RemoveSensitiveInformation());
        }
        
        [HttpPut("{token}/join")]
        public ActionResult<Spel> JoinGame(string token, [FromBody] string playerToken)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(playerToken)) 
                return BadRequest(new { message = "Missing fields"});

            Spel game = _spelRepository.GetSpel(token);

            if (game == null) return NotFound(new { message = "Game not found"});

            if (!game.HasPlayer(playerToken) && _spelRepository.IsInActiveGame(playerToken))
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

            return Ok(game.RemoveSensitiveInformation());
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

            return Ok(spel.RemoveSensitiveInformation());
        }

        [HttpPost("{token}/move")]
        public ActionResult<Spel> PlaceMove(string token, [FromBody] MoveApi moveInfo)
        {
            if (string.IsNullOrEmpty(token)) return BadRequest(new { message = "Token not provided."});
            var game = _spelRepository.GetSpel(token);

            if (game == null) return NotFound(new { message = "Game not found"});
            if (string.IsNullOrEmpty(moveInfo.playerToken) || !game.HasPlayer(moveInfo.playerToken)) return BadRequest(new { message = "Invalid player provided"});
            
            if (game.GameFinished) return BadRequest(new { message = "Dit spel is afgelopen"});
            if (!game.IsPlayable()) return BadRequest(new { message = "Nodig een andere speler uit om te beginnen! Je bent nu helemaal alleen."});
            
            if (moveInfo.playerToken.Equals(game.Player1Token))
            {
                // player 1 doet zet
                if (!game.AandeBeurt.Equals(Kleur.Wit))
                {
                    return BadRequest(new { message = "Jij (wit) bent niet aan de beurt"});
                }
            }
            else if(moveInfo.playerToken.Equals(game.Player2Token))
            {
                // player 2 doet zet
                if (!game.AandeBeurt.Equals(Kleur.Zwart))
                {
                    return BadRequest(new { message = "Jij (zwart) bent niet aan de beurt"});
                }
            }
            else
            {
                // Speler die niet in het spel zit doet zet
                return BadRequest(new { message = "Unknown user"});
            }

            try
            {
                game.DoeZet(moveInfo.x, moveInfo.y);
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.Message});
            }

            if (game.Afgelopen())
            {
                var overwegendeKleur = game.OverwegendeKleur();
                game.GameWinner = overwegendeKleur;
                game.GameWinnerPlayerToken = overwegendeKleur == Kleur.Geen ? null : (overwegendeKleur == Kleur.Wit ? game.Player1Token : game.Player2Token);
                game.GameFinished = true;
            }
            
            _spelRepository.Save(game);

            _theHub.Clients.All.SendAsync("ReceiveMovementUpdate");

            return Ok(game.RemoveSensitiveInformation());
        }

        [HttpGet("{token}/{playerToken}/skip")]
        public ActionResult<Spel> SkipMove(string token, string playerToken)
        {
            if (string.IsNullOrEmpty(token)) return BadRequest(new { message = "Token not provided."});
            var game = _spelRepository.GetSpel(token);

            if (game == null) return NotFound(new { message = "Game not found"});
            if (string.IsNullOrEmpty(playerToken) || !game.HasPlayer(playerToken)) return BadRequest(new { message = "Invalid token"});
            
            if (game.GameFinished) return BadRequest(new { message = "Dit spel is afgelopen"});
            if (!game.IsPlayable()) return BadRequest(new { message = "Nodig een andere speler uit om te beginnen! Je bent nu helemaal alleen."});

            try
            {
                game.Pas();
                _spelRepository.Save(game);
                
                return Ok(game.RemoveSensitiveInformation());
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.Message});
            }
        }
    }
}
  