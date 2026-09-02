htmx.registerExtension('issue-154-extension-header', {
	'htmx:config:request': context => {
		context.ctx.request.headers['HX-PTag'] = 'browser-extension';
		return true;
	},
});
