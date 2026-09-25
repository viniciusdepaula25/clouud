using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Clouud.Web.Services
{
    public static class SlugHelper
    {
        /// <summary>"Tekken 8: Edição Ultimate" -> "tekken-8-edicao-ultimate"</summary>
        public static string Gerar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return string.Empty;
            }

            var semAcentos = new StringBuilder();
            foreach (var c in texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    semAcentos.Append(c);
                }
            }

            return Regex.Replace(semAcentos.ToString(), "[^a-z0-9]+", "-").Trim('-');
        }
    }
}
