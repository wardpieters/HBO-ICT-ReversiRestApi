namespace ReversiRestApi.Model
{
    public class GameResponse
    {
        public Spel Game;
        public GameStats Stats;
        public bool IsMyTurn = false;
        public bool IsPlayable = false;

        public GameResponse(Spel game)
        {
            Game = game;
            Stats = GenerateGameStats(game.Bord);
            IsPlayable = GameIsPlayable(game);

            Game.Player1Token = null;
            Game.Player2Token = null;
            Game.GameWinnerPlayerToken = null;
        }

        private bool GameIsPlayable(Spel game)
        {
            return !string.IsNullOrEmpty(game.Player1Token) && !string.IsNullOrEmpty(game.Player2Token);
        }

        public GameResponse(Spel game, string playerToken)
        {
            Game = game;
            Stats = GenerateGameStats(game.Bord);
            IsPlayable = GameIsPlayable(game);

            if (!string.IsNullOrEmpty(playerToken))
            {
                if (playerToken.Equals(game.Player1Token) && game.AandeBeurt.Equals(Kleur.Wit))
                {
                    IsMyTurn = true;
                }
                
                else if (playerToken.Equals(game.Player2Token) && game.AandeBeurt.Equals(Kleur.Zwart))
                {
                    IsMyTurn = true;
                }
            }

            Game.Player1Token = null;
            Game.Player2Token = null;
            Game.GameWinnerPlayerToken = null;
        }

        private GameStats GenerateGameStats(Kleur[,] bord)
        {
            int black = 0;
            int white = 0;

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    var value = bord[i, j];
                    if (value.Equals(Kleur.Wit))
                    {
                        white++;
                    } else if (value.Equals(Kleur.Zwart))
                    {
                        black++;
                    }
                }
            }
            
            return new GameStats
            {
                BlackCount = black,
                WhiteCount = white,
                TotalCount = black + white
            };
        }
    }
}