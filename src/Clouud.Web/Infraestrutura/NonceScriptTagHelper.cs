using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Clouud.Web.Infraestrutura
{
    /// <summary>
    /// Põe o nonce da Content-Security-Policy em todo &lt;script&gt; das views. Assim os scripts escritos
    /// nas páginas rodam, e um script injetado por um atacante (sem o nonce, que muda a cada resposta) não.
    /// </summary>
    [HtmlTargetElement("script")]
    public class NonceScriptTagHelper : TagHelper
    {
        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; } = default!;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (CabecalhosSeguranca.Nonce(ViewContext.HttpContext) is string nonce)
            {
                output.Attributes.SetAttribute("nonce", nonce);
            }
        }
    }
}
