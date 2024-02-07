using KEOPBackend.Data;
using KEOPBackend.helpers.SpentUtility;
using KEOPBackend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static Azure.Core.HttpHeader;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace KEOPBackend.Controllers
{
    [ApiController]
    public class SpentAnalysisController : ControllerBase
    {
        private readonly SpentAnalysisDbContext _spentAnalysisDbContext;
        private readonly UsersDbContext _usersDbContext;
        private readonly IConfiguration _configuration;
        public SpentAnalysisController(SpentAnalysisDbContext spentAnalysisDbContext, UsersDbContext usersDbContext, IConfiguration configuration)
        {
            _spentAnalysisDbContext = spentAnalysisDbContext;
            _usersDbContext = usersDbContext;
            _configuration = configuration;
        }

        [Route("FetchCurrentMonthRecord")]
        [HttpGet]
        public async Task<IActionResult> FetchCurrentMonthRecord()
        {
            try
            {
                var currentMonthYear = DateTime.Now.ToString("MMMM-yyyy");
                var jwtToken = Request.Headers["Authorization"];
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
                SpentUtility spentUtility = new SpentUtility(_usersDbContext, _spentAnalysisDbContext);
                var userIdClaim = claimsPrincipal.FindFirst("user_id");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    var result = await _spentAnalysisDbContext.SpentAnalyses
                        .Where(s => s.user_id == userId)
                        .Select(s => s.spent_data)
                        .ToListAsync();
                    if (result.IsNullOrEmpty())
                    {
                        SpentAnalysis data = new SpentAnalysis
                        {
                            spent_data = await spentUtility.CreateJson(userId),
                            user_id = userId,
                            created_on = DateTime.UtcNow
                        };
                        await _spentAnalysisDbContext.SpentAnalyses.AddAsync(data);
                        await _spentAnalysisDbContext.SaveChangesAsync();
                        return Ok(JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(data.spent_data));
                    }
                    else
                    {
                        var transformedResult = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(result[0]);
                        string curMonthYear = DateTime.Now.ToString("MMMM-yyyy");
                        if (transformedResult.ContainsKey(curMonthYear))
                        {
                            var result3 = new Dictionary<string, object>
                                {
                                    { curMonthYear, transformedResult[curMonthYear] }
                                };
                            return Ok(result3);
                        }
                        else
                        {
                            var result1 = spentUtility.JsonForCurrentMonth(userId, transformedResult);
                            SpentAnalysis spentAnalysis = new SpentAnalysis
                            {
                                spent_data = JsonConvert.SerializeObject(result1),
                                user_id = userId,
                                modified_on = DateTime.UtcNow
                            };
                            _spentAnalysisDbContext.Entry(spentAnalysis).State = EntityState.Modified;
                            await _spentAnalysisDbContext.SaveChangesAsync();
                            return Ok(result1);
                        }
                    }
                }
                return BadRequest("invalid");
            }
            catch 
            {
                return BadRequest("No data found");
            }
        }

        [Route("FetchTotalSalary")]
        [HttpGet]
        public async Task<IActionResult> FetchTotalSalary()
        {
            try
            {
                var currentMonthYear = DateTime.Now.ToString("MMMM-yyyy");
                var jwtToken = Request.Headers["Authorization"];
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
                SpentUtility spentUtility = new SpentUtility(_usersDbContext, _spentAnalysisDbContext);
                var userIdClaim = claimsPrincipal.FindFirst("user_id");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    var salary = await _usersDbContext.Users.Where(u => u.user_id == userId).Select(u => u.salary).FirstOrDefaultAsync();
                    return Ok(salary);
                }
                return Ok(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception arised while executing FetchTotalSalary method ", ex.ToString());
                return BadRequest(ex.ToString());
            }
        }

        [Route("UpdateSpentRecord")]
        [HttpPost]
        public async Task<IActionResult> UpdateSpentRecord()
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

                SpentUtility spentUtility = new SpentUtility(_usersDbContext, _spentAnalysisDbContext);
                var userIdClaim = claimsPrincipal.FindFirst("user_id");

                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    if (int.TryParse(requestData.amount?.ToString(), out int amount))
                    {
                        string currentMonthYear = DateTime.Now.ToString("MMMM-yyyy");
                        var completeRecord = await _spentAnalysisDbContext.SpentAnalyses
                            .Where(s => s.user_id == userId)
                            .FirstOrDefaultAsync();

                        if (completeRecord == null)
                        {
                            completeRecord = new SpentAnalysis
                            {
                                user_id = userId,
                                modified_on = DateTime.UtcNow
                            };
                            _spentAnalysisDbContext.SpentAnalyses.Add(completeRecord);
                        }

                        var jsonCompleteRecord = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(completeRecord.spent_data ?? "{}");
                        var spentType = (string)requestData.spenttype;
                        string savings = "Savings";

                        if (!jsonCompleteRecord.ContainsKey(currentMonthYear))
                        {
                            var salary = await _usersDbContext.Users.Where(u => u.user_id == userId).Select(u => u.salary).FirstOrDefaultAsync();
                            jsonCompleteRecord[currentMonthYear] = new Dictionary<string, int>
                    {
                        { "PG", 0 },
                        { "Food", 0 },
                        { "Bills", 0 },
                        { "Traveling", 0 },
                        { "Shopping", 0 },
                        { "Other", 0 },
                        { "Savings", salary }
                    };
                        }

                        jsonCompleteRecord[currentMonthYear][spentType] += amount;
                        jsonCompleteRecord[currentMonthYear][savings] -= amount;

                        completeRecord.spent_data = JsonConvert.SerializeObject(jsonCompleteRecord);
                        completeRecord.modified_on = DateTime.UtcNow;

                        await _spentAnalysisDbContext.SaveChangesAsync();

                        return Ok(true);
                    }
                    else
                    {
                        return BadRequest("Invalid amount provided");
                    }
                }
                else
                {
                    return BadRequest("Invalid user ID claim");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return BadRequest(ex.Message);
            }
        }

    }
}
