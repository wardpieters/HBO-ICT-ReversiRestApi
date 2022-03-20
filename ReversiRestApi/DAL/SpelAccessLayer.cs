using System.Collections.Generic;
using System.Linq;
using ReversiRestApi.Helpers;
using ReversiRestApi.Model;
using ReversiRestApi.Repository;

namespace ReversiRestApi.DAL
{
    public class SpelAccessLayer : ISpelRepository
    {
        private ReversiContext _context;

        public SpelAccessLayer(ReversiContext context) { _context = context; }

        public void AddSpel(Spel spel)
        {
            _context.Spellen.Add(spel);
            _context.SaveChanges();
        }

        public List<Spel> GetSpellen()
        {
            return _context.Spellen.ToList();
        }

        public Spel GetSpel(string spelToken)
        {
            return _context.Spellen.FirstOrDefault(spel => spel.Token == spelToken);
        }

        public void Delete(string gameToken)
        {
            _context.Spellen.RemoveWhere(x => x.Token == gameToken);
            Save();
        }

        public bool IsInGame(string playerToken)
        {
            return GetSpelByPlayerToken(playerToken) != null;
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        public Spel GetSpelByPlayerToken(string playerToken)
        {
            return _context.Spellen.FirstOrDefault(spel => spel.Player1Token == playerToken || spel.Player2Token == playerToken);
        }
    }
}