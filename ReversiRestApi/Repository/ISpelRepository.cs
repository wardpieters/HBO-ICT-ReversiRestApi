using System.Collections.Generic;
using ReversiRestApi.Model;

namespace ReversiRestApi.Repository
{
    public interface ISpelRepository
    {
        void AddSpel(Spel spel);

        public List<Spel> GetSpellen();

        Spel GetSpel(string spelToken);

        Spel GetSpelByPlayerToken(string playerToken);
    }
}