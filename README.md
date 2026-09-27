# game1

Unity + C# game.

## First-time setup after cloning

Enable the tracked git hooks (run once per clone):

```sh
git config core.hooksPath .githooks
```

`.githooks/post-commit` records each commit in the private dev-history repo
whose location is set in `.claude/dev-repo-config.json`. If that repo isn't
checked out next to this one, the hook does nothing.
