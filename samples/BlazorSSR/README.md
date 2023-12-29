# Blazor SSR idiomatic sample

The purpose of this sample is to show how to use Blazor SSR in an idiomatic way to implement the "Contact APP" solution. This should give us an idea of how this solutions compares to the HTMX enabled ones.

The implementation is based on the HTMX sample app from https://hypermedia.systems/.

**Features and experience:**

- Render mode is SSR only.
- All pages are reached using enhanced navigation. 
- All forms are enhanced, with no full page loads between adding, editing, and deleting. This seems to work even across redirects instigated by the NavigationManager.
- The view contacts page uses streaming rendering.
- The delete component seems to require the originating form to be present in the new component, otherwise it fails with a "Cannot submit the form 'delete-contact-form' because no form on the page currently has that name." error.
- Using other HTTP verbs than GET and POST is not supported.
- Interactive features such as live search needs custom JS to be supported.

## Notes:

- The `contacts.json` file is not ignored by the GIT, but still included. To toggle this behavior:
   
  Disable tracking: `git update-index --skip-worktree contacts.json`
  Enable tracking: `git update-index --no-skip-worktree contacts.json`

