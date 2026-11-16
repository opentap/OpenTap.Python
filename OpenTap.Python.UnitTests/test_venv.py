"""Exercise a configured virtual environment in fresh OpenTAP processes.

Usage: python test_venv.py path/to/tap [path/to/base/python]
The OpenTAP installation's Python settings are restored when the test finishes.
"""
import os
from pathlib import Path
import subprocess
import sys
import tempfile


def run(*args, env=None):
    result = subprocess.run(args, env=env, text=True, stdout=subprocess.PIPE,
                            stderr=subprocess.STDOUT)
    print(result.stdout, end='')
    if result.returncode:
        raise AssertionError(f'{args} returned {result.returncode}')
    return result.stdout


def main():
    tap = Path(sys.argv[1]).resolve()
    python = Path(sys.argv[2] if len(sys.argv) > 2 else sys.executable).resolve()
    settings = tap.parent / 'Settings' / 'Python.xml'
    original_settings = settings.read_bytes() if settings.exists() else None
    try:
        with tempfile.TemporaryDirectory(prefix='opentap venv ') as tmp:
            root = Path(tmp)
            venv = root / 'venv with spaces'
            run(str(python), '-m', 'venv', str(venv))
            executable = venv / ('Scripts/python.exe' if os.name == 'nt' else 'bin/python')
            site = Path(subprocess.check_output(
                [str(executable), '-c', 'import sysconfig; print(sysconfig.get_path("purelib"))'],
                text=True).strip())
            (site / 'opentap_venv_marker.py').write_text('VALUE = 42\n')
            pth_dir = root / 'pth modules'
            pth_dir.mkdir()
            (pth_dir / 'opentap_pth_marker.py').write_text('VALUE = 1\n')
            (site / 'test.pth').write_text(str(pth_dir) + '\n')
            external = root / 'external modules'
            external.mkdir()
            (external / 'opentap_external_marker.py').write_text('VALUE = 1\n')
            # A conflicting external marker must not take precedence over the venv.
            (external / 'opentap_venv_marker.py').write_text('VALUE = 0\n')
            env = dict(os.environ, PYTHONPATH=str(external), OPENTAP_TEST_VENV=str(venv),
                       OPENTAP_TEST_EXTERNAL='1')
            env.pop('VIRTUAL_ENV', None)
            run(str(tap), 'python', 'set-path', str(python.parent), env=env)
            # Prefer the shared library used by the Python executable that created
            # the venv, rather than another discovered installation.
            library = subprocess.check_output([str(python), '-c',
                'import os, sys, sysconfig; '
                'print(os.path.join(os.path.dirname(sys.executable), "python%d%d.dll" % sys.version_info[:2]) '
                'if os.name == "nt" else os.path.join(sysconfig.get_config_var("LIBDIR") or "", '
                'sysconfig.get_config_var("LDLIBRARY") or ""))'], text=True).strip()
            if library and Path(library).is_file():
                run(str(tap), 'python', 'set-lib-path', library, env=env)
            run(str(tap), 'python', 'set-virtual-environment', str(venv), env=env)
            clean_env = dict(env)
            clean_env.pop('PYTHONPATH', None)
            clean_env.pop('OPENTAP_TEST_EXTERNAL', None)
            run(str(tap), 'python', 'test-venv', env=clean_env)
            run(str(tap), 'python', 'test-venv', env=env)
            run(str(tap), 'python', 'set-virtual-environment', '--unset', env=env)
            (external / 'opentap_venv_marker.py').unlink()
            missing = subprocess.run([str(tap), 'python', 'test-venv'], env=env,
                                     text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
            print(missing.stdout, end='')
            if missing.returncode == 0 or 'opentap_venv_marker' not in missing.stdout:
                raise AssertionError('Marker unexpectedly importable without virtual environment')
            invalid = root / 'not a venv'
            invalid.mkdir()
            run(str(tap), 'python', 'set-virtual-environment', str(invalid), env=env)
            failed = subprocess.run([str(tap), 'python', 'test-venv'], env=env,
                                    text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
            print(failed.stdout, end='')
            if failed.returncode == 0 or 'Virtual environment site-packages not found' not in failed.stdout:
                raise AssertionError('Invalid virtual environment was not reported')
            print('Virtual environment regression checks passed.')
    finally:
        if original_settings is None:
            if settings.exists():
                settings.unlink()
        else:
            settings.parent.mkdir(parents=True, exist_ok=True)
            settings.write_bytes(original_settings)


if __name__ == '__main__':
    main()
