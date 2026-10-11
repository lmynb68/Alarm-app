# Release validation

## Automatic

Build.cmd compiles and runs the bundled application self-tests before packaging. The tests cover finite counts, date boundaries, history persistence, language preference, icon loading and native sound-file opening. GitHub compilation CI builds AlarmApp.sln and saves the compiled output; it does not run those native sound tests on the hosted runner.

## Manual Windows checks

- Extract portable ZIP into a new writable directory and launch without Codex.
- Install setup ZIP as a standard user; verify app files, desktop/Start menu shortcuts and uninstall entry.
- Trigger a scheduled alarm; stop and snooze it.
- Set a 1-minute interval with 3 repeats; confirm count and automatic disable.
- Preview every built-in sound and import WAV/MP3.
- Switch English/Simplified Chinese; reopen and check preference and filters.
- Verify tray close/reopen/exit and duplicate-instance handling.
- Check daily/monthly history, midnight split and data preservation on update.
- Test sleep wake-up separately on the target hardware.
- Uninstall after exiting; verify only program files/shortcuts are removed and user data is retained.

These manual checks have not yet been completed in the restricted Codex environment. Record results before describing a release as fully tested.
