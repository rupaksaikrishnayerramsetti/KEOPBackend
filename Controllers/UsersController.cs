using KEOPBackend.Data;
using KEOPBackend.helpers.EmailServices;
using KEOPBackend.helpers.Passwords;
using KEOPBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Microsoft.Data.SqlClient;

namespace KEOPBackend.Controllers
{
    [ApiController]
    public class UsersController : ControllerBase
    {
        string ConnectionString = "Data Source=.\\sqlexpress;Initial Catalog=KEAOP;Integrated Security=True;Encrypt=False;Trust Server Certificate=True";
        private readonly UsersDbContext _userDbContext;
        private readonly IConfiguration _configuration;
        private const string JwtSecretKey = "kzUf4sxss4AeG5uHkNZAqT1Nyi1zVfpz"; // Replace with your actual JWT secret key

        public UsersController(UsersDbContext userDbContext, IConfiguration configuration)
        {
            _userDbContext = userDbContext;
            _configuration = configuration;
        }

        private string GenerateJwtToken(Users user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(JwtSecretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim("user_id", user.user_id.ToString()),
                    new Claim(ClaimTypes.Email, user.email)
                }),
                Expires = DateTime.UtcNow.AddDays(1), // Token expiration time
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        [Route("Users")]
        [HttpGet]
        public async Task<IEnumerable<Users>> Get() => await _userDbContext.Users.ToListAsync();

