using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Clouud.Web.Infraestrutura
{
    /// <summary>
    /// Faz os campos decimal dos formulários (e da URL) aceitarem "59,90" e "59.90".
    /// Sem ele o ASP.NET leria "59,90" como 5990.
    /// </summary>
    public class ModeloDecimalBrasileiro : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext contexto)
        {
            var resultado = contexto.ValueProvider.GetValue(contexto.ModelName);
            if (resultado == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            contexto.ModelState.SetModelValue(contexto.ModelName, resultado);
            var texto = resultado.FirstValue;
            if (string.IsNullOrWhiteSpace(texto))
            {
                // Campo vazio: decimal? fica nulo; decimal obrigatório é tratado pelo [Required]
                if (Nullable.GetUnderlyingType(contexto.ModelType) != null)
                {
                    contexto.Result = ModelBindingResult.Success(null);
                }
                return Task.CompletedTask;
            }

            if (Dinheiro.TentarLer(texto, out var valor))
            {
                contexto.Result = ModelBindingResult.Success(valor);
            }
            else
            {
                var nome = contexto.ModelMetadata.DisplayName ?? contexto.ModelMetadata.Name ?? contexto.ModelName;
                contexto.ModelState.TryAddModelError(contexto.ModelName, $"{nome}: valor inválido. Use, por exemplo, 59,90.");
            }
            return Task.CompletedTask;
        }
    }

    public class ModeloDecimalBrasileiroProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext contexto) =>
            contexto.Metadata.ModelType == typeof(decimal) || contexto.Metadata.ModelType == typeof(decimal?)
                ? new ModeloDecimalBrasileiro()
                : null;
    }
}
