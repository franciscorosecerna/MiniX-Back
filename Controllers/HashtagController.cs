using Microsoft.AspNetCore.Mvc;
using MiniX.Backend.Services;
namespace MiniX.Backend.Controllers;

[ApiController]
public class HashtagController: ControllerBase {

    private readonly IPostService _postService;
    public HashtagController(IPostService postService){
        _postService = postService;
    }

    [HttpGet("/api/htag")]
    public IActionResult ObtenerHashtags([FromQuery] string q = "") {
        if (q == "") return BadRequest(new { message = "No puede existir un Hashtag vacio" });

        string[] htags = _postService.SearchHashTags(q).Result;

        return Ok(htags);
    }
}
