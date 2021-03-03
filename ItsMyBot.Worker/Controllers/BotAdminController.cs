using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace ItsMyBot.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BotAdminController : ControllerBase
    {        
        public BotAdminController()
        {
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> GoLive()
        {
            return Ok();
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> GetUsers()
        {
            return this.Ok();
        }


        [HttpGet]
        [Route("[action]")]
        public IActionResult GetCacheUsers()
        {
            return this.Ok();
        }       

        [HttpPost]
        [Route("[action]/{id}/{name}")]
        public async Task NewFollower()
        {

        }

    }
}
