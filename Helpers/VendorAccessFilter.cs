using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace TestLandingPageNet8.Helpers
{
    public class VendorAccessFilter : IAsyncPageFilter
    {
        public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
        {
            return Task.CompletedTask;
        }

        public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                var role = user.FindFirstValue(ClaimTypes.Role);
                
                // Get the route/page path (e.g. /Index, /UnitList/UnitList)
                var pagePath = context.ActionDescriptor.ViewEnginePath;

                if (role == "VENDOR")
                {
                    // Allow access ONLY to pages starting with /VendorPortal, /Login, or /Error
                    bool isAllowed = pagePath.StartsWith("/VendorPortal", StringComparison.OrdinalIgnoreCase) ||
                                     pagePath.Equals("/Login", StringComparison.OrdinalIgnoreCase) ||
                                     pagePath.Equals("/Error", StringComparison.OrdinalIgnoreCase);

                    if (!isAllowed)
                    {
                        context.Result = new RedirectToPageResult("/VendorPortal/Index");
                        return;
                    }
                }
                else
                {
                    // For non-vendor users (OWNER, COMPANY, etc.), prevent them from accessing /VendorPortal pages
                    bool isVendorPage = pagePath.StartsWith("/VendorPortal", StringComparison.OrdinalIgnoreCase);
                    if (isVendorPage)
                    {
                        context.Result = new RedirectToPageResult("/Index");
                        return;
                    }
                }
            }

            await next();
        }
    }
}
