using Microsoft.AspNetCore.Mvc.Filters;

namespace ApiService.ActionFilters;

public class LogActionFilter : ActionFilterAttribute
{
    private readonly ILogger<LogActionFilter> _logger;

    public LogActionFilter(ILogger<LogActionFilter> logger)
    {
        _logger = logger;
    }

    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        Log("starting", filterContext.RouteData);
    }

    public override void OnActionExecuted(ActionExecutedContext filterContext)
    {
        Log("ending", filterContext.RouteData);
    }

    private void Log(string moment, RouteData routeData)
    {
        var controllerName = routeData.Values["controller"];
        var actionName = routeData.Values["action"];
        var message = String.Format("{0} '{1}' on controller:{2}", moment.ToUpper(), actionName, controllerName);

        _logger.LogInformation(message);
    }
}