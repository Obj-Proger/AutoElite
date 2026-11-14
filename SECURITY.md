# Security policy

AutoElite keeps the personal data of students and staff, their contracts and payments, so a vulnerability in it matters.
Thank you for reporting one responsibly.

## Supported versions

Security fixes are made on the `main` branch and in the latest release. Schools that run an older version should update
to the latest one.

## Reporting a vulnerability

Please do not open a public issue, discussion or pull request for a vulnerability. Report it privately through GitHub
instead: open [Security > Report a vulnerability](https://github.com/Obj-Proger/AutoElite/security/advisories/new) in
this repository. Describe what is affected, how to reproduce it and what an attacker could do with it; a proof of
concept helps, but please test only against an installation of your own, never against the data of a real school.

You can expect an answer within a week. Once the report is confirmed, a fix is prepared in a private fork, released, and
the advisory is published with credit to you, unless you prefer to stay anonymous.

## Scope

In scope are the code in this repository: the API, its client, the offline mode, the mobile application and the
website, and the way they are built and released. Out of scope are a school's own installation and hosting, denial of
service by sheer volume of traffic, and findings that need a device or an account that is already compromised.

## How the project is kept secure

- Dependabot keeps the NuGet packages and the GitHub Actions up to date, and NuGet Audit fails the build on a package
  with a known vulnerability, also in a weekly run without any change.
- CodeQL analyses the C# code and the workflows on every change and weekly.
- The actions of the workflows are pinned by commit, and every package comes from nuget.org only (`nuget.config`).
- Secrets are never committed: they are kept in user secrets during development and in the environment in production.
