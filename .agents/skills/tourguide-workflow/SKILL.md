---
name: tourguide-workflow
description: Use when you run the TourGuideFunction tests or commit and push changes in this repository. Gives the expected test result and the push rule.
---

# TourGuideFunction test and push workflow

## Tests

- Run the tests with `dotnet test`.
- The last known result: 26 tests pass and the runner skips 13 tests.
- The skipped tests are not failures. A new failure or a lower pass count is a regression.
- The counts change when somebody adds tests. Do not treat a higher count as an error.

## Git

- Do not push to `main`.
- You can make local commits.
- Ask the user before you push any branch.
