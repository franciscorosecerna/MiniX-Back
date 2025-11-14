
using Microsoft.AspNetCore.Mvc;

namespace Minix.Backend.Controllers;

[ApiController]
public class HomeController : ControllerBase
{
    [HttpGet("/")]
    public IActionResult AbrirSwagger(){
        return Redirect("/swagger");
    }
}
