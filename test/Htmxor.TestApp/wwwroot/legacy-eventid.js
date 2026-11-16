// Test-owned shim: the packaged htmxor.js no longer forwards hxor-eventid (#300).
// The legacy renderer used by this app (retired in #303) still dispatches on the
// HXOR-Event-Handler-Id header, so restore the forwarding for this app only.
document.addEventListener('htmx:config:request', (event) => {
	const context = event.detail.ctx;
	const eventHandlerId = context.sourceElement.getAttribute('hxor-eventid');
	if (eventHandlerId)
		context.request.headers['HXOR-Event-Handler-Id'] = eventHandlerId;
});
