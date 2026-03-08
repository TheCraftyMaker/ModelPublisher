---
name: github-modelp
description: >
  GitHub workflow skill for the ModelPublisher project (TheCraftyMaker/ModelPublisher).
  Use this skill whenever working with git or GitHub in the ModelPublisher project —
  creating branches, making commits, pushing, opening PRs, commenting on PRs, or
  checking PR status. Handles all the project-specific constraints automatically:
  gh CLI path, PowerShell invocation, branch protection rules, Co-Authored-By
  attribution, and PR-only workflow. Trigger any time the user says things like
  "push this", "create a PR", "commit", "open a pull request", "comment on the PR",
  or "merge this" in the context of ModelPublisher.
---

# GitHub Workflow — ModelPublisher

## Key Constraints

- **gh CLI**: Not on bash PATH. Always call via:
  `powershell.exe -Command "& 'C:\Program Files\GitHub CLI\gh.exe' <args>"`
- **Repo**: `TheCraftyMaker/ModelPublisher`
- **Branch protection**: `master` requires PRs — no direct pushes. `enforce_admins=true`.
- **Auto-delete**: Branches are deleted automatically on merge.
- **Working dir**: `C:\Source\ModelPublisher`
- **Git in bash**: `git` works directly in bash from `/c/Source/ModelPublisher`

## Co-Authored-By (always required)

Every commit message must end with:
```
Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
```

Always pass commit messages via heredoc to preserve formatting:
```bash
git commit -m "$(cat <<'EOF'
Short summary line

Longer explanation if needed.

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
EOF
)"
```

## Standard Workflow

### 1. Create a branch and commit
```bash
cd /c/Source/ModelPublisher
git checkout master && git pull
git checkout -b <kebab-case-branch-name>
git add <specific files>
git commit -m "$(cat <<'EOF'
<summary>

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
EOF
)"
git push -u origin <branch-name>
```

### 2. Create a PR
```powershell
powershell.exe -Command "& 'C:\Program Files\GitHub CLI\gh.exe' pr create --repo TheCraftyMaker/ModelPublisher --title '<title>' --body '<body>' --base master --head <branch-name>"
```

Keep PR titles under 70 characters. Body should explain *why*, not just *what*.

### 3. Comment on a PR
```powershell
powershell.exe -Command "& 'C:\Program Files\GitHub CLI\gh.exe' pr comment <number> --repo TheCraftyMaker/ModelPublisher --body '<comment>'"
```

### 4. Check PR status / read comments
```powershell
# Status
powershell.exe -Command "& 'C:\Program Files\GitHub CLI\gh.exe' pr view <number> --repo TheCraftyMaker/ModelPublisher --json number,title,state,headRefName,commits --jq '{state,branch:.headRefName,commits:[.commits[].messageHeadline]}'"

# Comments
powershell.exe -Command "& 'C:\Program Files\GitHub CLI\gh.exe' api repos/TheCraftyMaker/ModelPublisher/pulls/<number>/comments --jq '.[].body'"
powershell.exe -Command "& 'C:\Program Files\GitHub CLI\gh.exe' api repos/TheCraftyMaker/ModelPublisher/issues/<number>/comments --jq '.[].body'"
```

### 5. Push changes to an existing PR branch
```bash
cd /c/Source/ModelPublisher
git add <files>
git commit -m "$(cat <<'EOF'
<summary>

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
EOF
)"
git push
```

## Things to Watch Out For

- **After a PR is merged**, the remote branch is auto-deleted. If you push again to the same branch name, GitHub treats it as a new branch — you'll need a new PR.
- **Local master may lag behind** after a PR merges on GitHub. Always `git pull` on master before branching.
- **Avoid backticks in `--body` strings** passed via PowerShell — they trigger bash command substitution and can corrupt the text. Use plain text or write the body to a temp file.
- **JSON payloads** (e.g. branch protection): write to a temp file in `C:\Source\ModelPublisher\`, pass with `--input`, then delete the file.
- **Stage specific files** rather than `git add .` to avoid accidentally committing secrets or build artifacts.
