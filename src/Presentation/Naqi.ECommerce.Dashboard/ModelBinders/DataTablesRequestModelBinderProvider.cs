// src/Presentation/Naqi.ECommerce.Dashboard/ModelBinders/DataTablesRequestModelBinderProvider.cs

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Naqi.ECommerce.Application.Common.Models;

namespace Naqi.ECommerce.Dashboard.ModelBinders;

public class DataTablesRequestModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Metadata.ModelType == typeof(DataTablesRequest)
            ? new DataTablesRequestModelBinder()
            : null;
    }
}