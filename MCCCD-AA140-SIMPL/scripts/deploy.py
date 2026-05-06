"""
Deploy MCCCD-AA140.cpz to a Crestron 4-Series processor via SFTP + SSH PROGLOAD.

Mirrors the panel deploy.py pattern (paramiko, non-interactive). Default target
is the AA140 RMC4 at 192.168.1.191, slot 01.

Usage:
  python scripts/deploy.py                          # default: slot 01
  python scripts/deploy.py path/to/MCCCD-AA140.cpz  # custom archive
  PROC_SLOT=02 python scripts/deploy.py             # slot override

Env overrides: PROC_HOST, PROC_USER, PROC_PASS, PROC_SLOT.
"""

import os
import sys
import time
from pathlib import Path

import paramiko


HOST = os.environ.get("PROC_HOST", "192.168.1.191")
USER = os.environ.get("PROC_USER", "admin")
PASS = os.environ.get("PROC_PASS", "password")
SLOT = os.environ.get("PROC_SLOT", "01")
PROJECT = "MCCCD-AA140"


def deploy(archive_path: Path, slot: str) -> int:
    if not archive_path.exists():
        print(f"[deploy] ERROR: archive not found: {archive_path}", file=sys.stderr)
        return 1

    size_mb = archive_path.stat().st_size / (1024 * 1024)
    remote_dir = f"/program{slot}"
    remote_path = f"{remote_dir}/{archive_path.name}"

    print(f"[deploy] {archive_path.name} ({size_mb:.1f} MB) -> {USER}@{HOST}:{remote_path}")

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
        print(f"[deploy] ERROR: auth failed for {USER}@{HOST}", file=sys.stderr)
        return 1
    except Exception as exc:
        print(f"[deploy] ERROR: SSH connect failed: {exc}", file=sys.stderr)
        return 1

    try:
        sftp = ssh.open_sftp()
        try:
            try:
                sftp.stat(remote_dir)
            except IOError:
                print(f"[deploy] mkdir {remote_dir}")
                sftp.mkdir(remote_dir)
            sftp.put(str(archive_path), remote_path)
            print(f"[deploy] SFTP upload done in {time.time() - t0:.1f}s")
        finally:
            sftp.close()

        # PROGLOAD -P:NN  → wipes app folder (except the .cpz/.lpz),
        # extracts the new archive, registers it, and starts the program.
        cmd = f"PROGLOAD -P:{slot}"
        print(f"[deploy] SSH: {cmd}")
        stdin, stdout, stderr = ssh.exec_command(cmd, timeout=180)
        out = stdout.read().decode(errors="replace").strip()
        err = stderr.read().decode(errors="replace").strip()
        if out:
            print(f"[deploy] --- PROGLOAD output ---")
            print(out)
            print(f"[deploy] --- end PROGLOAD ---")
        if err:
            print(f"[deploy] PROGLOAD stderr: {err}")

        # Settle, then verify
        time.sleep(2)
        stdin, stdout, _ = ssh.exec_command(f"proginfo -p:{slot}", timeout=30)
        info = stdout.read().decode(errors="replace").strip()
        print(f"[deploy] --- proginfo -p:{slot} ---")
        print(info)
        print(f"[deploy] --- end proginfo ---")
    finally:
        ssh.close()

    print(f"[deploy] OK in {time.time() - t0:.1f}s")
    return 0


def main() -> int:
    if len(sys.argv) > 1:
        archive = Path(sys.argv[1])
    else:
        # Default: bin/Release/net6.0/MCCCD-AA140.cpz relative to repo root
        archive = (
            Path(__file__).resolve().parent.parent
            / "MCCCD-AA140"
            / "bin"
            / "Release"
            / "net6.0"
            / f"{PROJECT}.cpz"
        )
    return deploy(archive, SLOT)


if __name__ == "__main__":
    sys.exit(main())
