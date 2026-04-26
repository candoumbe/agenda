# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### 🚀 New features

#### Frontend

- Added UI for scheduling an appointment
- Added appointments list page with card-based layout, date grouping, search by subject, and pagination (#502)
- Added automatic redirect to appointments list after creating a new appointment (#502)

#### API
- Added `DELETE /appointments/{id}/attendees/{id}` endpoint
- Added RabbitMQ integration and related configuration.
- Added publication of `AppointmentScheduled` event when a new appointment is scheduled : 
- Added healthchecks for PostgreSQL and RabbitMQ
- Fixed appointments listing pagination metadata for UI navigation (`total` now represents total pages, with explicit `totalCount` and `pageSize` fields)
- Added multi-criteria filtering for appointments listing (`subject`, `location`, and `from`/`to` time range)

### 🧹 Housekeeping
- Added task issue template
- Updated bug issue template
- Added Codecov configuration file
- Updated `xRetry` to `1.0.0-rc2`
- Updated `xunit.v3` to `3.2.0`
- Updated `Paramore.Brighter.*` packages to `10.0.4`
- Bumped TFM to `net10.0`
- Added dotnet devcontainer feature to manage .NET versions

## [0.1.0] / 2025-10-03
### 🚀 New features
- Initial release of the Agenda module

[Unreleased]: https://github.com/candoumbe/agenda/compare/0.1.0...HEAD
[0.1.0]: https://github.com/candoumbe/agenda/tree/0.1.0