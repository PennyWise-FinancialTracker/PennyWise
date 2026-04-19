using System.Security.Claims;
using PennyWise.Data.Entities;

namespace PennyWise.Data;

public static class AppUserClaims
{
    public static ClaimsPrincipal Create(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
        };

        var identity = new ClaimsIdentity(claims, "PennyWiseCookie");
        return new ClaimsPrincipal(identity);
    }
}
