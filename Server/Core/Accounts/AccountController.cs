using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Core.Accounts.Codes;
using Server.Core.Accounts.Requests;
using Server.Database;
using Server.Web.Extensions;
using Server.Web.Models;

namespace Server.Core.Accounts;

[ApiController]
[Authorize]
[Route("api/{controller}")]
[Produces("application/json")]
public class AccountController(AppDbContext db, IConfiguration configuration) : Controller
{
    private readonly AccountManager _accountManager = new(db, configuration);

    [AllowAnonymous]
    [HttpPost]
    [Route("initialize-registration")]
    public async Task<IActionResult> InitializeRegistration(InitializeRegistrationRequest request)
    {
        if (!await _accountManager.InitiateRegistration(request.Email))
            return BadRequest(new ApiResponse(false, "An account with that email address already exists.", null));

        return Ok(
            new ApiResponse(
                true,
                "",
                null));
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("verify-registration")]
    public async Task<IActionResult> VerifyRegistration(VerifyRegistrationRequest request)
    {
        if (!await _accountManager.VerifyRegistration(request.Email, request.VerificationCode))
            return BadRequest(new ApiResponse(false, "Verification code expired or does not exist.", null));

        return Ok(
            new ApiResponse(
                true,
                null,
                null));
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("finalize-registration")]
    public async Task<IActionResult> FinalizeRegistration(FinalizeRegistrationRequest request)
    {
        if (!await _accountManager.FinalizeRegistration(request.Email, request.VerificationCode, request.Password))
            return BadRequest(new ApiResponse(false, "There was a problem creating your account.", null));

        return Ok(
            new ApiResponse(
                true,
                null,
                null));
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var clientInformation = await GetClientInformation(HttpContext);
        var authToken = await _accountManager.Login(request.Email, request.Password, clientInformation);
        if (authToken is null)
            return BadRequest(new ApiResponse(false, "There was a problem with your login credentials.", null));
        return Ok(new ApiResponse(true, null, new { Token = authToken }));
    }


    [HttpGet]
    [Route("details")]
    public async Task<IActionResult> GetDetails()
    {
        var accountGuid = this.GetAccountGuid(HttpContext);

        var accountDetails = await _accountManager.GetDetails(accountGuid);

        if (accountDetails is null)
            return BadRequest(new ApiResponse(false, "There was a problem with your login credentials.", null));

        return Ok(
            new ApiResponse(
                true,
                null,
                accountDetails
            )
        );
    }

    [HttpGet]
    [Route("login-history")]
    public async Task<IActionResult> GetLoginHistory()
    {
        var accountGuid = this.GetAccountGuid(HttpContext);
        var loginHistory = await _accountManager.GetLoginHistory(accountGuid);

        var simplifiedHistory = new
        {
            loginHistory = loginHistory.Select(authLog => new
            {
                authLog.Timestamp,
                authLog.IPAddress,
                Location = $"{authLog.Region}, {authLog.Country}",
                Device = authLog.UserAgent
            }).ToList()
        };

        return Ok(
            new ApiResponse(
                true,
                null,
                simplifiedHistory
            )
        );
    }

    [HttpPost]
    [Route("get-code/initial-email/{email}")]
    public async Task<IActionResult> GetInitialEmailCode(string email)
    {
        var wasEmailSent = await _accountManager.SendCode(CodeType.InitialEmail, email);
        return Ok(new ApiResponse(wasEmailSent, null, null));
    }

    [HttpGet]
    [Route("get-code/current-email")]
    public async Task<IActionResult> GetCurrentEmailCode()
    {
        var accountGuid = this.GetAccountGuid(HttpContext);
        var account = await _accountManager.GetByGuid(accountGuid);
        if (account is null) return Ok(new ApiResponse(false, "Account not found.", null));
        var wasEmailSent = await _accountManager.SendCode(CodeType.CurrentEmail, account.Email);
        return Ok(new ApiResponse(wasEmailSent, null, null));
    }

    [HttpPost]
    [Route("change-email")]
    public async Task<IActionResult> ChangeEmail(ChangeEmailRequest request)
    {
        var accountGuid = this.GetAccountGuid(HttpContext);
        var (success, message) = await _accountManager.ChangeEmail(accountGuid, request.NewEmail, request.NewEmailCode,
            request.CurrentEmailCode);
        return Ok(new ApiResponse(success, message, null));
    }

    [HttpPost]
    [Route("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var accountGuid = this.GetAccountGuid(HttpContext);
        var (success, message) =
            await _accountManager.ChangePassword(accountGuid, request.CurrentPassword, request.NewPassword);
        return Ok(new ApiResponse(success, message, null));
    }

    [HttpGet]
    [Route("get-code/new-email/{email}")]
    public async Task<IActionResult> GetNewEmailCode(string email)
    {
        var wasEmailSent = await _accountManager.SendCode(CodeType.NewEmail, email);
        return Ok(new ApiResponse(wasEmailSent, null, null));
    }

    private async Task<ClientInformation> GetClientInformation(HttpContext context)
    {
        var clientInformation = new ClientInformation
        {
            IPAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            UserAgent = context.Request.Headers.UserAgent.ToString()
        };
        // call api for location data
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        var response = httpClient.GetAsync("http://ip-api.com/json/107.179.170.70").Result;
        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var locationData = JsonSerializer.Deserialize<LocationResponse>(responseBody);
            clientInformation.Country = locationData?.Country ?? "Unknown Country";
            clientInformation.Region = locationData?.Region ?? "Unknown Region";
        }

        httpClient.Dispose();
        return clientInformation;
    }
}