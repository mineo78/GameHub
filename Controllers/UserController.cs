using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{


    [ApiController]
    [Route("api/[controller]")]
    public class UserController(IUserService userService) : ControllerBase
    {
        private readonly IUserService _userService = userService;

        // Modèle de requête simple (DTO) pour la saisie du pseudo
        public record CreateUserRequest(string Pseudo);


        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(CreateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Pseudo))
            {
                return BadRequest("Le pseudo est obligatoire.");
            }

            try
            {
                var user = await _userService.CreateUserAsync(request.Pseudo);
                return CreatedAtAction(nameof(CreateUser), user);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
