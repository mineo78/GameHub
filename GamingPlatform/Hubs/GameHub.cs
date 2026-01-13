using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace GamingPlatform.Hubs
{
    public class GameHub : Hub
    {
        private static int[][] _board = new int[6][] 
        {
            new int[7], new int[7], new int[7], new int[7], new int[7], new int[7]
        };

        private static int _currentPlayer = 1;

        public GameHub()
        {
            if (_board[0][0] == 0)
            {
                for (int i = 0; i < 6; i++)
                    _board[i] = new int[7];
            }
        }

        public override Task OnConnectedAsync()
        {
            Clients.Caller.SendAsync("InitBoard", _board);
            return base.OnConnectedAsync();
        }

        public async Task MakeMove(int row, int col)
        {
            if (row < 0 || row >= 6 || col < 0 || col >= 7 || _board[row][col] != 0)
                return;

            _board[row][col] = _currentPlayer;

            if (CheckWin(_currentPlayer))
            {
                await Clients.All.SendAsync("GameOver", _currentPlayer);
                ResetBoard();
                return;
            }

            _currentPlayer = _currentPlayer == 1 ? 2 : 1;

            await Clients.All.SendAsync("UpdateBoard", _board);
        }

        private bool CheckWin(int player)
        {
            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col <= 7 - 4; col++)
                {
                    if (_board[row][col] == player &&
                        _board[row][col + 1] == player &&
                        _board[row][col + 2] == player &&
                        _board[row][col + 3] == player)
                    {
                        return true;
                    }
                }
            }

            for (int col = 0; col < 7; col++)
            {
                for (int row = 0; row <= 6 - 4; row++)
                {
                    if (_board[row][col] == player &&
                        _board[row + 1][col] == player &&
                        _board[row + 2][col] == player &&
                        _board[row + 3][col] == player)
                    {
                        return true;
                    }
                }
            }

            for (int row = 0; row <= 6 - 4; row++)
            {
                for (int col = 0; col <= 7 - 4; col++)
                {
                    if (_board[row][col] == player &&
                        _board[row + 1][col + 1] == player &&
                        _board[row + 2][col + 2] == player &&
                        _board[row + 3][col + 3] == player)
                    {
                        return true;
                    }
                }
            }

            for (int row = 0; row <= 6 - 4; row++)
            {
                for (int col = 3; col < 7; col++)
                {
                    if (_board[row][col] == player &&
                        _board[row + 1][col - 1] == player &&
                        _board[row + 2][col - 2] == player &&
                        _board[row + 3][col - 3] == player)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void ResetBoard()
        {
            for (int i = 0; i < 6; i++)
                for (int j = 0; j < 7; j++)
                    _board[i][j] = 0;

            _currentPlayer = 1;
        }
    }
}
