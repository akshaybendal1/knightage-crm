# Knightage CRM: closed testing playbook (Personal developer account)

Why this exists: a **Personal** Play developer account created after 13 Nov 2023 cannot publish to production until a **closed test** has run with
**at least 12 testers opted in for 14 continuous days**, and Google has then approved the production-access application. Organization accounts are
exempt; ours is Personal, so this is on the critical path. These rules are Google's and have changed before: re-read the Console's own wording
(Testing > Closed testing, and the "Apply for production" card on the dashboard) before relying on any number here.

Internal testing does **not** count. Only a closed-testing track does.

## Timeline

| Step | Who | Time |
|---|---|---|
| Developer account verification finishes | Google | days (in progress) |
| Create the app, fill the App content forms, upload the .aab to a closed track | you (+ me for the answers) | 1 day |
| Google reviews the first closed release | Google | usually a few days |
| Recruit and opt in 12+ testers (aim for 15-20) | you | start now, before the track is live |
| Run the test, with real use | testers | **14 days after 12 are opted in** |
| Apply for production access | you | same day the 14 days are up |
| Google reviews the application | Google | up to ~7 days |
| Production release | you | after approval |

Realistic total from "verification done": about 4-5 weeks. Everything that can be prepared before verification completes should be.

## 1. Before inviting anyone

- [ ] Verification finished and the app created in Play Console (`com.knightage.crm`, default language, App, Free).
- [ ] App content forms done: see `play-data-safety.md` (Data safety, Ads = No, Advertising ID = No, target audience 18+, content rating, App access).
- [ ] Store listing filled in from `play-store-listing.md`, images from `play-store/` (8 phone screenshots, feature graphic, 512 icon).
- [ ] Release: Testing > Closed testing > create track > Create new release > upload `app-release.aab` (versionCode 2 / 1.0.1) > release notes > roll out.
      Enroll in Play App Signing when asked. Then add Play's signing SHA-1 to the Android OAuth client in Google Cloud project 38648420321,
      or Google sign-in will fail for people who install from Play.
- [ ] **A working login for every tester.** The app needs an organization on a CRM plan (see "Tester logins" below).
- [ ] Set the **feedback email** on the track (contact@knightagetech.com) so testers can reach you from Play.

## 2. Tester logins (needs your decision)

Google checks that testers actually *use* the app, so each tester needs a login that works, not a sign-up that dead-ends at "no subscription".
Recommended: one throwaway organization, for example "Play Testing", with an **active subscription** assigned in Platform (Pro plan allows 20 users),
and each tester added as a **Member** (or Sales) user from the Team screen with their own email. The same organization can serve as the Play review
login (App access). Using real customer data here is not allowed; seed it with sample leads. This creates real rows on production, so it
needs your OK first. A passwords-in-a-file approach is not needed: share each login privately and ask testers to change the password.

## 3. Recruiting 12+ testers

- Need: people with an **Android phone** and a **Google account** (the Gmail the Play Store uses on that phone). iPhones do not count.
- Aim for 15-20 so a few drop-outs do not reset the count. A tester who leaves the test removes themselves from the total.
- Good sources: your own team and family, friends in sales, existing contacts who run small businesses, the 15-20 people most likely to reply
  within a day. Testers may be anyone; they do not need to be customers.
- Add them to the track by email list or, easier to manage, a **Google Group** (Play adds everyone in the group; you add or remove people in the Group).
- Each tester must open the **opt-in link** from the Console, accept, and install from Play. Being on the list is not enough.
- Keep a simple tracker: name, Gmail, phone model, opted in (date), installed (Y/N), used the app on days 3/7/14 (Y/N).

### Message to send (edit freely)

> Hi! I'm launching an Android app, Knightage CRM, a sales app for tracking leads, follow-ups and field visits, and Google needs 12 people to try it for
> 2 weeks before it can go live. Could you help? It takes about 5 minutes to set up, then a couple of minutes a day.
> 1) Send me the Gmail you use on your Android phone. 2) I'll add you, then send a link. Open it, tap "Become a tester", then install the app from Google Play.
> 3) I'll send your login. Please stay opted in for the full 14 days (don't leave the test) and open the app a few times, add a lead, a task, and tap around.
> If anything looks wrong, tell me or use the feedback option in Play. Thank you!

### What testers should try (they do not need to do all of it)

Sign in; add a lead and move it between stages; add a task with a due date; open the Timeline board; create a quotation from a lead and download the PDF;
(Sales role) log a field visit and allow location when asked; turn notifications on or off; sign out and in again.
Ask them to open the app on at least 3 different days. Google looks at real engagement, not only installs.

## 4. During the 14 days

- Check the Console's closed-test page every few days: opted-in count, installs. If it falls under 12, recruit more at once: the clock needs 12 continuously.
- Reply to feedback quickly and **fix real problems**. Web fixes ship instantly (the app loads the live site); native changes need a new .aab.
  Keep a short log of "feedback received, what we changed": Google asks for exactly this in the production application.
- Do not change the track's tester list in a way that drops below 12.

## 5. Production-access application (draft answers)

Google asks about your testing, the app, and readiness. Answer truthfully from the real log. Starting points:

- **How did you recruit testers?** Direct invitations to people I know with Android phones (team, contacts and small-business owners), managed through a Google Group.
- **How many testers and for how long?** N testers opted in for at least 14 continuous days. (Use the real numbers.)
- **What feedback did you get, and what did you change?** (From the log.)
- **Who is the app for?** Sales teams that manage leads, follow-ups, quotations and field visits. Organizations sign up on the web; team members are invited by their admin.
- **Is the app ready?** It has been in a closed test for 14+ days, and the issues found were fixed. Privacy policy, account deletion and Data safety are complete.
- **Expected installs / how will you grow?** Direct outreach to small businesses in India; web sign-up at crm.knightagetech.com.

## 6. Risks

- Fewer than 12 opted in at any point resets the count (Google counts continuously).
- Testers who install but never open the app may not count as active. Remind them.
- Google sign-in on Play-installed builds needs Play's signing SHA-1 registered (step 1); test it yourself on one phone first.
- If production access is refused, Google gives reasons; fix, keep the test running, and re-apply.
- Policy numbers (12 testers / 14 days) can change; confirm in the Console on the day you start.
