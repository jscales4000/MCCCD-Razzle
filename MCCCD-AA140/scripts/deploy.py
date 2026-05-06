"""
Deploy MCCCD-AA140.ch5z to a Crestron touchpanel via SFTP + SSH PROJECTLOAD.

Why this exists:
  ch5-cli deploy prompts for SFTP credentials interactively with no -p flag —
  hangs forever in non-TTY shells (Claude Code, CI, scripted automation).
  paramiko is the proven workaround documented across multiple FRED guides.

Default target: TS-1070 Tabletop @ 192.168.2.53 (admin/password).
Override via env vars: PANEL_HOST, PANEL_USER, PANEL_PASS, PANEL_DIR.

Usage:
  python scripts/deploy.py                  # uses output/MCCCD-AA140.ch5z
  python scripts/deploy.py path/to.ch5z     # custom archive

Returns exit 0 on success, 1 on failure. Prints concise progress.
"""

import os
import sys
import time
from pathlib import Path

import paramiko


HOST = os.environ.get("PANEL_HOST", "192.168.2.53")
USER = os.environ.get("PANEL_USER", "admin")
PASS = os.environ.get("PANEL_PASS", "password")
REMOTE_DIR = os.environ.get("PANEL_DIR", "/display")
PROJECT_NAME = "MCCCD-AA140"


def deploy(archive_path: Path) -> int:
    if not archive_path.exists():
        print(f"[deploy] ERROR: archive not found: {archive_path}", file=sys.stderr)
        return 1

    size_kb = archive_path.stat().st_size / 1024
    remote_filename = archive_path.name
    remote_path = f"{REMOTE_DIR}/{remote_filename}"

    print(f"[deploy] {archive_path.name} ({size_kb:.0f} KB) -> {USER}@{HOST}:{remote_path}")

    ssh = paramiko.SSHClient()
    ssh.set_missing_host_key_policy(paramiko.AutoAddPolicy())

    t0 = time.time()
    try:
        ssh.connect(
            hostname=HOST,
            username=USER,
            password=PASS,
            allow_agent=False,
            look_for_keys=False,
            timeout=15,
        )
    except paramiko.AuthenticationException:
        print(f"[deploy] ERROR: auth failed for {USER}@{HOST} — check PANEL_PASS", file=sys.stderr)
        return 1
    except Exception as exc:
        print(f"[deploy] ERROR: SSH connect failed: {exc}", file=sys.stderr)
        return 1

    try:
        sftp = ssh.open_sftp()
        try:
            sftp.put(str(archive_path), remote_path)
            print(f"[deploy] SFTP upload done in {time.time() - t0:.1f}s")
        finally:
            sftp.close()

        # PROJECTLOAD reloads the panel UI from the new ch5z
        cmd = f"PROJECTLOAD {remote_filename}"
        print(f"[deploy] SSH: {cmd}")
        stdin, stdout, stderr = ssh.exec_command(cmd, timeout=30)
        out = stdout.read().decode(errors="replace").strip()
        err = stderr.read().decode(errors="replace").strip()
        if out:
            print(f"[deploy] stdout: {out}")
        if err:
            print(f"[deploy] stderr: {err}")
    finally:
        ssh.close()

    print(f"[deploy] OK in {time.time() - t0:.1f}s — panel will restart UI")
    return 0


def main() -> int:
    if len(sys.argv) > 1:
        archive = Path(sys.argv[1])
    else:
        # Default: <repo>/output/MCCCD-AA140.ch5z (relative to script location)
        archive = Path(__file__).resolve().parent.parent / "output" / f"{PROJECT_NAME}.ch5z"
    return deploy(archive)


if __name__ == "__main__":
    sys.exit(main())
