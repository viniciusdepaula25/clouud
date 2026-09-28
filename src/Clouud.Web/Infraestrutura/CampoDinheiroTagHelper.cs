using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Clouud.Web.Infraestrutura
{
    /// <summary>
    /// &lt;input asp-for="Preco" dinheiro /&gt;: campo de texto que mostra "59,90" e aceita vírgula ou ponto.
    /// Roda depois do tag helper padrão do input e só ajusta o que ele gerou.
    /// </summary>
    [HtmlTargetElement("input", Attributes = "asp-for,dinheiro")]
    public class CampoDinheiroTagHelper : TagHelper
    {
        public override int Order => 1000;

        [HtmlAttributeName("asp-for")]
        public ModelExpression? Para { get; set; }

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; } = default!;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.Attributes.RemoveAll("dinheiro");
            output.Attributes.SetAttribute("type", "text");
            output.Attributes.SetAttribute("inputmode", "decimal");
            output.Attributes.SetAttribute("autocomplete", "off");
            output.Attributes.SetAttribute("data-dinheiro", "true");

            // Valor digitado que voltou com erro fica como a pessoa escreveu; o valor salvo aparece como 59,90
            var nomeCompleto = Para == null ? "" : ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(Para.Name);
            var digitado = ViewContext.ViewData.ModelState.TryGetValue(nomeCompleto, out var estado)
                && estado.AttemptedValue != null;
            if (!digitado && Para?.Model is decimal valor)
            {
                output.Attributes.SetAttribute("value", Infraestrutura.Dinheiro.ParaCampo(valor));
            }
        }
    }
}
