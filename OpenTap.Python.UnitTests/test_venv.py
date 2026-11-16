"""Exercise a configured virtual environment in fresh OpenTAP processes.

Usage: python test_venv.py path/to/tap [path/to/base/python [path/to/python/library]]
The OpenTAP installation's Python settings are restored when the test finishes.
"""
import os
from pathlib import Path
import subprocess
import sys
import tempfile


def run(*args, env=None, cwd=None):
    result = subprocess.run(args, env=env, cwd=cwd, text=True, stdout=subprocess.PIPE,
                            stderr=subprocess.STDOUT)
    print(result.stdout, end='')
    if result.returncode:
        raise AssertionError(f'{args} returned {result.returncode}')
    return result.stdout


def main():
    tap = Path(sys.argv[1]).resolve()
    # Keep the selected executable's path: resolving a symlink can select a
    # different framework installation on macOS.
    python = Path(os.path.abspath(sys.argv[2] if len(sys.argv) > 2 else sys.executable))
    explicit_library = Path(os.path.abspath(sys.argv[3])) if len(sys.argv) > 3 else None
    if explicit_library is not None and not explicit_library.is_file():
        raise FileNotFoundError(f'Python library does not exist: {explicit_library}')
    settings = tap.parent / 'Settings' / 'Python.xml'
    original_settings = settings.read_bytes() if settings.exists() else None
    try:
        with tempfile.TemporaryDirectory(prefix='opentap venv ') as tmp:
            # On macOS /var is a symlink to /private/var. Python resolves the
            # site-packages path, so use the same canonical path for assertions.
            root = Path(tmp).resolve()
            venv = root / 'venv with spaces'
            run(str(python), '-m', 'venv', str(venv))
            executable = venv / ('Scripts/python.exe' if os.name == 'nt' else 'bin/python')
            pip_env = dict(os.environ)
            pip_env.pop('PYTHONPATH', None)
            pip_env.pop('PYTHONHOME', None)
            run(str(executable), '-m', 'pip', 'install', 'numpy', env=pip_env)
            site = Path(subprocess.check_output(
                [str(executable), '-c', 'import sysconfig; print(sysconfig.get_path("purelib"))'],
                text=True).strip())
            # Install the venv before subsequent setup commands launch OpenTAP,
            # including the first process that discovers the example plugins.
            settings.parent.mkdir(parents=True, exist_ok=True)
            settings.write_text('<?xml version="1.0" encoding="utf-8"?>\n'
                                '<PythonSettings type="OpenTap.Python.PythonSettings" />\n',
                                encoding='utf-8')
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
            # Prefer the shared library used by the Python executable that created
            # the venv, rather than another discovered installation.
            library = subprocess.check_output([str(python), '-c',
                'import os, sys, sysconfig; '
                'print(os.path.join(os.path.dirname(sys.executable), "python%d%d.dll" % sys.version_info[:2]) '
                'if os.name == "nt" else os.path.join(sysconfig.get_config_var("LIBDIR") or "", '
                'sysconfig.get_config_var("LDLIBRARY") or ""))'], text=True).strip()
            run(str(tap), 'python', 'set-virtual-environment', str(venv), env=env)
            run(str(tap), 'python', 'set-path', str(python.parent), env=env)
            if explicit_library is not None:
                library = str(explicit_library)
            if library and Path(library).is_file():
                run(str(tap), 'python', 'set-lib-path', library, env=env)
            clean_env = dict(env)
            clean_env.pop('PYTHONPATH', None)
            clean_env.pop('OPENTAP_TEST_EXTERNAL', None)
            run(str(tap), 'python', 'test-venv', env=clean_env)
            run(str(tap), 'python', 'test-venv', env=env)
            # The existing test deletes its settings using a relative path.
            # Run from the installation directory and restore that file afterward.
            test_settings = tap.parent / 'Settings' / 'Settings Test.xml'
            original_test_settings = test_settings.read_bytes() if test_settings.exists() else None
            try:
                run(str(tap), 'python', 'test', env=clean_env, cwd=str(tap.parent))
            finally:
                if original_test_settings is None:
                    if test_settings.exists():
                        test_settings.unlink()
                else:
                    test_settings.write_bytes(original_test_settings)
            plan_output = run(str(tap), 'run', str(tap.parent / 'test.TapPlan'), '--non-interactive',
                              env=clean_env, cwd=str(root))
            if 'Generated 32x2 Points' not in plan_output:
                raise AssertionError('NumPy example step did not generate results')
            run(str(tap), 'python', 'set-virtual-environment', '--unset', env=env)
            (external / 'opentap_venv_marker.py').unlink()
            missing = subprocess.run([str(tap), 'python', 'test-venv'], env=env,
                                     text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
            if missing.returncode == 0 or 'opentap_venv_marker' not in missing.stdout:
                print(missing.stdout, end='')
                raise AssertionError('Marker unexpectedly importable without virtual environment')
            print('Expected failure: marker is unavailable after unsetting the virtual environment.')
            invalid = root / 'not a venv'
            invalid.mkdir()
            # Plugin discovery may report NumPy missing when no venv is set;
            # this command deliberately transitions to an invalid environment.
            setup = subprocess.run([str(tap), 'python', 'set-virtual-environment', str(invalid)],
                                   env=env, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
            if setup.returncode:
                print(setup.stdout, end='')
                raise AssertionError('Failed to set invalid environment for negative test')
            failed = subprocess.run([str(tap), 'python', 'test-venv'], env=env,
                                    text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
            if failed.returncode == 0 or 'Virtual environment site-packages not found' not in failed.stdout:
                print(failed.stdout, end='')
                raise AssertionError('Invalid virtual environment was not reported')
            print('Expected failure: invalid virtual environment was rejected.')
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
