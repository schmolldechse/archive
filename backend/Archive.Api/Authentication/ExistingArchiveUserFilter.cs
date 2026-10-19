using Archive.Api.Exceptions;
using Archive.Core.Repositories.UserRepository;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Archive.Api.Authentication;

/// <summary>Validates private owner sessions without requiring registration completion.</summary>
public sealed class ExistingArchiveUserFilter(IUserRepository users) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (await users.GetByIdAsync(context.HttpContext.User.ReadUserId(), context.HttpContext.RequestAborted) is null)
            throw new InvalidSessionException();
        await next();
    }
}
