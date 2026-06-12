Status: completed

## Parent

PRD: `.scratch/complete-business-profile/PRD.md`

## What to build

Update the existing Business aggregate and `BusinessManagerInvitationAccepted` event application to replace the single `OnboardingIncomplete` reason with granular reasons: `ProfileIncomplete`, `NoStaffMembers`, `NoAppointmentTypes`, `NoStaffCapabilities`, `NoBusinessHours`, `NoStaffAvailability`.

After `BusinessCreated`, the reason set should be `ManagerNotAccepted` only. After `BusinessManagerInvitationAccepted`, the reason set should become the full set of granular onboarding blockers (`ProfileIncomplete`, `NoStaffMembers`, etc.). This is a deliberate transition that makes the missing setup steps visible and actionable.

Update all existing integration and unit tests to match the new behavior.

## Acceptance criteria

- [x] `BusinessManagerInvitationAccepted` event application sets `BookabilityReasons` to `ProfileIncomplete`, `NoStaffMembers`, `NoAppointmentTypes`, `NoStaffCapabilities`, `NoBusinessHours`, `NoStaffAvailability`.
- [x] `BusinessCreated` event application sets `BookabilityReasons` to `ManagerNotAccepted` only.
- [x] All existing integration tests pass with the new granular reason set.
- [x] All existing unit tests pass with the new granular reason set.
- [x] No `OnboardingIncomplete` string remains in the codebase.

## Blocked by

None - can start immediately.
