document.addEventListener('htmx:config:request', (event) => {
	const context = event.detail.ctx;
	const sourceElement = context.sourceElement;
	const request = context.request;
	const method = request.method.toUpperCase();
	if (method === 'GET' || method === 'HEAD' || method === 'OPTIONS' || method === 'TRACE')
		return;

	const form = sourceElement.form ?? sourceElement.closest('form');
	const requestToken = (form ?? document)
		.querySelector("input[name='__RequestVerificationToken']");
	if (requestToken?.value)
		request.headers.RequestVerificationToken = requestToken.value;

	if (method === 'DELETE')
		request.body?.delete?.('__RequestVerificationToken');
});
