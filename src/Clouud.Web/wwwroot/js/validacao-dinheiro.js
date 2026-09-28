// Validação no navegador dos campos de dinheiro (<input ... data-dinheiro>):
// aceita "59,90", "59.90" e "1.234,50", seguindo as mesmas regras do servidor (Infraestrutura/Dinheiro.cs).
(function ($) {
    if (!$ || !$.validator) {
        return;
    }

    function lerDinheiro(texto) {
        var t = String(texto).replace(/R\$/gi, "").replace(/[\s ]/g, "");
        var negativo = t.charAt(0) === "-";
        if (negativo) {
            t = t.substring(1);
        }
        if (!/^(\d+([.,]\d+)*|\d*[.,]\d+)$/.test(t)) {
            return NaN;
        }
        var virgula = t.lastIndexOf(","), ponto = t.lastIndexOf(".");
        var milharOk = function (inteira, sep) {
            var grupos = inteira.split(sep);
            return grupos.length === 1 || (grupos[0].length >= 1 && grupos[0].length <= 3 &&
                grupos.slice(1).every(function (g) { return g.length === 3; }));
        };
        var normal;
        if (virgula >= 0 && ponto >= 0) {
            var dec = virgula > ponto ? "," : ".", mil = dec === "," ? "." : ",";
            var partes = t.split(dec);
            if (partes.length !== 2 || partes[1].indexOf(mil) >= 0 || !milharOk(partes[0], mil)) {
                return NaN;
            }
            normal = partes[0].split(mil).join("") + "." + partes[1];
        } else if (virgula >= 0) {
            var p = t.split(",");
            if (p.length !== 2) {
                return NaN;
            }
            normal = p[0] + "." + p[1];
        } else if (ponto >= 0) {
            var q = t.split(".");
            var ehMilhar = q.length > 2 || (q[1].length === 3 && q[0] !== "0");
            if (ehMilhar && !milharOk(t, ".")) {
                return NaN;
            }
            normal = ehMilhar ? t.split(".").join("") : t;
        } else {
            normal = t;
        }
        var valor = parseFloat(normal);
        return negativo ? -valor : valor;
    }

    window.lerDinheiro = lerDinheiro;

    var numeroOriginal = $.validator.methods.number;
    var rangeOriginal = $.validator.methods.range;
    var ehDinheiro = function (elemento) { return elemento && elemento.getAttribute("data-dinheiro") === "true"; };

    $.validator.methods.number = function (valor, elemento) {
        if (ehDinheiro(elemento)) {
            return this.optional(elemento) || !isNaN(lerDinheiro(valor));
        }
        return numeroOriginal.call(this, valor, elemento);
    };
    $.validator.methods.range = function (valor, elemento, parametros) {
        if (ehDinheiro(elemento)) {
            var n = lerDinheiro(valor);
            return this.optional(elemento) || (!isNaN(n) && n >= parametros[0] && n <= parametros[1]);
        }
        return rangeOriginal.call(this, valor, elemento, parametros);
    };
})(window.jQuery);
