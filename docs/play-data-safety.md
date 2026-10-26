# Knightage CRM: Google Play "App content" answers (draft)

Fill these into Play Console > App content. Every answer below was checked against the shipped build
(`com.knightage.crm`, versionCode 2 / 1.0.1) and the web app it loads, not guessed. Re-check this sheet whenever an SDK or a data field is added:
Google compares the declaration with the binary and with the privacy policy.

What was verified
- Libraries inside the release bundle: Firebase Cloud Messaging (push), Google Play services Auth + Credential Manager (Google sign-in),
  AndroidX Biometric (pulled in by Credential Manager, not used by us). **No** analytics, advertising, attribution or crash-reporting SDK
  (no `firebase-analytics`, no ads library). The web app has no tracking scripts either.
- Merged manifest permissions: INTERNET, ACCESS_NETWORK_STATE, POST_NOTIFICATIONS, ACCESS_FINE_LOCATION, ACCESS_COARSE_LOCATION,
  WAKE_LOCK / C2DM receive (push), plus USE_BIOMETRIC / USE_FINGERPRINT / USE_CREDENTIALS merged in by the sign-in library.
  **No** advertising-ID permission, no background location, no contacts/SMS/call-log/camera/microphone/storage permission.
- Traffic: the app loads `https://crm.knightagetech.com` only (HTTPS).

## 1. Data safety form

**Does your app collect or share any of the required user data types?** Yes
**Is all of the user data collected by your app encrypted in transit?** Yes (HTTPS only)
**Do you provide a way for users to request that their data is deleted?** Yes
- Deletion URL: `https://crm.knightagetech.com/delete-account`
- In the app: Profile > Delete account (organization owners email a request from that page).

"Shared" is **No** for every row. Data goes only to Knightage's own servers, plus Google Firebase Cloud Messaging for the push token, and Google
for sign-in. Google's rules treat a service provider that processes data on your behalf as not "sharing". If you want to be extra
conservative, mark the Device ID row as shared; it does no harm.

| Category > type | Collected | Required or optional | Purpose | Why |
|---|---|---|---|---|
| Location > Precise location | Yes | Optional | App functionality | GPS position attached to a Field Sales visit, read only at the moment the visit is saved, foreground only, never in the background. Used only if the user logs visits. |
| Personal info > Name | Yes | Required | App functionality, Account management | Account name; also the names of leads and contacts that users enter. |
| Personal info > Email address | Yes | Required | App functionality, Account management | Sign-in identity; also lead contact emails users enter. |
| Personal info > Phone number | Yes | Optional | App functionality, Account management | Optional profile phone; lead phone numbers users enter. |
| Personal info > User IDs | Yes | Required | App functionality, Account management | Internal account id; Google account id when signing in with Google. |
| Personal info > Address | Yes | Optional | App functionality | Optional organization contact address on the Organization page. |
| Photos and videos > Photos | Yes | Optional | App functionality | Optional organization logo (PNG/JPEG) uploaded by an admin and printed on quotations. |
| App activity > Other user-generated content | Yes | Required | App functionality | Lead notes, tasks, meetings, message templates, quotation and invoice content. |
| App info and performance, Messages, Contacts, Calendar, Files and docs, Health, Audio, Web browsing | **No** | | | See notes below. |
| Device or other IDs | Yes | Optional | App functionality | Firebase Cloud Messaging registration token, used only to deliver task and meeting reminders. Optional because notifications can be declined. |
| Financial info | **No** | | | See notes below. |

Notes
- **Calendar: No.** Google Calendar sync is switched off (`googleCalendarEnabled = false`). Meetings are records inside the CRM, not
  the device or Google calendar. The old Sept 20 checklist suggested declaring Calendar; that is superseded.
- **Files and docs: No.** The leads CSV import is parsed on the server and the file is not kept (ephemeral processing). A file chosen for
  a WhatsApp message stays on the device and is handed to WhatsApp's own share sheet, never uploaded to us.
- **Messages: No.** WhatsApp messages are composed and sent inside WhatsApp; the CRM only stores the user's own message templates.
- **Financial info: No (judgement call).** Quotations and invoices hold the organization's own sales figures, not the user's payment or
  financial data, and no card or bank details are collected. If you prefer to be conservative, add "Purchase history" as optional / app functionality.
- **Crash logs / diagnostics: No.** There is no crash-reporting SDK; only ordinary server logs.
- Third-party leads' names, emails and phone numbers are covered by the Name / Email / Phone rows above.

**Security practices:** encrypted in transit = Yes; users can request deletion = Yes (URL above);
committed to the Play Families policy = No (not a children's app); independent security review = No.

## 2. Other App content declarations

| Section | Answer |
|---|---|
| Privacy policy URL | `https://crm.knightagetech.com/privacy-policy` |
| Ads | No, the app contains no ads |
| Advertising ID | **No** (no ad SDK, no `AD_ID` permission in the merged manifest) |
| App access | Restricted. Provide a working reviewer login (throwaway organization; the password goes only in the Console form, never in this file). |
| Target audience | 18 and over; not designed for children |
| Content rating (IARC) | Category: Utility / Productivity (business tool). No violence, sexual content, profanity, gambling, drugs or user-to-user public content. **Location:** answer honestly that a user's location is visible to other users of the same organization (admins see the coverage map of a rep's visits). That adds a "shares location" note and does not raise the age rating much. |
| News / Government / Health / Financial-features apps | No / No / No / No (no payments, loans or trading) |
| Data deletion | Yes, URL above. Say that records belonging to the organization (leads, tasks, documents) are kept and the person's name, email, phone and password are removed (anonymized). |
| Permissions | Location is foreground only (field visit logging). No background-location declaration is needed. Notifications permission is runtime and optional. |
| Export compliance / encryption | Uses standard HTTPS/TLS only, so it is exempt from export licensing; the declaration still has to be ticked. |
| Target API level | 36, compliant |
