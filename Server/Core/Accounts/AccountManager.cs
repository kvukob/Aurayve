using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.Core.Accounts.Codes;
using Server.Core.Accounts.Models;
using Server.Database;
using Server.Logging;
using Server.Services.Email;
using Server.Web.Models;

namespace Server.Core.Accounts;

public class AccountManager(AppDbContext db, IConfiguration configuration)
{
    private readonly EmailService _emailService = new(configuration);

    /// <summary>
    /// Retrieves an account from the database based on the GUID.
    /// </summary>
    /// <param name="guid">The GUID associated with the account to be retrieved.</param>
    /// <returns>
    /// An <see cref="Account"/>.
    /// The task result is the retrieved <see cref="Account"/> or <c>null</c> if no matching account is found.
    /// </returns>
    public async Task<Account?> GetByGuid(Guid guid)
    {
        return await db.Accounts.FirstOrDefaultAsync(a => a.Guid == guid);
    }

    /// <summary>
    /// Retrieves an accounts details based on the GUID.
    /// </summary>
    /// <param name="accountGuid">The GUID associated with the account to be retrieved.</param>
    /// <returns>
    /// An <see cref="Account"/>.
    /// The task result is the retrieved <see cref="AccountDetails"/> or <c>null</c> if no matching account is found.
    /// </returns>
    public async Task<AccountDetails?> GetDetails(Guid accountGuid)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Guid == accountGuid);
        if (account is null)
            return null;
        var details = new AccountDetails
        {
            MaskedEmail = Account.MaskEmail(account.Email),
            DateRegistered = account.DateRegistered,
            LastLogin = db.AuthLogs.Where(a => a.Account == account)
                .OrderByDescending(l => l.Timestamp).Take(2).LastAsync().Result.Timestamp
        };
        return details;
    }

    /// <summary>
    /// Retrieves an accounts login history based on the GUID.
    /// </summary>
    /// <param name="accountGuid">The GUID associated with the account to be retrieved.</param>
    /// <returns>
    /// An <see cref="AuthLog"/> list.
    /// The task result is the retrieved <see cref="AuthLog"/> list or <c>null</c> if no matching account is found.
    /// </returns>
    public async Task<IEnumerable<AuthLog>> GetLoginHistory(Guid accountGuid)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Guid == accountGuid);
        if (account is null)
            return new List<AuthLog>();
        var authLogItems = await db.AuthLogs.Where(log => log.Account == account)
            .OrderByDescending(l => l.Timestamp).Take(30)
            .ToListAsync();
        return authLogItems;
    }

    /// <summary>
    ///     c
    ///     Authenticates a user by verifying the provided <paramref name="email" /> and <paramref name="password" />.
    /// </summary>
    /// <param name="email">The email address of the user attempting to log in.</param>
    /// <param name="password">The password provided for authentication.</param>
    /// <param name="clientInformation">Contains client information for the request.</param>
    /// <returns>
    ///     A tuple containing the authentication token and a boolean indicating whether the login was successful.
    ///     If the authentication fails (due to an incorrect email, password, or other reasons), returns null.
    /// </returns>
    /// <remarks>
    ///     This method attempts to find an account with the specified email in the Accounts DbSet.
    ///     If the account is not found, the method returns null, indicating authentication failure.
    ///     If the account is found, the provided password is verified using the PasswordHasher.
    ///     If the password verification fails, the authentication event is logged, and the method returns null.
    ///     The method returns a tuple containing the generated token and a boolean indicating whether the email is verified.
    /// </remarks>
    public async Task<string?> Login(string email, string password, ClientInformation clientInformation)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(account => account.Email == email);
        if (account is null) return null;

        var hasher = new PasswordHasher<Account>();
        if (hasher.VerifyHashedPassword(account, account.HashedPassword, password) == PasswordVerificationResult.Failed)
        {
            await LogAuthEvent(account, clientInformation, false, "Password Verification Failed");
            return null;
        }

        var token = GenerateJwtToken(account);

        await LogAuthEvent(account, clientInformation, true, "Login Successful");
        await db.SaveChangesAsync();

        return token;
    }

    /// <summary>
    /// Initiates account creation by sending an email address a verification code.
    /// </summary>
    /// <param name="email">The email address of which to send the code to.</param>
    /// <returns>
    /// The task result is a boolean, returning true if the email was sent or false if something went wrong.
    /// </returns>
    public async Task<bool> InitiateRegistration(string email)
    {
        // Check if account already exists
        var existingAccount = await db.Accounts.FirstOrDefaultAsync(a => a.Email == email);
        if (existingAccount is not null) return false;

        // Send verification email
        return await SendCode(CodeType.InitialEmail, email);
    }
    
    /// <summary>
    /// Verifies a verification code and email against the stored verification log in the database.
    /// </summary>
    /// <param name="email">The email address of which to send the code to.</param>
    /// <param name="verificationCode">A verification code.</param>
    /// <returns>
    /// The task result is a boolean, returning true if the code matches the email.
    /// </returns>
    public async Task<bool> VerifyRegistration(string email, string verificationCode)
    {
        // Check if account already exists
        var existingAccount = await db.Accounts.FirstOrDefaultAsync(a => a.Email == email);
        if (existingAccount is not null) return false;

        var verificationLog =
            await db.CodeLogs.FirstOrDefaultAsync(
                vC => vC.Code == verificationCode &&
                      vC.Email == email &&
                      vC.Type == CodeType.InitialEmail
            );
        return verificationLog is not null;
    }

    /// <summary>
    /// Finishes registering a new account.
    /// </summary>
    /// <param name="email">The email address for the new account.</param>
    /// <param name="verificationCode">A verification code.</param>
    /// <param name="password">The password for the new account.</param>
    /// <returns>
    /// The task result is a boolean, returning true if the account has been registered.
    /// </returns>
    public async Task<bool> FinalizeRegistration(string email, string verificationCode, string password)
    {
        // Check if account already exists
        var existingAccount = await db.Accounts.FirstOrDefaultAsync(a => a.Email == email);
        if (existingAccount is not null) return false;

        var verificationLog =
            await db.CodeLogs.FirstOrDefaultAsync(
                vC => vC.Code == verificationCode &&
                      vC.Email == email &&
                      vC.Type == CodeType.InitialEmail
            );
        if (verificationLog is null)
            return false;
        db.CodeLogs.Remove(verificationLog);

        var account = new Account { Email = email };

        var hasher = new PasswordHasher<Account>();
        account.HashedPassword = hasher.HashPassword(account, password);

        await db.Accounts.AddAsync(account);
        return await db.SaveChangesAsync() == 2;
    }

    /// <summary>
    /// Changes an email address for a specified account.
    /// </summary>
    /// <param name="accountGuid">The GUID representing the account.</param>
    /// <param name="newEmail">The new email address for the account.</param>
    /// <param name="newEmailCode">The verification code for the new email.</param>
    /// <param name="currentEmailCode">The verification code for the accounts current email.</param>
    /// <returns>
    /// The task result is a boolean, returning if the email has been changed with an error message if applicable. 
    /// </returns>
    public async Task<Tuple<bool, string>> ChangeEmail(Guid accountGuid, string newEmail, string newEmailCode,
        string currentEmailCode)
    {
        var account = await GetByGuid(accountGuid);
        if (account is null)
            return new Tuple<bool, string>(false, "Account not found.");

        var newEmailCodeLog =
            await db.CodeLogs.FirstOrDefaultAsync(log => log.Email == newEmail && log.Code == newEmailCode);
        if (newEmailCodeLog is null)
            return new Tuple<bool, string>(false, "Verification code for new email not found.");
        if (newEmailCodeLog.ExpirationDate < DateTime.UtcNow)
            return new Tuple<bool, string>(false, "Verification code for new email has expired.");

        var currentEmailCodeLog =
            await db.CodeLogs.FirstOrDefaultAsync(log => log.Email == account.Email && log.Code == currentEmailCode);
        if (currentEmailCodeLog is null)
            return new Tuple<bool, string>(false, "Verification code for current email not found.  Please try again.");
        if (currentEmailCodeLog.ExpirationDate < DateTime.UtcNow)
            return new Tuple<bool, string>(false,
                "Verification code for current email has expired.  Please try again.");

        account.Email = newEmail;

        db.CodeLogs.Remove(newEmailCodeLog);
        db.CodeLogs.Remove(currentEmailCodeLog);
        await db.SaveChangesAsync();

        return new Tuple<bool, string>(true, "Your email has been changed successfully.");
    }

    /// <summary>
    /// Changes an password for a specified account.
    /// </summary>
    /// <param name="accountGuid">The GUID representing the account.</param>
    /// <param name="currentPassword">The current password for the account.</param>
    /// <param name="newPassword">The new password for the account.</param>
    /// <returns>
    /// The task result is a boolean, returning if the password has been changed with an error message if applicable. 
    /// </returns>
    public async Task<Tuple<bool, string>> ChangePassword(Guid accountGuid, string currentPassword, string newPassword)
    {
        var account = await GetByGuid(accountGuid);
        if (account is null)
            return new Tuple<bool, string>(false, "Account not found.");

        // Verify current password
        var hasher = new PasswordHasher<Account>();
        if (hasher.VerifyHashedPassword(account, account.HashedPassword, currentPassword) ==
            PasswordVerificationResult.Failed)
            return new Tuple<bool, string>(false, "Current password is incorrect.");

        account.HashedPassword = hasher.HashPassword(account, newPassword);
        db.Accounts.Update(account);
        await db.SaveChangesAsync();
        return new Tuple<bool, string>(true, "Your password has been changed successfully.");
    }


    /// <summary>
    ///     Logs an authentication event for the specified <paramref name="account" />.
    /// </summary>
    /// <param name="account">The user account for which the authentication event is logged.</param>
    /// <param name="clientInformation">Contains client information for the request.</param>
    /// <param name="loginSuccessful">A flag indicating whether the login attempt was successful.</param>
    /// <param name="details">Additional details or information about the authentication event.</param>
    /// <remarks>
    ///     This method creates an <see cref="AuthLog" /> instance with information about the authentication event.
    ///     The created log item is then added asynchronously to the AuthLogs DbSet in the database context.
    /// </remarks>
    private async Task LogAuthEvent(Account account, ClientInformation clientInformation, bool loginSuccessful,
        string details)
    {
        var logItem = new AuthLog
        {
            Account = account,
            IPAddress = clientInformation.IPAddress,
            Country = clientInformation.Country,
            Region = clientInformation.Region,
            UserAgent = clientInformation.UserAgent,
            LoginSuccessful = loginSuccessful,
            Details = details,
            Timestamp = DateTime.UtcNow
        };
        await db.AuthLogs.AddAsync(logItem);
    }

    /// <summary>
    ///     Generates a JSON Web Token (JWT) for the specified <paramref name="account" />.
    /// </summary>
    /// <param name="account">The user account for which the JWT is generated.</param>
    /// <returns>The JWT as a string.</returns>
    /// <remarks>
    ///     This method uses a secret key retrieved from the application configuration to sign the JWT.
    ///     The JWT includes a claim with the user's unique identifier (GUID).
    ///     The token is set to expire after 7 days from the current UTC time.
    /// </remarks>
    private string GenerateJwtToken(Account account)
    {
        var secretKey = configuration.GetValue<string>("AppSettings:Secret");
        if (secretKey is null) return string.Empty;
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(secretKey);
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, account.Guid.ToString())
        };
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    ///     Sends a verification code to a provided email address to validate them as the email owner.
    /// </summary>
    /// <param name="codeType">The type of code to generate</param>
    /// <param name="email">The email address to send the code.</param>
    /// <returns>The JWT as a string.</returns>
    public async Task<bool> SendCode(CodeType codeType, string email)
    {
        // Check, if exists, previous code of same type and removes it regardless of expiry
        var previousCode = await db.CodeLogs.FirstOrDefaultAsync(log => log.Type == codeType && log.Email == email);
        if (previousCode is not null) db.CodeLogs.Remove(previousCode);

        var verificationCode = GenerateVerificationCode();
        var htmlBuilder = new StringBuilder();
        switch (codeType)
        {
            case CodeType.InitialEmail:
                htmlBuilder.AppendLine(
                    "<p>Welcome!</p>");
                htmlBuilder.AppendLine(
                    "<p>Please enter the code below to verify your email address.</p>");
                break;
            case CodeType.CurrentEmail:
                htmlBuilder.AppendLine(
                    "<p>Please enter the code below to verify your email address.</p>");
                break;
            case CodeType.NewEmail:
                htmlBuilder.AppendLine(
                    "<p>Please enter the code below to verify your new email address.</p>");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(codeType), codeType, null);
        }

        htmlBuilder.AppendLine(
            $"<p><br/>Your verification code is: <div style='border: 1px dotted #623CEA; padding: 10px; border-radius: 5px;'>{verificationCode}</div></p>");
        var emailBodyInsert = htmlBuilder.ToString();
        await _emailService.SendEmailAsync("kvukob@gmail.com", $"{email}", emailBodyInsert);
        var verificationLog = new CodeLog
        {
            Code = verificationCode,
            Email = email,
            Type = codeType,
            ExpirationDate = SetCodeExpiration(codeType)
        };
        await db.CodeLogs.AddAsync(verificationLog);
        await db.SaveChangesAsync();
        return true;
    }
    
    /// <summary>
    ///     Returns the expiry time based on a provided CodeType.
    /// </summary>
    /// <param name="codeType">The type of code.</param>
    /// <returns>A DateTime object containing the expiry time.</returns>
    private static DateTime SetCodeExpiration(CodeType codeType)
    {
        return codeType switch
        {
            CodeType.InitialEmail => DateTime.UtcNow.AddDays(1),
            CodeType.NewEmail => DateTime.UtcNow.AddSeconds(120),
            CodeType.CurrentEmail => DateTime.UtcNow.AddSeconds(120),
            _ => throw new ArgumentOutOfRangeException(nameof(codeType), codeType, null)
        };
    }
    /// <summary>
    ///     Generates and returns a verification code.
    /// </summary>
    /// <param name="length">The length of code.</param>
    /// <returns>A string containing the generated code.</returns>
    private static string GenerateVerificationCode(int length = 6)
    {
        const string characters = "0123456789";
        var random = new Random();

        var code = new char[length];

        for (var i = 0; i < length; i++) code[i] = characters[random.Next(characters.Length)];

        return new string(code);
    }
}