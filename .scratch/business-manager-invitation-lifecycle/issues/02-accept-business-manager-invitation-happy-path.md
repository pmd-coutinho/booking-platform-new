Status: done

# Accept Business Manager Invitation Happy Path

## Parent

`.scratch/business-manager-invitation-lifecycle/PRD.md`

## What to build

Build the successful end-to-end path for a future Business Manager to accept a pending Business Manager Invitation. The Server should accept the Business identity, invitation identity, and manager email identity, reconstruct the current Business state from its event stream, verify the pending invitation belongs to that normalized email identity, append the accepted invitation fact, and update Bookability Reasons so the Business is still Unbookable only because Business Onboarding remains incomplete.

The completed slice should be demoable by creating a Business, accepting its initial Business Manager Invitation, and inspecting the response and Business stream.

## Acceptance criteria

- [x] A valid acceptance request for the initial Business Manager Invitation succeeds.
- [x] The acceptance route is scoped by both Business identity and invitation identity.
- [x] Manager email identity is normalized before matching and returning acceptance state.
- [x] Acceptance reconstructs Business state from Business stream history before deciding.
- [x] Successful acceptance appends `BusinessManagerInvitationAccepted` with invitation identity, normalized manager email, and server-supplied accepted time.
- [x] Successful acceptance appends `BusinessBookabilityChanged` because `ManagerNotAccepted` is removed.
- [x] After acceptance, Bookability status remains `Unbookable`.
- [x] After acceptance, Bookability Reasons contain `OnboardingIncomplete` and do not contain `ManagerNotAccepted`.
- [x] The acceptance response includes Business identity, invitation identity, normalized manager email, Bookability status, and Bookability Reasons.
- [x] Unit tests cover Business event reconstruction and the successful acceptance domain decision without infrastructure concerns.
- [x] Integration tests cover the successful HTTP behavior and persisted Business stream facts.

## Blocked by

- `.scratch/business-manager-invitation-lifecycle/issues/01-preserve-thin-business-command-seam.md`
