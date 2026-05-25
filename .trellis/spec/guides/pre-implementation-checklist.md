# Pre-Implementation Checklist

Use this checklist before making code changes.

## Scope

- what layer or layers will change?
- is this a shell/UI concern, an application concern, or an infrastructure concern?
- does the change affect an existing convention already documented in this spec?

## Reuse

- did you search for an existing service, helper, control, or pattern first?
- are you adding something new that should extend an existing abstraction instead?

## Desktop Shell

If the change touches Avalonia or SukiUI:

- does it affect `App.axaml`, `SukiTheme`, or `SukiWindow`?
- does it require dialog or toast host changes?
- does it add or change `SukiSideMenu` page content?

## Data Flow

- where does the data originate?
- where is it validated?
- where is it transformed for persistence?
- where is it projected for the UI?

## Failure Modes

- what happens when the operation fails?
- what is the user supposed to see?
- what should be logged?

## Quality Gate

Before you start coding, be able to state:

1. which files or modules will change
2. which layer owns the new logic
3. which existing pattern you are following
4. what regression would be easiest to introduce if you get it wrong
