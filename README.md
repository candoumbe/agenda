# Agenda module

[![Build Status](https://github.com/candoumbe/agenda/actions/workflows/integration.yml/badge.svg)](https://github.com/candoumbe/agenda/actions/workflows/integration.yml)
[![Delivery Status](https://github.com/candoumbe/agenda/actions/workflows/delivery.yml/badge.svg)](https://github.com/candoumbe/agenda/actions/workflows/delivery.yml)
[![codecov](https://codecov.io/gh/candoumbe/agenda/graph/badge.svg?token=RVArShIZY1)](https://codecov.io/gh/candoumbe/agenda)
[![API mutation testing badge](https://img.shields.io/endpoint?label=Agenda.API&style=flat&url=https%3A%2F%2Fbadge-api.stryker-mutator.io%2Fgithub.com%2Fcandoumbe%2Fagenda%2Fdevelop%3Fmodule=Agenda.API)](https://dashboard.stryker-mutator.io/reports/github.com/candoumbe/agenda/develop?module=Agenda.API)
[![Agenda.Ids Mutation testing badge](https://img.shields.io/endpoint?label=Agenda.Ids&style=flat&url=https%3A%2F%2Fbadge-api.stryker-mutator.io%2Fgithub.com%2Fcandoumbe%2Fagenda%2Fdevelop%3Fmodule=Agenda.Ids)](https://dashboard.stryker-mutator.io/reports/github.com/candoumbe/agenda/develop?module=Agenda.Ids)
[![Agenda.Objects Mutation testing badge](https://img.shields.io/endpoint?label=Agenda.Objects&style=flat&url=https%3A%2F%2Fbadge-api.stryker-mutator.io%2Fgithub.com%2Fcandoumbe%2Fagenda%2Fdevelop%3Fmodule=Agenda.Objects)](https://dashboard.stryker-mutator.io/reports/github.com/candoumbe/agenda/develop?module=Agenda.Objects)

A REST API designed to handle appointments

## Design principles

This repo follows the gitflow to manage its branches.

## Get started

1. clone this repo

## <a id="lnk-contribute">Want to contribute ?</a>

You can start contributing by looking at [`good first issues`](https://github.com/kalic-io/agenda/contribute)
on the issue tracker.

Make sure you've read the [contribution guidelines](CONTRIBUTING.md)

## Cleaning GitHub PR branch config duplicates

The script [clean-git-pr-config.sh](clean-git-pr-config.sh) helps fix duplicated
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

## <a id="lnk-contribute">Troubleshooting</a>

If you find an issue, you can submit a pull request (PRs are welcome 😀 !!) or [open an issue](https://github.com/kalic-io/agenda/issues/new).
