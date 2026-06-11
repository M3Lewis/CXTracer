# Product Requirements Document

## Goal and User Value
Users need to view the original full-size screenshots/images from the session logs directly inside the app. If the local temp file of the image has been deleted, the app should fall back to extracting and resolving it from the base64 data URL in the raw JSON payload. Clicking on the image inside any card should open a floating viewer pop-up overlay without changing the card's active selection.

## Confirmed Facts
- Temp files (e.g. `codex-clipboard-xxx.png`) might be cleaned up locally.
- Base64 data URLs exist in developer/user payload JSON fields.
- Visual virtualization is disabled.
- Localization is required for toggle button strings in Chinese and English.

## Requirements
1. **Placeholder & Base64 Resolution**:
   - Scan event JSON payloads for base64 image data and local temp path associations.
   - Replace missing local paths with their corresponding base64 data URLs.
2. **Floating Image Viewer Pop-up**:
   - Render a modal overlay at window level with a scrollable container.
   - Provide "Fit Window" (Uniform) and "Original Size" (None) stretch modes.
   - Close on click outside, Escape key, or close button.
3. **Card-Selection Avoidance**:
   - Wrap images in a transparent Button with a hand cursor.
   - Trigger the viewer popup overlay without altering the selected card.

## Acceptance Criteria
- Clicking an event card image opens the viewer.
- The viewer fits/scrolls correctly depending on the scale mode.
- Missing images resolve to base64 fallback where available in the session.
- Card selection remains unchanged when the image is clicked.
