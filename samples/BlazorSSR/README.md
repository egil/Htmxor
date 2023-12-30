# Blazor SSR idiomatic sample

This sample aims to show how to use Blazor SSR in an idiomatic way to implement the "Contact APP" solution. This should give us an idea of how this solution compares to the HTMX-enabled one.

The implementation is based on the HTMX sample app from https://hypermedia.systems/.

## Features and Experience:

### Pros:
- Render mode is SSR only.
- All pages are reached using enhanced navigation. 
- All forms are enhanced, with no full page loads between adding, editing and deleting. This works even across redirects instigated by `NavigationManager`.
- The view contacts page uses streaming rendering.
- With streaming rendering it is easy to show a loading indicator while the page is loading.

### Cons:

- The [delete component seems to require the originating form](https://github.com/egil/BlazorHtmx/blob/974e3ba24382fa2b2aab0a14b0f50426a29161af/samples/BlazorSSR/Components/Contacts/DeleteContactPage.razor#L33-L36) to be present in the new component, otherwise, it fails with a "Cannot submit the form 'delete-contact-form' because no form on the page currently has that name." error.
- Using other HTTP verbs than GET and POST is not supported.
- Interactive features such as live search needs custom JS to be supported.
- Even with enhanced navigation and forms, the entire page is generated on the backend and sent to the client. 

## Notes:

- The `contacts.json` file is not ignored by the GIT, but is still included. To toggle this behavior:
  - Disable tracking: `git update-index --skip-worktree contacts.json`  
  - Enable tracking: `git update-index --no-skip-worktree contacts.json`

