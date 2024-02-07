using KEOPBackend.Data;
using KEOPBackend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static Azure.Core.HttpHeader;

namespace KEOPBackend.Controllers
{
    [ApiController]
    public class NotesController : ControllerBase
    {
        private readonly NotesDbContext _notesDbContext;
        private readonly IConfiguration _configuration;
        public NotesController(NotesDbContext notesDbContext, IConfiguration configuration)
        {
            _notesDbContext = notesDbContext;
            _configuration = configuration;
        }

        [Route("createNote")]
        [HttpPost]
        public async Task<IActionResult> CreateNote()
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

                var userIdClaim = claimsPrincipal.FindFirst("user_id");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                {
                    //return Ok(userId);
                    Notes notes = new Notes();
                    notes.user_id = userId;
                    notes.title = (string)requestData.title;
                    notes.value = (string)requestData.value;
                    notes.created_on = DateTime.UtcNow;
                    await _notesDbContext.Notes.AddAsync(notes);
                    await _notesDbContext.SaveChangesAsync();
                    return Ok(true);
                }
                return BadRequest("Invalid Token provided");
            }
            catch (Exception ex)
            {
                return Ok(false);
            }
        }

        [Route("editNote")]
        [HttpPost]
        public async Task<IActionResult> EditNote()
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

                var userIdClaim = claimsPrincipal.FindFirst("user_id");
                int.TryParse(userIdClaim.Value, out int userId);
                if (int.TryParse(requestData.note_id?.ToString(), out int noteId) &&
                    userIdClaim != null)
                {
                    Notes notes = new Notes
                    {
                        note_id = noteId,
                        user_id = userId,
                        title = (string)requestData.title,
                        value = (string)requestData.value,
                        modified_on = DateTime.UtcNow
                    };

                    _notesDbContext.Entry(notes).State = EntityState.Modified;
                    await _notesDbContext.SaveChangesAsync();

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

        [Route("fetchAllNotes")]
        [HttpGet]
        public async Task<IActionResult> fetchAllNotes()
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
                    var notes = await _notesDbContext.Notes
                        .Where(n => n.user_id == userId)
                        .Select(n => new { n.note_id, n.title, n.value })
                        .ToListAsync();

                    return Ok(notes);
                }
                return Ok(new { });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception while fetching "+ ex.Message );
                return StatusCode(500, "Internal Server Error");
            }
        }

        [Route("deleteNote")]
        [HttpDelete]
        public async Task<IActionResult> DeleteNote()
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
                    if (int.TryParse(Request.Headers["noteId"].ToString(), out int noteId))
                    {
                        var noteToDelete = await _notesDbContext.Notes
                            .FirstOrDefaultAsync(n => n.user_id == userId && n.note_id == noteId);

                        if (noteToDelete != null)
                        {
                            _notesDbContext.Notes.Remove(noteToDelete);
                            await _notesDbContext.SaveChangesAsync();

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

        [Route("getnotecount")]
        [HttpGet]
        public async Task<IActionResult> GetNoteCount()
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
                    var rowCount = _notesDbContext.Notes
                    .Where(n => n.user_id == userId)
                    .Count();
                    return Ok(rowCount);
                }
                return Ok(new { });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception while fetching " + ex.Message);
                return StatusCode(500, "Internal Server Error");
            }
        }

        [Route("fetchNotes")]
        [HttpGet]
        public async Task<IActionResult> FetchNotes()
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
                    var rowCount = _notesDbContext.Notes
                        .Where(n => n.user_id == userId)
                        .Count();

                    var notes = await _notesDbContext.Notes
                        .Where(n => n.user_id == userId)
                        .OrderByDescending(n => n.note_id)
                        .Take(4)
                        .ToListAsync();

                    var response = new
                    {
                        count = rowCount,
                        data = notes.Select(n => new { n.note_id, n.title, n.value }).ToList()
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
