using System.Linq;

namespace GamingPlatform.Models.Puissance4
{
    public class Board
    {
        private const int ROWS = 6;
        private const int COLS = 7;
        private int totalPiecesPlaced;

        public Board()
        {
            Pieces = new string[ROWS][];
            for (int i = 0; i < ROWS; i++)
            {
                Pieces[i] = new string[COLS];
                for (int j = 0; j < COLS; j++)
                {
                    Pieces[i][j] = "Empty";
                }
            }
        }

        public string[][] Pieces { get; private set; }
        public bool AreSpacesLeft => totalPiecesPlaced < (ROWS * COLS);

        public bool IsColumnAvailable(int col)
        {
            if (col < 0 || col >= COLS) return false;
            return Pieces[0][col] == "Empty";
        }

        public int GetLowestAvailableRow(int col)
        {
            if (col < 0 || col >= COLS) return -1;
            
            for (int row = ROWS - 1; row >= 0; row--)
            {
                if (Pieces[row][col] == "Empty") return row;
            }
            return -1;
        }

        public void PlacePiece(int row, int col, string color)
        {
            if (row >= 0 && row < ROWS && col >= 0 && col < COLS)
            {
                Pieces[row][col] = color;
                totalPiecesPlaced++;
            }
        }

        public bool CheckWin(int row, int col)
        {
            string color = Pieces[row][col];
            if (color == "Empty") return false;

            if (CountDirection(row, col, 0, 1, color) + CountDirection(row, col, 0, -1, color) >= 3) return true;
            if (CountDirection(row, col, 1, 0, color) + CountDirection(row, col, -1, 0, color) >= 3) return true;
            if (CountDirection(row, col, 1, 1, color) + CountDirection(row, col, -1, -1, color) >= 3) return true;
            if (CountDirection(row, col, 1, -1, color) + CountDirection(row, col, -1, 1, color) >= 3) return true;

            return false;
        }

        private int CountDirection(int row, int col, int dRow, int dCol, string color)
        {
            int count = 0;
            int r = row + dRow;
            int c = col + dCol;

            while (r >= 0 && r < ROWS && c >= 0 && c < COLS && Pieces[r][c] == color)
            {
                count++;
                r += dRow;
                c += dCol;
            }

            return count;
        }

        public override string ToString() => string.Join("\n", Pieces.Select(row => string.Join(" ", row)));
    }
}
