htmx.registerExtension('issue-154-extension-header', {
	htmx_config_request: (_element, detail) => {
		detail.ctx.request.headers['HX-PTag'] = 'browser-extension';
	},
});
