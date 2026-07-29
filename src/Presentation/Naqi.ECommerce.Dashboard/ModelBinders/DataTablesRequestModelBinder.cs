// src/Presentation/Naqi.ECommerce.Dashboard/ModelBinders/DataTablesRequestModelBinder.cs
//
// DataTables sends its server-side processing state as bracket-notation
// form fields (draw, start, length, search[value], order[0][column]...).
// ASP.NET Core's default model binder expects dot-notation for nested
// properties (Search.Value) and won't match "search[value]" - so a plain
// [FromForm] DataTablesRequest parameter on its own would silently bind
// nothing. This binder reads the raw form fields directly and constructs
// the strongly-typed DTO, so the controller action itself never touches
// Request.Form or a single magic string key.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Naqi.ECommerce.Application.Common.Models;

namespace Naqi.ECommerce.Dashboard.ModelBinders;

public class DataTablesRequestModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var form = bindingContext.HttpContext.Request.Form;

        var request = new DataTablesRequest
        {
            Draw = ParseInt(form["draw"], fallback: 1),
            Start = ParseInt(form["start"], fallback: 0),
            Length = ParseInt(form["length"], fallback: 20),
            SearchValue = form["search[value]"].FirstOrDefault() ?? string.Empty
        };

        bindingContext.Result = ModelBindingResult.Success(request);
        return Task.CompletedTask;
    }

    private static int ParseInt(Microsoft.Extensions.Primitives.StringValues value, int fallback) =>
        int.TryParse(value.FirstOrDefault(), out var parsed) ? parsed : fallback;
}