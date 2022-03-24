using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ReversiRestApi.Model
{
    public class StatisticsInfoApi
    {
        public string GameToken { get; set; }
        public string Player1Token { get; set; }
        public string Player2Token { get; set; }
        public string GameWinnerToken { get; set; }
    }
}