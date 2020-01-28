@echo on
set TARGETDIR=%TEMP%\python_gitlab_ci
set UNIQUEBUILDTOKEN="v3"
mkdir %TARGETDIR%

set PYTHONHOME=C:\Python27
set PYTHONPATH=C:\Python27\Lib
set BUILD_PY=2.7.14
set PATH=%PATH%;%PYTHONHOME%;%PYTHONHOME%\Scripts
set PYFILE=python-%BUILD_PY%.msi

set TARGETFILE=%TARGETDIR%\.py_installed
call :check_installed

if "%SHOULDINSTALL%"=="1" (
  wget http://gitlab.it.keysight.com/Rolf/Installers/raw/9157397983bbe99cb04f7a11e6b3e16920a65ad5/Python/python-2.7.14.msi
  echo "Fetched file"
  REM setx BUILD_PY %BUILD_PY%
  start /wait msiexec /a %PYFILE% /qn ALLUSERS=0
  echo "Installed Python"
  REM setx PATH "%PATH%"
  REM setx PYTHONHOME "%PYTHONHOME%"
  REM setx PYTHONPATH "%PYTHONPATH%"
  echo "%PATH%"
  echo %BUILD_PY%
  echo %PYTHONHOME%
  echo %PYTHONPATH%
  del %PYFILE%
  echo "Deleted installer"
  echo %UNIQUEBUILDTOKEN% > %TARGETFILE%
)

set TARGETFILE=%TARGETDIR%\.pynet_installed
call :check_installed
%PYTHONHOME%\python.exe get-pip.py
if "%SHOULDINSTALL%"=="1" (

  echo %UNIQUEBUILDTOKEN% > %TARGETFILE%
)
%PYTHONHOME%\Scripts\pip.exe install pip --upgrade
%PYTHONHOME%\Scripts\pip.exe install pythonnet --upgrade

goto :eof

:check_installed
  if exist %TARGETFILE% (
    type %TARGETFILE% | findstr %UNIQUEBUILDTOKEN%
    if errorlevel 1 (
      set SHOULDINSTALL=1
    ) else (
      set SHOULDINSTALL=0
    )
  ) else (
    set SHOULDINSTALL=1
  )
  goto :eof 
