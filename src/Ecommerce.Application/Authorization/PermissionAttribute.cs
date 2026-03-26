using Microsoft.AspNetCore.Authorization;

namespace Ecommerce.Application.Authorization
{
    public class PermissionAttribute : AuthorizeAttribute
    {
        public PermissionAttribute(string permission)
        {
            Policy = permission;
        }
    }
}
