using Doppler.PushContact.DopplerSecurity;
using Doppler.PushContact.Models;
using Doppler.PushContact.Services;
using Doppler.PushContact.Services.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Doppler.PushContact.Controllers
{
    [Authorize(Policies.ONLY_SUPERUSER)]
    [ApiController]
    public class DomainController : ControllerBase
    {
        private readonly IDomainService _domainService;
        private readonly IMessageRepository _messageRepository;
        private readonly IMessageSender _messageSender;

        public DomainController(IDomainService domainService, IMessageRepository messageRepository, IMessageSender messageSender)
        {
            _domainService = domainService;
            _messageRepository = messageRepository;
            _messageSender = messageSender;
        }

        [HttpPut]
        [Route("domains/{name}")]
        public async Task<IActionResult> Upsert([FromRoute] string name, [FromBody] Domain domain)
        {
            domain.Name = name;
            await _domainService.UpsertAsync(domain);

            return Ok();
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("domains/{name}/isPushFeatureEnabled")]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 120)]
        public async Task<ActionResult<bool>> GetPushFeatureStatus([FromRoute] string name)
        {
            var domain = await _domainService.GetByNameAsync(name);

            if (domain == null)
            {
                return NotFound();
            }

            return domain.IsPushFeatureEnabled;
        }

        [HttpPost]
        [Route("domains/{name}/message")]
        public async Task<IActionResult> CreateMessageAssociatedToDomain([FromRoute] string name, [FromBody] Message message)
        {
            try
            {
                _messageSender.ValidateMessage(message.Title, message.Body, message.OnClickLink, message.ImageUrl);
            }
            catch (ArgumentException argExc)
            {
                return UnprocessableEntity(argExc.Message);
            }

            var messageId = Guid.NewGuid();

            await _messageRepository.AddAsync(messageId, name, message.Title, message.Body, message.OnClickLink, 0, 0, 0, message.ImageUrl);

            return Ok(new MessageResult
            {
                MessageId = messageId
            });
        }
    }
}
