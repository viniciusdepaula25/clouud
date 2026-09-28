using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace Clouud.Web.Services.Emails
{
    /// <summary>
    /// Monta o corpo dos e-mails a partir das views Razor em Views/Emails (fáceis de editar, com o mesmo
    /// layout escuro da loja). Funciona também fora de uma requisição, nos serviços em segundo plano.
    /// </summary>
    public partial class RenderizadorEmail
    {
        private readonly IRazorViewEngine views;
        private readonly ITempDataProvider tempData;
        private readonly IServiceProvider servicos;

        public RenderizadorEmail(IRazorViewEngine views, ITempDataProvider tempData, IServiceProvider servicos)
        {
            this.views = views;
            this.tempData = tempData;
            this.servicos = servicos;
        }

        /// <summary>Devolve (html, texto) da view Views/Emails/{nome}.cshtml.</summary>
        public async Task<(string Html, string Texto)> RenderizarAsync<T>(string nome, T modelo)
        {
            // Dinheiro sempre como "R$ 1.234,50", também nos serviços em segundo plano (fora de uma requisição)
            var culturaAnterior = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = Cultura;
            try
            {
                return await RenderizarNaCulturaAsync(nome, modelo);
            }
            finally
            {
                CultureInfo.CurrentCulture = culturaAnterior;
            }
        }

        private static readonly CultureInfo Cultura = Infraestrutura.Dinheiro.CriarCultura();

        private async Task<(string Html, string Texto)> RenderizarNaCulturaAsync<T>(string nome, T modelo)
        {
            var httpContext = new DefaultHttpContext { RequestServices = servicos };
            var contexto = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var caminho = $"~/Views/Emails/{nome}.cshtml";
            var resultado = views.GetView(executingFilePath: null, viewPath: caminho, isMainPage: true);
            if (!resultado.Success)
            {
                throw new InvalidOperationException($"View de e-mail não encontrada: {caminho}");
            }

            await using var saida = new StringWriter();
            var viewData = new ViewDataDictionary<T>(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = modelo };
            var viewContext = new ViewContext(contexto, resultado.View, viewData,
                new TempDataDictionary(httpContext, tempData), saida, new HtmlHelperOptions());
            await resultado.View.RenderAsync(viewContext);

            var html = saida.ToString();
            return (html, ParaTexto(html));
        }

        /// <summary>Versão em texto simples: tira as tags, mantém quebras de linha e mostra o endereço dos links.</summary>
        public static string ParaTexto(string html)
        {
            var t = Regex.Replace(html, @"<(style|head|script)[^>]*>.*?</\1>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"<a\s[^>]*href=""([^""]+)""[^>]*>(.*?)</a>", m =>
            {
                var texto = Regex.Replace(m.Groups[2].Value, "<[^>]+>", "").Trim();
                var url = m.Groups[1].Value;
                return texto == url || texto.Length == 0 ? url : $"{texto} ({url})";
            }, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"</(p|div|h[1-6]|tr|li|table)>", "\n", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"</t[dh]>", "  ", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, "<[^>]+>", "");
            t = WebUtility.HtmlDecode(t);
            t = Regex.Replace(t, @"[ \t]+", " ");
            t = Regex.Replace(t, @" *\n *", "\n");
            t = Regex.Replace(t, @"\n{3,}", "\n\n");
            return t.Trim() + "\n";
        }
    }
}
