set /p SVNVERSION= < CurrentSVNVersion

.\UpdateBuilder\bin\Debug\UpdateBuilder.exe ".\WpfApplication2\bin\x86\Release" ".\UpdateBuilder\bin\Debug\filelist.lst" "%SVNVERSION%" "http://shahab.ite.tul.cz/NanoTransUpdate/Definitions.xml"