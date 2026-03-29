using Microsoft.AspNetCore.Mvc;
using ToTheTopDotnet.Game;

namespace ToTheTopDotnet.Controllers;

public record TapRequest(string PlayerId);

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly GameEngine _gameEngine;

    public GameController(GameEngine gameEngine)
    {
        _gameEngine = gameEngine;
    }

    /// POST /api/game/{lobbyId}/tap - player tapped
    /// this is a *hot path*
    /// intentionally kept lightweight, no async, just state mutation
    [HttpPost("{lobbyId}/tap")]
    public IActionResult Tap(string lobbyId, [FromBody] TapRequest request)
    {
        _gameEngine.HandleTap(lobbyId, request.PlayerId);
        return Ok();
    }

    /// GET /api/game/{lobbyId} - get current game state for spectators joining mid-game
    [HttpGet("{lobbyId}")]
    public IActionResult GetState(string lobbyId)
    {
        var game = _gameEngine.GetGame(lobbyId);
        if (game == null)
            return NotFound(new { error = "No active game for this lobby" });

        return Ok(new
        {
            game.LobbyId,
            game.State,
            game.TimeRemaining,
            game.WinnerId,
            players = game.Players.Values.Select(p => new
            {
                p.Id,
                p.Name,
                p.Platform,
                p.Value,
                p.TapCount
            })
        });
    }
}