using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ReversiRestApi.Model
{
    public class MoveApi
    {
        public string playerToken;
        public int x;
        public int y;
    }
}