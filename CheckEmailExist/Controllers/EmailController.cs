using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using CheckEmailExist.Services;
using Microsoft.AspNetCore.Mvc;

namespace CheckEmailExist.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmailController : ControllerBase
    {


        private readonly ILogger<EmailController> _logger;

        public EmailController(ILogger<EmailController> logger)
        {
            _logger = logger;
        }

        [HttpGet("ValidateEmail")]
        public bool ValidateEmail(string input)
        {
            var validator = new EmailValidator();

            return validator.Validate(input);
        }
    }
}