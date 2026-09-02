htmx.registerExtension('issue-154-extension-header', {
	onEvent: (name, event) => {
		if (name === 'htmx:config:request') {
			event.detail.ctx.request.headers['HX-PTag'] = 'browser-extension';
		}

		return true;
	},
});
