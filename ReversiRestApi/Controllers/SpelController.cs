
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

        // GET api/game
        [HttpGet]
        public ActionResult<IEnumerable<object>> Get()
        {
            return _spelRepository.GetSpellen()
                .Where(s => String.IsNullOrEmpty(s.Player2Token) || String.IsNullOrEmpty(s.Player1Token))
                .Select(s => new {s.Description, s.Token}).ToList();
        }

        // GET api/game/gameToken
        [HttpGet("{token}")]
        public ActionResult<Spel> GetGame(string token)
        {
            var game = _spelRepository.GetSpel(token);

            if (game == null)
            {
                return NotFound();
            }

            return Ok(new GameResponse(game));
        }
        
        // GET api/game/gameToken/playerToken
        [HttpGet("{gameToken}/{playerToken}")]
        public ActionResult<Spel> GetGame(string gameToken, string playerToken)
        {
            var game = _spelRepository.GetSpel(gameToken);

            if (game == null)
            {
                return NotFound();
            }

            return Ok(new GameResponse(game, playerToken));
        }

        // GET api/game/player-token/
        [HttpGet("player-token/{token}")]
        public ActionResult<Spel> GetGameByPlayerToken(string token)
        {
            var game = _spelRepository.GetSpelByPlayerToken(token);

            if (game == null)
            {
                return NotFound();
            }

            return Ok(new GameResponse(game, token));
        }

        // GET api/game/player-token/
        [HttpGet("player-token/{token}/active")]
        public ActionResult<Spel> GetActiveGameByPlayer(string token)
        {
            var games = _spelRepository.GetActiveGamesByPlayerToken(token);
            var game = games.FirstOrDefault();

            if (game == null || game.GameFinished)
            {
                return NotFound(new { message = "No game with that player token found"});
            }

            return Ok(new GameResponse(game, token));
        }

        [HttpPost]
        public ActionResult CreateGame([BindRequired, FromBody] GameInfoApi gameInfo)
        {
            if (_spelRepository.IsInActiveGame(gameInfo.Player1Token)) return BadRequest(new { message = "You're already in an active game."});

            Spel spel = new Spel();
            spel.Player1Token = gameInfo.Player1Token;
            spel.Description = gameInfo.Description;

            _spelRepository.AddSpel(spel);
            
            return Created(spel.Token, new GameResponse(spel, gameInfo.Player1Token));
        }
        
        [HttpPut("{token}/join")]
        public ActionResult<Spel> JoinGame(string token, [FromBody] string playerToken)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(playerToken)) 
                return BadRequest(new { message = "Missing fields"});

            Spel game = _spelRepository.GetSpel(token);

            if (game == null) return NotFound(new { message = "Game not found"});

            if (!game.HasPlayer(playerToken) && _spelRepository.IsInActiveGame(playerToken))
                return BadRequest(new { message = "You've already joined another game."});

            bool playerIsNew = false;

            if (!game.HasPlayer(playerToken))
            {
                if (game.Player1Token == null || game.Player1Token.Equals(playerToken))
                {
                    game.Player1Token = playerToken;
                    playerIsNew = true;
                }
                
                else if (game.Player2Token == null || game.Player2Token.Equals(playerToken))
                {
                    game.Player2Token = playerToken;
                    playerIsNew = true;
                }
                else
                {
                    return BadRequest(new { message = "Game already has two players"});
                }

                _spelRepository.Save();
            }

            if (playerIsNew) _theHub.Clients.All.SendAsync("JoinPlayerUpdate", token);
            
            return Ok(new GameResponse(game, playerToken));
        }

        [HttpPut("{token}/leave")]
        public ActionResult<Spel> LeaveGame(string token, [FromBody] string playerToken)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(playerToken))
            {
                return BadRequest(new { message = "Missing fields"});
            }
            
            Spel game = _spelRepository.GetSpel(token);
            
            if (!game.HasPlayer(playerToken))
            {
                return BadRequest(new { message = "Not in game"});
            }
            
            if (game.GameFinished) return BadRequest(new { message = "Dit spel is afgelopen"});

            if (game.Player1Token != null && game.Player1Token.Equals(playerToken))
            {
                game.Player1Token = null;
            } else if (game.Player2Token != null && game.Player2Token.Equals(playerToken))
            {
                game.Player2Token = null;
            }
            
            _spelRepository.Save();
            _theHub.Clients.All.SendAsync("LeavePlayerUpdate", token);

            return Ok(new GameResponse(game, playerToken));
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

            _theHub.Clients.All.SendAsync("ReceiveMovementUpdate", token);

            return Ok(new GameResponse(game, moveInfo.playerToken));
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
            
            if (playerToken.Equals(game.Player1Token))
            {
                // player 1 doet zet
                if (!game.AandeBeurt.Equals(Kleur.Wit))
                {
                    return BadRequest(new { message = "Jij (wit) bent niet aan de beurt"});
                }
            }
            else if(playerToken.Equals(game.Player2Token))
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
                game.Pas();
                _spelRepository.Save(game);
                
                _theHub.Clients.All.SendAsync("ReceiveSkipUpdate", token);
                
                return Ok(new GameResponse(game, playerToken));
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e.Message});
            }
        }
    }
}
  