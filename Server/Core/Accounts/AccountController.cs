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

    /// <summary>
    ///     Initializes the user registration process by sending an email to the provided address for email verification.
    /// </summary>
    /// <param name="request">The <see cref="InitializeRegistrationRequest" /> containing a new users email.</param>
    /// <returns>
    ///     An <see cref="IActionResult" /> representing the result of the registration initialization.
    ///     If successful, returns an HTTP 200 OK response with a success message.
    ///     If unsuccessful, returns an HTTP 400 Bad Request response with an error message.
    /// </returns>
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

    /// <summary>
    ///     Verifies the user registration by validating the provided email and verification code.
    /// </summary>
    /// <param name="request">
    ///     The <see cref="VerifyRegistrationRequest" /> containing a user email and registration
    ///     verification code.
    /// </param>
    /// <returns>
    ///     An <see cref="IActionResult" /> representing the result of the registration verification.
    ///     If successful, returns an HTTP 200 OK response with a success message.
    ///     If unsuccessful, returns an HTTP 400 Bad Request response with an error message.
    /// </returns>
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

    /// <summary>
    ///     Finalizes the user registration process using a verified email address and password.
    /// </summary>
    /// <param name="request">
    ///     The <see cref="FinalizeRegistrationRequest" /> containing user email, verification code, and
    ///     password.
    /// </param>
    /// <returns>
    ///     An <see cref="IActionResult" /> representing the result of the final registration step.
    ///     If successful, returns an HTTP 200 OK response with a success message.
    ///     If unsuccessful, returns an HTTP 400 Bad Request response with an error message.
    /// </returns>
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

    /// <summary>
    ///     Handles user login by validating the provided email and password, generating an authentication token upon success.
    /// </summary>
    /// <param name="request">The <see cref="LoginRequest" /> containing user email and password.</param>
    /// <returns>
    ///     An <see cref="IActionResult" /> representing the result of the login attempt.
    ///     If successful, returns an HTTP 200 OK response with a success message and an authentication token.
    ///     If unsuccessful, returns an HTTP 400 Bad Request response with an error message.
    /// </returns>
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

    /// <summary>
    ///     Gets an accounts details, including a masked email, date registered, and last login.
    /// </summary>
    /// <returns>
    ///     An <see cref="IActionResult" /> representing the result of the login attempt.
    ///     If successful, returns an HTTP 200 OK response with a success message and the accounts details.
    ///     If unsuccessful, returns an HTTP 400 Bad Request response with an error message.
    /// </returns>
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

    /// <summary>
    ///     Gets an accounts login history.
    /// </summary>
    /// <returns>
    ///     An <see cref="IActionResult" /> representing the result of the login attempt.
    ///     If successful, returns an HTTP 200 OK response with a success message an the login history.
    ///     If unsuccessful, returns an HTTP 400 Bad Request response with an error message.
    /// </returns>
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

    /// <summary>
    ///     Changes an accounts email.
    /// </summary>
    /// <param name="request">
    ///     A <see cref="ChangeEmailRequest" /> DTO containing an accounts new mail, and a verification
    ///     code for both the accounts current email and new email.
    /// </param>
    /// <returns>
    ///     An <see cref="IActionResult" /> representing the result of changing an accounts email
    ///     If successful, returns an HTTP 200 OK response with a success message.
    ///     If unsuccessful, returns an HTTP 400 Bad Request response with an error message.
    /// </returns>
    [HttpPost]
    [Route("change-email")]
    public async Task<IActionResult> ChangeEmail(ChangeEmailRequest request)
    {
        var accountGuid = this.GetAccountGuid(HttpContext);
        var (success, message) = await _accountManager.ChangeEmail(accountGuid, request.NewEmail, request.NewEmailCode,
            request.CurrentEmailCode);
        return Ok(new ApiResponse(success, message, null));
    }

    /// <summary>
    ///     Changes an accounts password.
    /// </summary>
    /// <param name="request">
    ///     A <see cref="ChangePasswordRequest" /> DTO containing an accounts current password and
    ///     new password.
    /// </param>
    /// <returns>
    ///     An <see cref="IActionResult" /> representing the result of changing an accounts password..
    ///     If successful, returns an HTTP 200 OK response with a success message.
    ///     If unsuccessful, returns an HTTP 400 Bad Request response with an error message.
    /// </returns>
    [HttpPost]
    [Route("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var accountGuid = this.GetAccountGuid(HttpContext);
        var (success, message) =
            await _accountManager.ChangePassword(accountGuid, request.CurrentPassword, request.NewPassword);
        return Ok(new ApiResponse(success, message, null));
    }

    /// <summary>
    ///     Sends an email verification code to the accounts current email
    /// </summary>
    /// <returns>
    ///     An <see cref="IActionResult" /> representing the result of sending the verification code.
    ///     If successful, returns an HTTP 200 OK response with a success message.
    ///     If unsuccessful, returns an HTTP 400 Bad Request response with an error message.
    /// </returns>
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

    /// <summary>
    ///     Sends an email verification code to a new email address.
    /// </summary>
    /// <returns>
    ///     An <see cref="IActionResult" /> representing the result of sending the verification code.
    ///     If successful, returns an HTTP 200 OK response with a success message.
    ///     If unsuccessful, returns an HTTP 400 Bad Request response with an error message.
    /// </returns>
    [HttpGet]
    [Route("get-code/new-email/{email}")]
    public async Task<IActionResult> GetNewEmailCode(string email)
    {
        var wasEmailSent = await _accountManager.SendCode(CodeType.NewEmail, email);
        return Ok(new ApiResponse(wasEmailSent, null, null));
    }

    /// <summary>
    ///     Retrieves client information including IP address, user agent, and location data.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext" /> providing information about the current request.</param>
    /// <returns>
    ///     A <see cref="ClientInformation" /> DTO containing the client information.
    /// </returns>
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