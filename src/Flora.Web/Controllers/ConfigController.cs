using AiCodo;
using Flora.Services;
using Flora.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flora.Web.Controllers
{
    [Authorize]
    public class ConfigController : ControllerBase
    {
        [AllowAnonymous]
        [Route("/LangText/{id}")]
        public IActionResult GetLangText(string id, [FromQuery] string defaultText, [FromQuery] string langCode)
        {
            return Ok(LangTextService.GetText(id, 
                defaultText.IsNullOrEmpty()?"":defaultText,
                langCode.IsNullOrEmpty()?"CN":langCode));
        }

        [Authorize]
        [Route("/Menus")]
        public IActionResult GetMenus()
        {
            var file="SysMenu.xml".FixedAppConfigPath();
            if (file.IsFileExists())
            {
                return PhysicalFile(file, "application/xml");
            }
            return NotFound();
        }
    }
}
