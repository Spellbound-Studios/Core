# Working in this repository

## What is where

- The repository root is the Unity dev project.
- The package is `Packages/<Name>`. It is the only folder a consumer receives.
- `Assets/_Project` is scaffolding for the dev project and never ships.

## Pull request titles

Format: `type(scope): what changed`

- Types: `feat fix perf refactor docs test build ci chore`
- Put a `!` before the colon for a breaking change, for example `feat(scope)!: what changed`.
- Pull requests are squash-merged with the title alone, so the title is the commit message.

## Versions

Never edit `version` in `package.json`. The release writes it.

## Checks

- Run `bash scripts/setup.sh` once after cloning.
- Before asking for review, run the Unity tests through the Unity command line.

## Releasing

- A pull request into `beta` releases a prerelease.
- A person moves `main` at a release.

## The rules

The binding rules are the line text at the address in `line-text-address.txt`. Nothing here copies them.
