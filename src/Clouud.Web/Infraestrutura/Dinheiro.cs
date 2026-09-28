using System.Globalization;
using System.Text.RegularExpressions;

namespace Clouud.Web.Infraestrutura
{
    /// <summary>
    /// Leitura e escrita de valores em reais no padrão brasileiro ("1.234,50").
    /// Ao ler, aceita vírgula ou ponto como separador decimal, para quem digita "59,90" ou "59.90".
    /// </summary>
    public static partial class Dinheiro
    {
        /// <summary>Cultura usada para mostrar valores: moeda "R$ 1.234,50". Números comuns continuam com ponto (ex.: coordenadas do SVG do painel).</summary>
        public static CultureInfo CriarCultura()
        {
            var cultura = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            cultura.NumberFormat.CurrencySymbol = "R$";
            cultura.NumberFormat.CurrencyDecimalSeparator = ",";
            cultura.NumberFormat.CurrencyGroupSeparator = ".";
            cultura.NumberFormat.CurrencyPositivePattern = 2; // "R$ 59,90"
            cultura.NumberFormat.CurrencyNegativePattern = 9; // "-R$ 59,90"
            return cultura;
        }

        // Montado à mão (sem depender dos dados de cultura do sistema, que podem faltar em contêineres enxutos)
        private static readonly NumberFormatInfo Brasil = new()
        {
            NumberDecimalSeparator = ",",
            NumberGroupSeparator = "."
        };

        /// <summary>"59,90" (sem símbolo e sem separador de milhar), para campos de formulário.</summary>
        public static string ParaCampo(decimal valor) => valor.ToString("0.00", Brasil);

        /// <summary>"12,5" — número com vírgula e sem zeros à direita (ex.: porcentagem de cupom).</summary>
        public static string Numero(decimal valor) => valor.ToString("0.##", Brasil);

        /// <summary>
        /// Lê um valor digitado. Regras:
        /// "1.234,50", "1234,50", "1234.50" e "R$ 1.234,50" viram 1234.50;
        /// com os dois separadores, o último é o decimal ("1,234.50" também vale 1234.50);
        /// só ponto seguido de exatamente 3 dígitos é milhar ("1.500" = 1500, como se escreve no Brasil).
        /// </summary>
        public static bool TentarLer(string? texto, out decimal valor)
        {
            valor = 0;
            if (string.IsNullOrWhiteSpace(texto))
            {
                return false;
            }

            var t = texto.Replace("R$", "", StringComparison.OrdinalIgnoreCase).Replace(" ", "").Replace(" ", "").Trim();
            var negativo = t.StartsWith('-');
            if (negativo)
            {
                t = t[1..];
            }
            if (!FormatoValido().IsMatch(t))
            {
                return false;
            }

            var ultimaVirgula = t.LastIndexOf(',');
            var ultimoPonto = t.LastIndexOf('.');
            string normalizado;
            if (ultimaVirgula >= 0 && ultimoPonto >= 0)
            {
                // Os dois aparecem: o que vem por último separa os centavos, o outro é milhar
                var decimalChar = ultimaVirgula > ultimoPonto ? ',' : '.';
                var milharChar = decimalChar == ',' ? '.' : ',';
                var partes = t.Split(decimalChar);
                if (partes.Length != 2 || partes[1].Contains(milharChar) || !MilharCorreto(partes[0], milharChar))
                {
                    return false;
                }
                normalizado = partes[0].Replace(milharChar.ToString(), "") + "." + partes[1];
            }
            else if (ultimaVirgula >= 0)
            {
                var partes = t.Split(',');
                if (partes.Length != 2)
                {
                    return false;
                }
                normalizado = partes[0] + "." + partes[1];
            }
            else if (ultimoPonto >= 0)
            {
                var partes = t.Split('.');
                var ehMilhar = partes.Length > 2 || (partes[1].Length == 3 && partes[0] != "0");
                if (ehMilhar)
                {
                    if (!MilharCorreto(t, '.'))
                    {
                        return false;
                    }
                    normalizado = t.Replace(".", "");
                }
                else
                {
                    normalizado = t;
                }
            }
            else
            {
                normalizado = t;
            }

            if (!decimal.TryParse(normalizado, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out valor))
            {
                return false;
            }
            if (negativo)
            {
                valor = -valor;
            }
            return true;
        }

        /// <summary>"1.234.567": grupos de 3 dígitos depois do primeiro.</summary>
        private static bool MilharCorreto(string parteInteira, char separador)
        {
            var grupos = parteInteira.Split(separador);
            return grupos[0].Length is >= 1 and <= 3 && grupos.Skip(1).All(g => g.Length == 3)
                   || grupos.Length == 1;
        }

        [GeneratedRegex(@"^(\d+([.,]\d+)*|\d*[.,]\d+)$")]
        private static partial Regex FormatoValido();
    }
}
