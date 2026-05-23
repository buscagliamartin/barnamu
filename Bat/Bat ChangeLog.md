# BarnaMu BAT Scripts ChangeLog

## Objective

Simplify the BarnaMu maintenance scripts so there are fewer files, less duplicated logic, cleaner output, and safer behavior when building, starting, stopping, backing up, and pushing the server files.

The main idea is:

- `BarnaMu.bat` is the central control script.
- Small wrapper `.bat` files only exist for double-click convenience or Task Scheduler compatibility.

---

## Current BAT Files

| File | Purpose | When to use |
|---|---|---|
| `BarnaMu.bat` | Main control script with all commands. | Use from CMD or PowerShell when you want full control. |
| `Recompilar.bat` | Wrapper for `BarnaMu.bat build`. | Double-click after modifying server source code. |
| `Backup.bat` | Wrapper for `BarnaMu.bat backup`. | Double-click when you only want a database backup. |
| `Push.bat` | Wrapper for `BarnaMu.bat push`. | Use from CMD/PowerShell with a commit message. |
| `AutoRestart.bat` | Starts web, then runs the server auto-restart loop. | Good candidate for Windows Task Scheduler at boot. |
| `StartServer.bat` | Wrapper for `BarnaMu.bat start server`. | Mostly redundant; kept temporarily for compatibility. |

Removed redundant files:

- `StartWeb.bat`
- `STOP_ALL.bat`
- `STOP_SERVER.bat`
- `STOP_WEB.bat`

---

## Normal Usage

### After modifying server files

Double-click:

```bat
Recompilar.bat
```

Equivalent command:

```bat
BarnaMu.bat build
```

This stops OpenMU processes, cleans `Startup\bin` and `Startup\obj`, then builds OpenMU in Release mode with quiet output.

---

### Start server and web

From PowerShell:

```powershell
.\BarnaMu.bat start all
```

From CMD:

```bat
BarnaMu.bat start all
```

This starts both:

- BarnaMuWeb
- OpenMU server

Each one runs in its own auto-restart window.

---

### Stop server and web

From PowerShell:

```powershell
.\BarnaMu.bat stop all
```

From CMD:

```bat
BarnaMu.bat stop all
```

This stops both the game server and the web process.

---

### Backup database only

Double-click:

```bat
Backup.bat
```

Equivalent command:

```bat
BarnaMu.bat backup
```

Creates:

```bat
C:\MuDev\Backups\barnamu_daily.sql
```

On Sundays, it also refreshes:

```bat
C:\MuDev\Backups\barnamu_weekly.sql
```

---

### Backup, commit, and push to GitHub

From PowerShell:

```powershell
.\Push.bat "your commit message"
```

From CMD:

```bat
Push.bat "your commit message"
```

Equivalent command:

```bat
BarnaMu.bat push "your commit message"
```

This runs a database backup first, stages changed files, commits only if there are changes, then pushes to GitHub.

If the current Git branch has no upstream branch yet, the push command should automatically run the equivalent of:

```bat
git push --set-upstream origin <current-branch>
```

Normal output should stay compact:

```bat
[backup] ...
[git] Preparing commit...
[git] 12 files changed, 100 insertions(+), 20 deletions(-)
[git] Committing...
[git] Pushing...
[ok] Push completed.
```

It should no longer print every staged file path during normal pushes.

---

### Check running processes

From PowerShell:

```powershell
.\BarnaMu.bat status
```

From CMD:

```bat
BarnaMu.bat status
```

Shows matching BarnaMu/OpenMU/nginx processes.

Also shows:

- Internal web port `8081`
- Public web port `80`
- Public HTTPS port `443`

---

## Full `BarnaMu.bat` Command List

```bat
BarnaMu.bat build
BarnaMu.bat start server
BarnaMu.bat start web
BarnaMu.bat start nginx
BarnaMu.bat start all
BarnaMu.bat stop server
BarnaMu.bat stop web
BarnaMu.bat stop nginx
BarnaMu.bat stop all
BarnaMu.bat backup
BarnaMu.bat push "commit message"
BarnaMu.bat status
```

If `BarnaMu.bat` is double-clicked without arguments, it only shows the help menu.

---

## Suggested Daily Workflow

### Editing server code

```bat
Recompilar.bat
BarnaMu.bat stop all
BarnaMu.bat start all
```

### Saving work to GitHub

```bat
Push.bat "describe the change"
```

### Server startup after Windows reboot

Use Task Scheduler to run:

```bat
AutoRestart.bat
```

---

## Notes

- `StartServer.bat` is currently redundant because `BarnaMu.bat start server` does the same job.
- `AutoRestart.bat` is useful if Task Scheduler needs one simple double-clickable entry point.
- A future cleanup can remove `StartServer.bat` if no Task Scheduler entry or shortcut depends on it.
- `BarnaMu.bat start web` starts both BarnaMuWeb and nginx.
- `BarnaMu.bat start all` starts BarnaMuWeb, nginx, and the OpenMU server.
- nginx is expected at `C:\MuDev\nginx\nginx.exe`.
- `BarnaMu.bat start web` and `BarnaMu.bat start all` check the configured web port before launching BarnaMuWeb.
- Default web port is `8081`. Override it with `BARNAMU_WEB_PORT` if needed.
- Public `https://barnamu.ddns.net/` requires nginx to be running on port `443`.
- If the web fails with `address already in use`, run:

```bat
BarnaMu.bat status
BarnaMu.bat stop web
BarnaMu.bat start web
```

- A future convenience wrapper could be added:

```bat
StartAll.bat
```

with:

```bat
@echo off
call "%~dp0BarnaMu.bat" start all
exit /b %ERRORLEVEL%
```

That would allow starting server + web by double-clicking.

