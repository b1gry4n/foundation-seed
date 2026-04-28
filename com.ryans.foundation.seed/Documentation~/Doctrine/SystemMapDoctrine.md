# System Map Doctrine

## Purpose
- System maps are required architecture memory and navigation shortcuts.
- They reduce repeated full-project rescans by keeping structured ownership maps current.

## Rules
- Every major subsystem should have a system map entry.
- Entry points, ownership boundaries, contracts, and validation steps should be documented.
- Runtime authority changes must update the corresponding system map in the same change pass.
- Stale system maps are maintenance defects and should be corrected immediately.
- If project code updates outpace system map updates by more than 7 days, treat as stale and fix before continuing.
- Decommissioned systems must be recorded in the project deprecated ledger before removal.
