htmx.registerExtension('issue-154-extension-header', {
	'htmx:config:request': event => {
		event.detail.ctx.request.headers['HX-PTag'] = 'browser-extension';
		return true;
	},
});
