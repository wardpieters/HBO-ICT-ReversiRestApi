using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ReversiRestApi.Model;
using ReversiRestApi.Repository;

namespace ReversiRestApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SpelController : ControllerBase
    {
        private readonly ISpelRepository spelRepository;
        public SpelController(ISpelRepository repository) => spelRepository = repository;

        // GET api/spel
        [HttpGet]
        public ActionResult<IEnumerable<string>> GetSpelOmschrijvingenVanSpellenMetWachtendeSpeler()
        {
            IEnumerable<string> games = spelRepository.GetSpellen().Where(x => String.IsNullOrWhiteSpace(x.Speler2Token)).Select(x => x.Omschrijving);

            return Ok(games);
        }

        // GET api/spel
        [HttpGet("{token}")]
        public ActionResult<Spel> GetGame(string token)
        {
            var game = spelRepository.GetSpel(token);

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
            var game = spelRepository.GetSpelByPlayerToken(token);

            if (game == null)
            {
                return NotFound();
            }

            return Ok(game);
        }

        [HttpPost]
        public ActionResult CreateGame([BindRequired, FromBody] GameInfoApi gameInfo)
        {
            Spel spel = new Spel();
            spel.Speler1Token = gameInfo.token;
            spel.Omschrijving = gameInfo.description;

            spelRepository.AddSpel(spel);

            return Created(spel.Token, spel);
        }
    }
}
  