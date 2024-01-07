document.addEventListener('htmx:configRequest', (evt) => {
    const httpVerb = evt.detail.verb.toUpperCase();
    if (httpVerb === 'GET' || httpVerb === 'HEAD' || httpVerb === 'OPTIONS' || httpVerb === 'TRACE')
        return;

    const antiforgery = htmx.config.antiforgery;

    if (antiforgery) {

        // already specified on form, short circuit
        if (evt.detail.parameters[antiforgery.formFieldName])
            return;

        const requestToken = document.cookie
            .split("; ")
            .find(row => row.startsWith("HX-XSRF-TOKEN="))
            .split("=")[1];

        if (antiforgery.headerName) {
            evt.detail.headers[antiforgery.headerName] = requestToken;
        } else {
            evt.detail.parameters[antiforgery.formFieldName] = requestToken;
        }
    }
});