# CHANGELOG

## 2026-03-20

- Prevented owner self-lockout by blocking removal of the owner recipient entry and by validating that the owner recipient remains present before saving.
- Restricted block creation to sessions opened with the owner key in both the UI and the container API.
- Stabilized background hash calculation by hashing against local snapshots and suppressing stale UI updates after cancellation or container switches.
- Changed container-wide search to skip blocks without read permission instead of aborting the full search.
- Fixed whole-word search in container text so it continues past invalid intermediate matches and only treats letters, digits, and `_` as word characters.
