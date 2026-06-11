# Share Only Aggregate No-Show Risk Across Businesses

Businesses may see an aggregate `No-Show Risk` signal for a booking contact, but they must not see another business's customer history, appointment details, appointment dates, or business relationship details. This preserves the usefulness of platform-wide no-show patterns while keeping each business's relationship with a customer confidential.

## Considered Options

- Showing full cross-business no-show history was rejected because it leaks another business's customer relationship data.
- Showing raw counts or rates was rejected because it can still expose more cross-business behavior than a business needs to make scheduling decisions.
- Showing no cross-business signal was rejected because it prevents the platform from helping businesses identify repeated no-show patterns.
