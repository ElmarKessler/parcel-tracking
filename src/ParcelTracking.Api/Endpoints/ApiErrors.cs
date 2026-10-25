namespace ParcelTracking.Api.Endpoints;

internal static class ApiErrors
{
    public static IResult Validation(string field, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });
}
