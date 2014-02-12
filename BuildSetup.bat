echo on
cd "C:\Users\Pan Filuta\Documents\NanoTrans\Work"
set OLDDIR=%CD%
cd C:\Windows\Microsoft.NET\Framework\v4.0.30319\

msbuild.exe "%OLDDIR%\NanoTrans.sln" /t:Clean /p:configuration=Release /p:platform=x86
msbuild.exe "%OLDDIR%\NanoTrans.sln" /p:Optimize=true /t:Rebuild /p:configuration=Release /p:platform=x86

cd "%OLDDIR%"
Setup\ISTool\ISTool.exe -compile "%OLDDIR%\Setup\ReleaseSetupScript.iss"
copy Setup\Output\NanoTransSetup.exe WpfApplication2\bin\x86\Setup\Release\NanoTransSetup.exe