        [Route("create_user")]
        [HttpPost]
        public async Task<IActionResult> CreateUsers([FromBody] Users users)
        {
            if (_userDbContext.Users.Any(u => u.email == users.email))
            {
                return Ok(false);
            }
            try
            {
                Passwords passwordObj = new Passwords();
                string password = passwordObj.GeneratePassword();
                string hashedPassword = passwordObj.GenerateDigest(password);
                users.password_digest = hashedPassword;
                users.created_on = DateTime.UtcNow;
                await _userDbContext.Users.AddAsync(users);
                await _userDbContext.SaveChangesAsync();
                EmailSending emailSending = new EmailSending();
                var template = emailSending.AccountCreationTemplate(users.email, password);
                emailSending.SendEmailAsync(users.email, "These are the credentials for Keep Everything at One Place", template);
                return Ok(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Ok(false);
            }
        }

        [Route("login_check")]
        [HttpPost]
        public async Task<IActionResult> LoginCheck()
        {
            try
            {
                var content = await new StreamReader(Request.Body).ReadToEndAsync();
                dynamic requestData = Newtonsoft.Json.JsonConvert.DeserializeObject(content);

                Passwords passwordObj = new Passwords();
                var email = (string)requestData.email;
                var pass = (string)requestData.password;
                var passwordDigest = passwordObj.GenerateDigest(pass);

                var user = await _userDbContext.Users
                    .FirstOrDefaultAsync(u => u.email == email && u.password_digest == passwordDigest);

                if (user != null)
                {
                    var token = GenerateJwtToken(user);
                    return Ok(token);
                }
                return Ok("invalid");
                //return NotFound("Invalid credentials");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [Route("UpdateUserData")]
        [HttpPost]
        public async Task<IActionResult> UpdateUserData()
        {
            try
            {
                var content = await new StreamReader(Request.Body).ReadToEndAsync();
                dynamic requestData = Newtonsoft.Json.JsonConvert.DeserializeObject(content);

                // Validate required properties
                if (requestData.token == null ||
                    requestData.email == null ||
                    requestData.gender == null ||
                    requestData.occupation == null ||
                    requestData.phone_number == null ||
                    requestData.user_name == null ||
                    requestData.salary == null)
                {
                    return BadRequest("Required properties are missing in the request.");
                }

                string jwtToken = (string)requestData.token;
                Users users = new Users
                {
                    email = (string)requestData.email,
                    gender = (string)requestData.gender,
                    occupation = (string)requestData.occupation,
                    phone_number = (string)requestData.phone_number,
                    user_name = (string)requestData.user_name,
                    modified_on = DateTime.UtcNow,
                };
                if (!int.TryParse((string)requestData.salary, out int parsedSalary))
                {
                    return BadRequest("Invalid format for the salary property.");
                }

                users.salary = parsedSalary;
                if (string.IsNullOrEmpty(jwtToken))
                {
                    return BadRequest("Authorization token is missing");
                }
                var tokenHandler = new JwtSecurityTokenHandler();
                var token = jwtToken.Replace("Bearer ", "");
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
                    claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);
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
                var existingUser = await _userDbContext.Users.FirstOrDefaultAsync(u => u.email == userEmail);
                if (existingUser != null)
                {
                    existingUser.user_name = users.user_name;
                    existingUser.gender = users.gender;
                    existingUser.occupation = users.occupation;
                    existingUser.phone_number = users.phone_number;
                    existingUser.salary = users.salary;
                    existingUser.created_on = DateTime.UtcNow;
                    _userDbContext.Entry(existingUser).State = EntityState.Modified;
                    await _userDbContext.SaveChangesAsync();

                    return Ok(true);
                }
                else
                {
                    return NotFound("User not found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Ok(false);
            }
        }

        [Route("changePassword")]
        [HttpPost]
        public async Task<IActionResult> ChangePassword()
        {
            try
            {
                var content = await new StreamReader(Request.Body).ReadToEndAsync();
                dynamic requestData = Newtonsoft.Json.JsonConvert.DeserializeObject(content);

                string jwtToken = (string)requestData.token;

                Passwords pass = new Passwords();
                string oldPassword = (string)requestData.password;
                string newPassword = (string)requestData.newpassword;

                if (string.IsNullOrEmpty(jwtToken))
                {
                    return BadRequest("Authorization token is missing");
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var token = jwtToken.Replace("Bearer ", "");
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
                    claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);
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
                var oldPasswordDigest = pass.GenerateDigest(oldPassword);
                var existingUser = await _userDbContext.Users.FirstOrDefaultAsync(u => u.email == userEmail && u.password_digest == oldPasswordDigest);

                if (existingUser != null)
                {
                    existingUser.password_digest = pass.GenerateDigest(newPassword);
                    existingUser.modified_on = DateTime.UtcNow;
                    _userDbContext.Entry(existingUser).State = EntityState.Modified;
                    await _userDbContext.SaveChangesAsync();

                    EmailSending emailSending = new EmailSending();
                    var template = emailSending.AccountCreationTemplate(userEmail, newPassword);
                    await emailSending.SendEmailAsync(userEmail, "Changed credentials for User account of Keep Everything at One Place", template);

                    return Ok(true);
                }
                else
                {
                    return Ok(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during ChangePassword: {ex.Message}");
                return StatusCode(500, "Internal Server Error");
            }
        }


        //[Authorize]
        [Route("fetchUserDetails")]
        [HttpGet]
        public async Task<IActionResult> FetchUserDetails()
        {
            try
            {
                string jwtToken = Request.Headers["Authorization"];

                if (string.IsNullOrEmpty(jwtToken))
                {
                    return BadRequest("Authorization token is missing");
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var token = jwtToken.Replace("Bearer ", "");

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
                    claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);
                }
                catch (SecurityTokenValidationException ex)
                {
                    // Log the exception details
                    Console.WriteLine($"Token validation failed: {ex.Message}");
                    return Unauthorized("Token validation failed");
                }

                var userIdClaim = claimsPrincipal.FindFirst("user_id");
                var emailClaim = claimsPrincipal.FindFirst(ClaimTypes.Email);

                if (userIdClaim != null && emailClaim != null)
                {
                    var userId = userIdClaim.Value;
                    var email = emailClaim.Value;
                    using(var connection = new SqlConnection(ConnectionString))
                    {
                        await connection.OpenAsync();
                        var query = "select * from users where email=@Email";
                        using(var command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Email", email);
                            using(var reader = await command.ExecuteReaderAsync())
                            {
                                if(await reader.ReadAsync())
                                {
                                    return Ok(new { user_id = reader["user_id"], email = reader["email"], user_name = reader["user_name"], gender = reader["gender"], occupation = reader["occupation"], phone_number = reader["phone_number"], salary = reader["salary"] });
                                }
                            }
                        }
                    }
                    //return Ok(new { UserId = userId, Email = email });
                }

                return BadRequest("Invalid token claims");
            }
            catch (Exception ex)
            {
                // Log the exception details
                Console.WriteLine($"Exception during FetchUserDetails: {ex.Message}");
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }
    }
}