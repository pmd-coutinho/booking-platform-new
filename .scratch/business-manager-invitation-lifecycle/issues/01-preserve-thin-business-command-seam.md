Status: ready-for-agent

# Preserve Thin Business Command Seam

## Parent

`.scratch/business-manager-invitation-lifecycle/PRD.md`

## What to build

Refactor the Business command path so Server endpoints remain thin while existing Create Business behavior stays externally unchanged. This slice should preserve the current ability for a Platform Admin to create a Registered Business and initial Business Manager Invitation, but move persistence/event-store operations behind the appropriate application or handler seam so future Business Manager Invitation lifecycle commands do not repeat endpoint-level infrastructure coupling.

The completed slice should be observable through the existing Create Business HTTP behavior and through architecture tests that protect the Critter Stack boundary.

## Acceptance criteria

- [ ] Valid Create Business requests still create a Registered Business, initial Business Manager Invitation, and initial Unbookable Bookability state.
- [ ] Create Business events still persist in one Business stream transaction.
- [ ] Actor metadata for Create Business events is still persisted as event metadata/envelope data, not event payload fields.
- [ ] Invalid Create Business requests still reject without appending events.
- [ ] Feature endpoints do not directly depend on Marten, JasperFx event APIs, Npgsql, or Wolverine.Marten infrastructure details.
- [ ] Domain decision code remains testable without HTTP transport or persistence infrastructure.
- [ ] Existing Create Business unit, integration, and architecture tests pass or are updated to assert the same external behavior through the intended seam.

## Blocked by

None - can start immediately
