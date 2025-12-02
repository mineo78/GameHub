using System;

namespace GamingPlatform.Models.Puissance4
{
    public class Game
    {
        private bool isFirstPlayersTurn;

        public Game(Player player1, Player player2)
        {
            Player1 = player1;
            Player2 = player2;
            Id = Guid.NewGuid().ToString("d");
            Board = new Board();
            isFirstPlayersTurn = true;

            Player1.GameId = Id;
            Player2.GameId = Id;
            Player1.Color = "Rouge";
            Player2.Color = "Jaune";
        }

        public string Id { get; set; }
        public Player Player1 { get; set; }
        public Player Player2 { get; set; }
        public Board Board { get; set; }
        public Player WhoseTurn => isFirstPlayersTurn ? Player1 : Player2;
        public bool IsOver { get; private set; }
        public bool IsTie { get; private set; }
        public Player? Winner { get; private set; }

        public int PlacePiece(int column)
        {
            if (IsOver) return -1;
            if (!Board.IsColumnAvailable(column)) return -1;

            int row = Board.GetLowestAvailableRow(column);
            if (row == -1) return -1;

            string color = WhoseTurn.Color;
            Board.PlacePiece(row, column, color);

            if (Board.CheckWin(row, column))
            {
                IsOver = true;
                Winner = WhoseTurn;
            }
            else if (!Board.AreSpacesLeft)
            {
                IsOver = true;
                IsTie = true;
            }
            else
            {
                isFirstPlayersTurn = !isFirstPlayersTurn;
            }

            return row;
        }

        public bool IsValidMove(int column) => column >= 0 && column < 7 && Board.IsColumnAvailable(column);

        public override string ToString() => $"(Id={Id}, Player1={Player1}, Player2={Player2}, WhoseTurn={WhoseTurn})";
    }
}
