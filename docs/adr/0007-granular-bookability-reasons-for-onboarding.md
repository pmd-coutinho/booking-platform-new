# Granular Bookability Reasons for Business Onboarding

Bookability will use granular reasons (`ManagerNotAccepted`, `ProfileIncomplete`, `NoStaffMembers`, `NoAppointmentTypes`, `NoStaffCapabilities`, `NoBusinessHours`, `NoStaffAvailability`) instead of a single `OnboardingIncomplete` catch-all. This lets the platform show exactly what is missing at each step of onboarding and avoids the ambiguity of a single opaque reason. The Business transitions from `Unbookable` to `Bookable` only when the set of reasons is empty.

## Considered Options

- A single `OnboardingIncomplete` reason was rejected because it hides which specific setup steps are missing, making the UI less actionable and making it harder to reason about when the Business becomes Bookable.
- A separate reason for every conceivable detail was rejected because it would create noise and tightly couple the reason set to the exact event order.

## Consequences

- The Business aggregate must track which reasons are present and emit `BusinessBookabilityChanged` only when the externally visible reason set changes.
- Future onboarding commands must know which reasons to add or remove.
- The `BusinessBookabilityChanged` event must carry the full current reason set, not deltas.
