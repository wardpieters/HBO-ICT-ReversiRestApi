using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ReversiRestApi.Model;
using ReversiRestApi.Repository;

namespace ReversiRestApi.Controllers
{
    [Route("api/stats")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly ISpelRepository _spelRepository;
        private readonly IConfiguration _configuration;

        public StatisticsController(IConfiguration configuration, ISpelRepository repository)
        {
            _spelRepository = repository;
            _configuration = configuration;
        }
        
        [HttpGet]
        public IActionResult Get(string apiKey)
        {
            if (!_configuration.GetValue<string>("StatsApiKey").Equals(apiKey))
            {
                return BadRequest(new {message = "API key is invalid."});
            }
            
            var games = _spelRepository.GetSpellen().Where(s => s.GameFinished);
            IEnumerable<StatisticsInfoApi> gameList = games.Select(g => new StatisticsInfoApi
            {
                GameToken = g.Token,
                GameWinnerToken = g.GameWinner == null ? null : g.GameWinnerPlayerToken,
                Player1Token = g.Player1Token,
                Player2Token = g.Player2Token
            });
            
            return Ok(gameList);
        }
    }
}