# game1

## Commit attribution (required)

Every commit you make in this repo must end with a `Session-Id` trailer carrying
your Claude Code session id (it's given to you at session start):

```sh
git commit -m "Short summary" --trailer "Session-Id: <your-session-id>"
```

`.githooks/post-commit` uses this trailer to link the commit to this session's
history in the dev-history repo. Without it the commit is recorded as a human commit.

## Dev-history hooks

`.claude/hooks/` syncs this session's transcript to the dev-history repo (location in
`.claude/dev-repo-config.json` — never hardcode it) and blocks reads of secret files
(`.env*`, `*.pem`, `*.key`, `secrets/`, `credentials/`). Don't try to work around the block;
ask the user for any value you need.
