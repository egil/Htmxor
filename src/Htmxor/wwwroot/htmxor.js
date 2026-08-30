document.addEventListener('htmx:config:request', (event) => {
	const context = event.detail.ctx;
	const sourceElement = context.sourceElement;
	const request = context.request;
	const eventHandlerId = sourceElement.getAttribute('hxor-eventid');
	if (eventHandlerId)
		request.headers['HXOR-Event-Handler-Id'] = eventHandlerId;

	const method = request.method.toUpperCase();
	if (method === 'GET' || method === 'HEAD' || method === 'OPTIONS' || method === 'TRACE')
		return;

	const form = sourceElement.form ?? sourceElement.closest('form');
	const requestToken = (form ?? document)
		.querySelector("input[name='__RequestVerificationToken']");
	if (requestToken?.value)
		request.headers.RequestVerificationToken = requestToken.value;
});
