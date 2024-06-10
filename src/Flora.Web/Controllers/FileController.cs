using Microsoft.AspNetCore.Mvc;

namespace Flora.Web.Controllers
{
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        [HttpPost]
        [Route("{group}/{name}")]
        public Task<IActionResult> Post(string group, string name)
        {
            return null;
        }
    }
}
