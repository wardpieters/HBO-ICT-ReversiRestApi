using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ReversiRestApi.Model
{
    public class GameStats
    {
        public int BlackCount;
        public int WhiteCount;
        public int TotalCount;
    }
}