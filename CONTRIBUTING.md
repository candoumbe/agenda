# How to contribute

I'm really glad you're reading this, because I need volunteer developers to help this project come to fruition.

For repository-specific AI instructions, see [AGENTS.md](AGENTS.md).

## Testing

There are a handful of unit/integration tests. Please write unit/integration tests examples for new code you create.

## Branch workflow

This repository follows a lightweight GitFlow workflow. The main branches are:

| Branch      | Source branch | Purpose                                                                                                                           |
| ----------- | ------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| `develop`   | N/A           | Default integration branch for day-to-day work. All topic branches are created from here.                                         |
| `feature/*` | `develop`     | Short-lived branches for new features (e.g. `feature/add-new-participant-to-existing-appointment`).                               |
| `coldfix/*` | `develop`     | Non-urgent fixes that can wait for the next release (e.g. `coldfix/replace-podman-with-docker-when-inside-the-docker-container`). |
| `hotfix/*`  | `main`        | Urgent fixes applied directly to a release (e.g. `hotfix/fix-null-reference-in-search`). Created from `main` and merged back into `main` and `develop`. |
| `release/*` | `develop`     | Stabilization branches for an upcoming version (e.g. `release/0.1.0`).                                                            |
| `chore/*`   | `develop`     | Branches where to do some chores (e.g. `chore/adjust-style` or `chore/improve-performance`).                                      |
| `exp/*`     | `develop`     | Experimentation branches.                                                                                                         |
                                                                                                      |

**Rules:**

1. Always branch off `develop` unless you are creating a `hotfix/*` branch.
2. Keep topic branches short-lived: merge back into `develop` as soon as the work is complete.
3. Use descriptive branch names with the appropriate prefix so that [GitVersion](GitVersion.yml) can compute the version automatically.
4. Delete branches after they are merged.

For a quick project overview, read [README.md](README.md).

## Commit messages — Conventional Commits

This project uses the [Conventional Commits](https://www.conventionalcommits.org/) specification for all commit messages. This ensures a consistent, machine-readable history that can be used to generate changelogs and determine semantic version bumps.

A commit message must follow this format:

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### Common types

| Type       | When to use                                                                        |
| ---------- | ---------------------------------------------------------------------------------- |
| `feat`     | A new feature                                                                      |
| `fix`      | A bug fix                                                                          |
| `docs`     | Documentation-only changes                                                         |
| `style`    | Changes that do not affect the meaning of the code (white-space, formatting, etc.) |
| `refactor` | A code change that neither fixes a bug nor adds a feature                          |
| `test`     | Adding or correcting tests                                                         |
| `chore`    | Maintenance tasks (dependencies, CI, tooling, etc.)                                |
| `ci`       | Changes to CI configuration files and scripts                                      |

### Examples

```bash
# Simple feature
git commit -m "feat: add DELETE endpoint for appointment attendees"

# Bug fix with scope
git commit -m "fix(search): handle null reference when query is empty"

# Breaking change (note the ! after the type)
git commit -m "feat!: remove legacy v1 appointments endpoint"

# Multi-line commit with body and footer
git commit -m "feat(attendees): allow adding multiple attendees at once

Accept an array of attendee objects in the POST body instead of a single object.

Closes #42"
```

### Important notes

- The **type** and **description** are mandatory.
- Use the imperative mood in the description ("add feature", not "added feature").
- A commit with a **`!`** after the type/scope, or a `BREAKING CHANGE:` footer, signals a breaking change and triggers a major version bump.
- Keep each commit atomic — one logical change per commit.

## Submitting changes

Please send a [GitHub Pull Request to Agenda](https://github.com/candoumbe/agenda/pull/new/develop) with a clear list of what you've done (read more about [pull requests](http://help.github.com/pull-requests/)).
When you send a pull request, we will love you forever if you include unit tests as examples. We can always use more test coverage. Please follow our coding conventions (below) and make sure all of your commits are atomic (one feature per commit).

## Coding conventions

Start reading our code and you'll get the hang of it. We optimize for readability:

- **Stick to the [.editorconfig](.editorconfig) file** at the root of the repository
- **Do not `var`** unless there's no other option : `var` should only be used for anonymous types. So instead of `var data = new Something()`, prefer `Something data = new Something()`.
  Even better, prefer `ISomething data = new Something()` whenever possbile.
- **Single entry, single exit** : a method should have one entry and one exit. This is just to avoid missing an exit point that could be in the middle of a complex algorithm.
  I don't really mind the complexity of an algorithm (up to a certain point 😉).

    I will always prefer having

    ```csharp
    int result;
    if (condition)
     result = 42;
    else
        result = 97;
    return result;
    ```

instead of

```csharp
if (condition)
   return 42;
else
   return 97;
```

The first version is longer for sure, but I'm more comfortable reading a large code block where the exit will always be at the end than having a block of code of the same size without knowing where the exit will be.

- This is open source software. Consider the people who will read your code, and make it look nice for them. It's sort of like driving a car: Perhaps you love doing donuts when you're alone, but with passengers the goal is to make the ride as smooth as possible.
- So that we can consistently serve images from the CDN, always use image_path or image_tag when referring to images. Never prepend "/images/" when using image_path or image_tag.

Thanks
