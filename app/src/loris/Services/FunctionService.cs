// using Loris.Data;
// using Loris.Models;
// using Loris.Results;
//
// namespace Loris.Services;
//
// public class FunctionService(ILogger<FunctionService> logger, LorisDbContext ctx) : IFunctionService
// {
//     private readonly ILogger<FunctionService> _logger = logger;
//     private readonly LorisDbContext _ctx = ctx;
//
//     public Task<Result> CallFunction(Guid id)
//     {
//         var function = await _ctx.Functions.Where(f => f.Guid)
//     }
//
//     public Task<Result> CallFunction(DevEui devEui, int functionId)
//     {
//         throw new NotImplementedException();
//     }
// }
