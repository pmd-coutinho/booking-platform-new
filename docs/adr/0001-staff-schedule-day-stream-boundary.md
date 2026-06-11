# Use Staff Schedule Day Streams For Booking Conflicts

Service appointment conflict enforcement will use a `Staff Schedule Day` event stream: one stream per staff member per business-local date. This keeps the event-sourced consistency boundary aligned with the rule that a staff member can have only one booked service appointment at a time, while avoiding both one giant stream per staff member and unsafe conflict checks based only on per-appointment streams. A service appointment may cross one business-local midnight, in which case booking writes to each affected Staff Schedule Day stream in one transaction.

## Considered Options

- Per-appointment streams were rejected because they do not own the cross-appointment conflict invariant.
- Per-staff-member streams were rejected because they would grow without a natural time boundary and become a hot stream.
- Per-business-day streams were rejected because they would coordinate unrelated staff members through the same stream.

## Consequences

- Service appointments can affect at most two Staff Schedule Day streams.
- Cross-day booking commands must coordinate all affected Staff Schedule Day streams transactionally.
