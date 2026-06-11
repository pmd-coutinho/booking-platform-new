# Event Source Domain State Changes

The write model will event-source meaningful booking-platform domain state changes, including business setup, staff, appointment types, staff capabilities, availability, and service appointment lifecycle. Operational concerns such as caches, outbox records, telemetry, and other infrastructure state are not part of the domain event model.

## Considered Options

- Event-sourcing only appointments was rejected because setup concepts like staff capabilities and availability directly explain why appointments can or cannot be scheduled.
- Event-sourcing every possible system change was rejected because operational data does not carry domain meaning and would make the event model noisy.
