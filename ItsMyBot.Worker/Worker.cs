using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace ItsMyBot.Worker
{
    public class BotWorker : BackgroundService
    {
        private readonly ILogger<BotWorker> _logger;
        private Bot _bot;
        
        public BotWorker(ILogger<BotWorker> logger, Bot bot)
        {
            _logger = logger;
            _bot = bot;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int delayInSeconds = 60;
            while (!stoppingToken.IsCancellationRequested)
            {
                #region Code to run every delayInSeconds
                //Add any code here that you want to run every delayInSeconds
                //Maybe add more logic here to create multiple timers.
                #endregion
                await Task.Delay(delayInSeconds * 1000, stoppingToken);                              
            }            
        }
    }
}
