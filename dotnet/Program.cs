using Google.Apis.Auth;
using Microsoft.Extensions.FileProviders;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// Configure port from environment variable
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");

var app = builder.Build();

// Serve static files from the '../public' directory
// This allows sharing the same frontend code as the Node.js example.
var publicPath = Path.Combine(builder.Environment.ContentRootPath, "../public");
if (Directory.Exists(publicPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(publicPath),
        RequestPath = ""
    });
    // Default file mapping (index.html)
    app.UseFileServer(new FileServerOptions
    {
        FileProvider = new PhysicalFileProvider(publicPath),
        EnableDirectoryBrowsing = false
    });
}
else
{
    Console.WriteLine($"Warning: '../public' directory not found at {publicPath}");
}

// IAP Token Verification Helper
async Task<object> VerifyIapToken(string iapJwt)
{
    try
    {
        // Decode the token to get the audience (to allow dynamic verification)
        // NOTE: In production, you should validate that 'aud' matches your specific Service/Client ID.
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(iapJwt);
        var audience = jwtToken.Audiences.FirstOrDefault();

        if (string.IsNullOrEmpty(audience))
        {
            throw new Exception("No audience claim found in token.");
        }

        // Verify using Google.Apis.Auth
        var payload = await JsonWebSignature.VerifySignedTokenAsync(
            iapJwt,
            new SignedTokenVerificationOptions
            {
                TrustedIssuers = { "https://cloud.google.com/iap" },
                TrustedAudiences = { audience },
                CertificatesUrl = "https://www.gstatic.com/iap/verify/public_key-jwk"
            }
        );

        return payload;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error verifying IAP token: {ex}");
        throw;
    }
}

app.MapGet("/api/data", async (HttpContext context) =>
{
    var iapJwt = context.Request.Headers["X-Goog-Iap-Jwt-Assertion"].ToString();

    if (!string.IsNullOrEmpty(iapJwt))
    {
        try
        {
            await VerifyIapToken(iapJwt);
            
            // We verify with Google lib, but extract claims from the standard .NET handler
            // because the Google payload class might not expose all IAP-specific claims easily.
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(iapJwt);
            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            var sub = jwtToken.Subject;

            return Results.Json(new
            {
                message = "Authenticated via IAP",
                user = email ?? sub,
                // We don't return the raw payload object here to avoid serialization issues with the library type
                claims = jwtToken.Claims.Select(c => new { c.Type, c.Value }) 
            });
        }
        catch
        {
            return Results.Json(new { error = "Invalid IAP Token" }, statusCode: 401);
        }
    }
    else
    {
        // Local Development / Mocking
        Console.WriteLine("No IAP header found. Assuming local development.");
        return Results.Json(new
        {
            message = "Local Development Mode (Mocked IAP)",
            user = "local-dev-user@example.com",
            raw_payload = new
            {
                sub = "mock-subject-id",
                email = "local-dev-user@example.com",
                iss = "mock-issuer"
            }
        });
    }
});

app.Run();