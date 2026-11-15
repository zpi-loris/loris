using Loris.Automations;
using Loris.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Loris.Extensions;

public static class ErrorExtensions
{
    extension(ControllerBase controller)
    {
        public ObjectResult ErrorProblem(Error error)
        {
            var (status, title) = error.Type switch
            {
                ErrorType.Validation => (StatusCodes.Status400BadRequest, "Validation failed"),
                ErrorType.NotFound => (StatusCodes.Status404NotFound, "Resource not found"),
                ErrorType.Conflict => (StatusCodes.Status409Conflict, "Conflict"),
                ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Forbidden"),
                ErrorType.Upstream => (StatusCodes.Status502BadGateway, "Upstream service failed"),
                _ => (StatusCodes.Status500InternalServerError, "An error occurred"),
            };

            var problem = controller.ProblemDetailsFactory.CreateProblemDetails(
                controller.HttpContext,
                statusCode: status,
                title: title,
                detail: error.Description
            );

            problem.Extensions["errorCode"] = error.Code;

            return new ObjectResult(problem)
            {
                StatusCode = status,
                ContentTypes = { "application/problem+json" },
            };
        }

        public ObjectResult ErrorProblem(IReadOnlyList<AutomationError> errors)
        {
            var problem = controller.ProblemDetailsFactory.CreateProblemDetails(
                controller.HttpContext,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation failed",
                detail: "One or more automation rules failed validation for its fields"
            );

            problem.Extensions["invalidRules"] = errors
                .GroupBy(e => (e.Id, e.Name))
                .Select(g => new InvalidRuleGroup(
                    g.Key.Id,
                    g.Key.Name,
                    [.. g.Select(v => new AutomationFieldError(v.Pointer, v.Value, v.Details))]
                ))
                .ToList();

            return new ObjectResult(problem)
            {
                StatusCode = StatusCodes.Status400BadRequest,
                ContentTypes = { "application/problem+json" },
            };
        }

        public ActionResult<T> ToActionResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
                return controller.Ok(result.Value);
            return controller.ErrorProblem(result.Error);
        }
    }
}
