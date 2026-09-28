// Comportamentos comuns da loja. Ficam aqui (e não em atributos onclick/onerror no HTML) porque a
// Content-Security-Policy só deixa rodar scripts de arquivos do próprio site ou com o "nonce" da página.
(function () {
    "use strict";

    // <img data-fallback="/img/sem-capa.svg">: se a imagem não carregar, mostra a imagem reserva
    function usarReserva(img) {
        if (img.dataset.fallback && img.dataset.fallbackUsada !== "1") {
            img.dataset.fallbackUsada = "1";
            img.src = img.dataset.fallback;
        }
    }
    document.addEventListener("error", function (evento) {
        if (evento.target instanceof HTMLImageElement) {
            usarReserva(evento.target);
        }
    }, true); // "error" não sobe na árvore: precisa ser capturado
    function conferirQuebradas() {
        // imagens que falharam antes deste script carregar
        document.querySelectorAll("img[data-fallback]").forEach(function (img) {
            if (img.complete && img.naturalWidth === 0) {
                usarReserva(img);
            }
        });
    }
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", conferirQuebradas);
    } else {
        conferirQuebradas();
    }
    window.addEventListener("load", conferirQuebradas);

    // <form data-confirmar="Excluir?">: pergunta antes de enviar
    document.addEventListener("submit", function (evento) {
        var form = evento.target;
        if (form instanceof HTMLFormElement && form.dataset.confirmar && !window.confirm(form.dataset.confirmar)) {
            evento.preventDefault();
        }
    });

    // <select data-enviar-ao-mudar>: envia o formulário quando a opção muda
    document.addEventListener("change", function (evento) {
        var campo = evento.target;
        if (campo instanceof HTMLSelectElement && campo.hasAttribute("data-enviar-ao-mudar") && campo.form) {
            campo.form.requestSubmit ? campo.form.requestSubmit() : campo.form.submit();
        }
    });
})();
