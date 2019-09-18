# -*- mode: python ; coding: utf-8 -*-


import sys
sys.path.append('../')
sys.path.append('../Il2cppSpy/ui/')


# https://github.com/pyinstaller/pyinstaller/issues/4064
import distutils
if distutils.distutils_path.endswith('__init__.py'):
    distutils.distutils_path = os.path.dirname(distutils.distutils_path)


block_cipher = None


import capstone


site_package_path = '/'.join(capstone.__file__.split('/')[:-2])
a = Analysis(['../Il2cppSpy/application.py'],
             pathex=['../Il2cppSpy/scripts'],
             binaries=[],
             datas=[('../Il2cppSpy/bin/Release/config.json', './bin/Release'), ('../Il2cppSpy/bin/Release/DumpWrapper.dll', './bin/Release'), ('../Il2cppSpy/bin/Release/Il2cppDumper.exe', './bin/Release'), ('../Il2cppSpy/bin/Release/Mono.Cecil.dll', './bin/Release'), (capstone._cs._name, '.'), (f'{site_package_path}/Python.Runtime.dll', '.')],
             hiddenimports=['clr'],
             hookspath=[],
             runtime_hooks=[],
             excludes=[],
             win_no_prefer_redirects=False,
             win_private_assemblies=False,
             cipher=block_cipher,
             noarchive=False)
pyz = PYZ(a.pure, a.zipped_data,
             cipher=block_cipher)
exe = EXE(pyz,
          a.scripts,
          [],
          exclude_binaries=True,
          name='Il2cppSpy',
          debug=False,
          bootloader_ignore_signals=False,
          strip=False,
          upx=True,
          console=False )
coll = COLLECT(exe,
               a.binaries,
               a.zipfiles,
               a.datas,
               strip=False,
               upx=True,
               upx_exclude=[],
               name='Il2cppSpy')
app = BUNDLE(coll,
             name='Il2cppSpy.app',
             icon=None,
             bundle_identifier=None)
