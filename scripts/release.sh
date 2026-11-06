#!/usr/bin/env bash
# Releases a new version: sets it in frontend/package.json (the version the app shows),
# pushes main and tags it, so CI builds the Docker images: :latest, and for 1.2.2 also
# :1.2.2, :1.2 and :1.
#
#   ./scripts/release.sh 1.2.2
#   ./scripts/release.sh 1.3      (same as 1.3.0)
#
# Only runs on a clean, up-to-date main. Asks before pushing anything.
set -euo pipefail

fail() { echo "✗ $*" >&2; exit 1; }

version="${1:-}"
[[ -n "$version" ]] || fail "Give the version, e.g.: ./scripts/release.sh 1.2.2"
version="${version#v}"
# "2" and "1.3" mean 2.0.0 and 1.3.0: package.json and the CI image tags need all three numbers.
[[ "$version" =~ ^[0-9]+$ ]] && version="$version.0.0"
[[ "$version" =~ ^[0-9]+\.[0-9]+$ ]] && version="$version.0"
[[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || fail "'$version' isn't a version like 2, 1.3 or 1.2.2"
tag="v$version"

cd "$(git rev-parse --show-toplevel)"

branch="$(git branch --show-current)"
[[ "$branch" == "main" ]] || fail "You're on '$branch'. Switch to main first."
[[ -z "$(git status --porcelain)" ]] || fail "You have uncommitted changes. Commit or stash them first."

echo "→ Getting the latest main"
git pull --ff-only origin main

git fetch --quiet --tags origin
if git rev-parse -q --verify "refs/tags/$tag" >/dev/null; then
  fail "Tag $tag already exists."
fi

current="$(node -p "require('./frontend/package.json').version")"
if ! grep -q "^## \[$version\]" CHANGELOG.md; then
  echo "! CHANGELOG.md has no '## [$version]' section yet."
fi

echo
echo "Ready to release $tag from:"
git log --oneline -1
[[ "$current" == "$version" ]] || echo "The app version will go from $current to $version (one extra commit)."
echo
read -r -p "Push main and $tag? [y/N] " answer
[[ "$answer" == "y" || "$answer" == "Y" ]] || { echo "Stopped. Nothing was changed or pushed."; exit 1; }

if [[ "$current" != "$version" ]]; then
  npm --prefix frontend version "$version" --no-git-tag-version >/dev/null
  git add frontend/package.json frontend/package-lock.json
  git commit --quiet -m "chore: release $version"
fi

git push origin main
git tag -a "$tag" -m "TimeTracker $version"
git push origin "$tag"

echo
repo="$(git remote get-url origin | sed -E 's#^(git@|https://)github\.com[:/]##; s#\.git$##')"
echo "✓ Released $tag. Follow the image builds at:"
echo "  https://github.com/$repo/actions"
