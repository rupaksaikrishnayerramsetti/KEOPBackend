using KEOPBackend.Data;
using KEOPBackend.helpers.AlertUtility;
using KEOPBackend.helpers.EmailServices;
using KEOPBackend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace KEOPBackend.Controllers
{
    [ApiController]
    public class AlertsController : ControllerBase
    {
        private readonly AlertsDbContext _alertsDbContext;
        private readonly IConfiguration _configuration;
        public AlertsController(AlertsDbContext alertsDbContext, IConfiguration configuration)
        {
            _alertsDbContext = alertsDbContext;
            _configuration = configuration;
        }

        [Route("createAlert")]
        [HttpPost]
        public async Task<IActionResult> CreateAlert()
        {
            try
            {
                var content = await new StreamReader(Request.Body).ReadToEndAsync();
                dynamic requestData = Newtonsoft.Json.JsonConvert.DeserializeObject(content);
                var jwtToken = (string)requestData.token;
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true
                };
                SecurityToken validatedToken;
                ClaimsPrincipal claimsPrincipal;

                try
                {
                    claimsPrincipal = tokenHandler.ValidateToken(jwtToken, validationParameters, out validatedToken);
                }
                catch (SecurityTokenValidationException ex)
                {
                    Console.WriteLine($"Token validation failed: {ex.Message}");
                    return Unauthorized("Token validation failed");
                }
                var emailClaim = claimsPrincipal.FindFirst(ClaimTypes.Email);
                if (emailClaim == null)
                {
                    return BadRequest("Email claim not found in the token.");
                }
                var userEmail = emailClaim.Value;
                var userIdClaim = claimsPrincipal.FindFirst("user_id");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                {
                    //return Ok(userId);
                    Alerts alerts = new Alerts();
                    alerts.user_id = userId;
                    alerts.title = (string)requestData.title;
                    alerts.title = alerts.title.ToUpper();
                    alerts.date = (string)requestData.date;
                    alerts.time = (string)requestData.time;
                    alerts.created_on = DateTime.UtcNow;
                    await _alertsDbContext.Alerts.AddAsync(alerts);
                    await _alertsDbContext.SaveChangesAsync();
                    AlertUtility alertUtility = new AlertUtility();
                    alerts.date = alertUtility.ModifyDate(alerts.date);
                    alerts.time = alertUtility.ModifyTime(alerts.time);
                    //string title, string date, string time, string description = ""
                    string description = $"You had an important meeting of {alerts.title}";
                    string eventLink = alertUtility.GenerateGoogleCalendarLink(alerts.title, alerts.date, alerts.time, description);
                    EmailSending emailSending = new EmailSending();
                    string template = emailSending.UserAlertMsgTemplate(alerts.title, alerts.date, alerts.time, eventLink);
                    emailSending.SendEmailAsync(userEmail, "New Remainder Creation Alert", template);
                    return Ok(true);
                }
                return BadRequest("Invalid Token provided");
            }
            catch (Exception ex)
            {
                return Ok(false);
            }
        }

        [Route("editAlert")]
        [HttpPost]
        public async Task<IActionResult> EditAlert()
        {
            try
            {
                var content = await new StreamReader(Request.Body).ReadToEndAsync();
                dynamic requestData = Newtonsoft.Json.JsonConvert.DeserializeObject(content);

                var jwtToken = (string)requestData.token;
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true
                };

                SecurityToken validatedToken;
                ClaimsPrincipal claimsPrincipal;

                try
                {
                    claimsPrincipal = tokenHandler.ValidateToken(jwtToken, validationParameters, out validatedToken);
                }
                catch (SecurityTokenValidationException ex)
                {
                    // Log the exception details instead of writing to the console
                    Console.WriteLine($"Token validation failed: {ex.Message}");
                    return Unauthorized("Token validation failed");
                }
                var emailClaim = claimsPrincipal.FindFirst(ClaimTypes.Email);
                if (emailClaim == null)
                {
                    return BadRequest("Email claim not found in the token.");
                }
                var userEmail = emailClaim.Value;
                var userIdClaim = claimsPrincipal.FindFirst("user_id");
                int.TryParse(userIdClaim.Value, out int userId);
                if (int.TryParse(requestData.alert_id?.ToString(), out int AlertId) &&
                    userIdClaim != null)
                {
                    Alerts alerts = new Alerts
                    {
                        alert_id = AlertId,
                        user_id = userId,
                        title = (string)requestData.title,
                        date = (string)requestData.date,
                        time = (string)requestData.time,
                        modified_on = DateTime.UtcNow
                    };

                    _alertsDbContext.Entry(alerts).State = EntityState.Modified;
                    await _alertsDbContext.SaveChangesAsync();
                    AlertUtility alertUtility = new AlertUtility();
                    alerts.date = alertUtility.ModifyDate(alerts.date);
                    alerts.time = alertUtility.ModifyTime(alerts.time);
                    string description = $"You had an important meeting of {alerts.title}";
                    string eventLink = alertUtility.GenerateGoogleCalendarLink(alerts.title, alerts.date, alerts.time, description);
                    EmailSending emailSending = new EmailSending();
                    string template = emailSending.UserEditAlertMsgTemplate(alerts.title, alerts.date, alerts.time, eventLink);
                    emailSending.SendEmailAsync(userEmail, "Remainder Update Alert", template);
                    return Ok(true);

                    return Ok(true);
                }

                return BadRequest("Invalid Token provided");
            }
            catch (Exception ex)
            {
                // Log the exception details instead of returning a generic response
                Console.WriteLine($"Exception during EditNote: {ex.Message}");
                return StatusCode(500, "Internal Server Error");
            }
        }

        [Route("fetchAllAlerts")]
        [HttpGet]
        public async Task<IActionResult> fetchAllAlerts()
        {
            try
            {
                string jwtToken = Request.Headers["Authorization"];

                if (string.IsNullOrEmpty(jwtToken))
                {
                    return BadRequest("Authorization token is missing");
                }
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true
                };
                SecurityToken validatedToken;
                ClaimsPrincipal claimsPrincipal;

                try
                {
                    claimsPrincipal = tokenHandler.ValidateToken(jwtToken, validationParameters, out validatedToken);
                }
                catch (SecurityTokenValidationException ex)
                {
                    Console.WriteLine($"Token validation failed: {ex.Message}");
                    return Unauthorized("Token validation failed");
                }

                var userIdClaim = claimsPrincipal.FindFirst("user_id");

                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                {
                    var alerts = await _alertsDbContext.Alerts
                        .Where(a => a.user_id == userId)
                        .Select(a => new { a.alert_id, a.title, a.date, a.time })
                        .ToListAsync();
                    return Ok(alerts);
                }
                return Ok(new { });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception while fetching " + ex.Message);
                return StatusCode(500, "Internal Server Error");
            }
        }

        [Route("deleteAlert")]
        [HttpDelete]
        public async Task<IActionResult> DeleteAlert()
        {
            try
            {
                string jwtToken = Request.Headers["Authorization"];
                if (string.IsNullOrEmpty(jwtToken))
                {
                    return BadRequest("Authorization token is missing");
                }
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true
                };
                SecurityToken validatedToken;
                ClaimsPrincipal claimsPrincipal;

                try
                {
                    claimsPrincipal = tokenHandler.ValidateToken(jwtToken, validationParameters, out validatedToken);
                }
                catch (SecurityTokenValidationException ex)
                {
                    Console.WriteLine($"Token validation failed: {ex.Message}");
                    return Unauthorized("Token validation failed");
                }

                var userIdClaim = claimsPrincipal.FindFirst("user_id");

                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                {
                    if (int.TryParse(Request.Headers["alertId"].ToString(), out int alertId))
                    {
                        var noteToDelete = await _alertsDbContext.Alerts
                            .FirstOrDefaultAsync(a => a.user_id == userId && a.alert_id == alertId);

                        if (noteToDelete != null)
                        {
                            _alertsDbContext.Alerts.Remove(noteToDelete);
                            await _alertsDbContext.SaveChangesAsync();

                            return Ok(true);
                        }
                    }

                    return Ok(false);
                }

                return BadRequest("Invalid Token provided");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception while Deleting " + ex.Message);
                return StatusCode(500, "Internal Server Error");
            }
        }

        [Route("getAlertscount")]
        [HttpGet]
        public async Task<IActionResult> GetAlertsCount()
        {
            try
            {
                string jwtToken = Request.Headers["Authorization"];

                if (string.IsNullOrEmpty(jwtToken))
                {
                    return BadRequest("Authorization token is missing");
                }
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true
                };
                SecurityToken validatedToken;
                ClaimsPrincipal claimsPrincipal;

                try
                {
                    claimsPrincipal = tokenHandler.ValidateToken(jwtToken, validationParameters, out validatedToken);
                }
                catch (SecurityTokenValidationException ex)
                {
                    Console.WriteLine($"Token validation failed: {ex.Message}");
                    return Unauthorized("Token validation failed");
                }

                var userIdClaim = claimsPrincipal.FindFirst("user_id");

                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                {
                    var rowCount = _alertsDbContext.Alerts
                    .Where(n => n.user_id == userId)
                    .Count();
                    return Ok(rowCount);
                }
                return Ok(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception while fetching " + ex.Message);
                return StatusCode(500, "Internal Server Error");
            }
        }

        [Route("fetchAlerts")]
        [HttpGet]
        public async Task<IActionResult> FetchAlerts()
        {
            try
            {
                string jwtToken = Request.Headers["Authorization"];

                if (string.IsNullOrEmpty(jwtToken))
                {
                    return BadRequest("Authorization token is missing");
                }
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"])),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true
                };
                SecurityToken validatedToken;
                ClaimsPrincipal claimsPrincipal;

                try
                {
                    claimsPrincipal = tokenHandler.ValidateToken(jwtToken, validationParameters, out validatedToken);
                }
                catch (SecurityTokenValidationException ex)
                {
                    Console.WriteLine($"Token validation failed: {ex.Message}");
                    return Unauthorized("Token validation failed");
                }

                var userIdClaim = claimsPrincipal.FindFirst("user_id");

                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                {
                    var rowCount = _alertsDbContext.Alerts
                        .Where(n => n.user_id == userId)
                        .Count();

                    var notes = await _alertsDbContext.Alerts
                        .Where(a => a.user_id == userId)
                        .OrderByDescending(a => a.alert_id)
                        .Take(4)
                        .ToListAsync();

                    var response = new
                    {
                        count = rowCount,
                        data = notes.Select(a => new { a.alert_id, a.title, a.date, a.time }).ToList()
                    };

                    return Ok(response);
                }

                return Ok(new { count = 0, data = new List<object>() });

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception while fetching " + ex.Message);
                return StatusCode(500, "Internal Server Error");
            }
        }
    }
}
