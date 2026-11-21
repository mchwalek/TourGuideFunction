---
name: azure-functions-ops
description: Use when you change the timer schedule of TourGuideFunction or deploy it to Azure. Gives the cron time zone rule, the deploy command, and how to read its exit code.
---

# Azure Functions operations for TourGuideFunction

## Timer schedule

- The timer trigger is in `TourGuideFunction/ScrapeExcursions.cs`. The schedule is `0 0 13 * * *`.
- The schedule uses the six-field NCRONTAB format: second, minute, hour, day, month, day of week.
- Azure reads the schedule in UTC. The `WEBSITE_TIME_ZONE` app setting changes this time zone.
- Before you change the hour, check the `WEBSITE_TIME_ZONE` value of the function app.

## Deployment

1. Run `func azure functionapp publish TourGuideFunction20230823212350`.
2. Read the command output.

The command can exit with code 1 because PowerShell writes warnings to stderr. An exit code of 1 does not always mean a failed deployment. Read the output to find the result.
