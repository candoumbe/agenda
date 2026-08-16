# Tips and tricks

## Cleaning GitHub PR branch config duplicates

The script [clean-git-pr-config.sh](../clean-git-pr-config.sh) helps fix duplicated
Git branch configuration values used by GitHub Pull Request tooling.

Why this matters:

- duplicated `branch.<current-branch>.github-pr-owner-number` values can confuse PR tooling
- duplicated `branch.<current-branch>.github-pr-base-branch` values can point to the wrong base branch
- this script audits all config origins and restores a single canonical local value per key

How to use it:

1. Run it from the repository root (required by the script).
2. Review the audit output (all origins and effective values).
3. Confirm cleanup when prompted.
4. Check the post-cleanup verification summary.

Command:

```bash
./clean-git-pr-config.sh
```

Notes:

- the script only targets the current branch
- it removes duplicates from local/worktree/global scopes then restores one canonical value locally
- if duplicates still exist after cleanup, inspect all origins with:

```bash
git config --show-origin --show-scope --list
```
