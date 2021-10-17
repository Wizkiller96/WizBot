﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WizBot.VotesApi.Services;

namespace WizBot.VotesApi.Controllers
{
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly ILogger<WebhookController> _logger;
        private readonly IVotesCache _votesCache;
        private readonly IConfiguration _conf;

        public WebhookController(ILogger<WebhookController> logger, IVotesCache votesCache, IConfiguration conf)
        {
            _logger = logger;
            _votesCache = votesCache;
            _conf = conf;
        }
        
        [HttpPost("/discordswebhook")]
        [Authorize(Policy = Policies.DiscordsAuth)]
        public async Task<IActionResult> DiscordsWebhook([FromBody]DiscordsVoteWebhookModel data)
        {

            _logger.LogInformation("User {UserId} has voted for Bot {BotId} on {Platform}",
                data.User,
                data.Bot,
                "discords.com");

            await _votesCache.AddNewDiscordsVote(data.User);
            return Ok();
        }

        [HttpPost("/topggwebhook")]
        [Authorize(Policy = Policies.TopggAuth)]
        public async Task<IActionResult> TopggWebhook([FromBody] TopggVoteWebhookModel data)
        {
            _logger.LogInformation("User {UserId} has voted for Bot {BotId} on {Platform}",
                data.User,
                data.Bot,
                "top.gg");

            await _votesCache.AddNewTopggVote(data.User);
            return Ok();
        }
    }
}