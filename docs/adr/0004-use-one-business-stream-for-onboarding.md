# Use One Business Stream For Onboarding

Business onboarding and setup will use one Business event stream for now, including business creation, manager invitation and acceptance, profile completion, business hours, staff members, appointment types, staff capabilities, staff availability, and bookability transitions. This keeps bookability invariants local while the setup model is still young; appointment scheduling conflicts remain in Staff Schedule Day streams.

## Considered Options

- Separate setup streams for staff, appointment types, capabilities, and availability were rejected for now because they would require a coordinator before the domain boundaries have proven they need independence.
- A hybrid setup model was rejected for now because it would split onboarding decisions without a concrete scaling or lifecycle reason.

## Consequences

- Business events should carry stable identifiers for nested setup concepts so they can be split into child streams later if needed.
- Future stream splitting will require a deliberate migration or projection strategy; Marten projection rebuilds help read models, but command-side stream boundaries still need careful migration.
