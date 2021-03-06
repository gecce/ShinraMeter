@echo off
msbuild Tera.sln /p:Configuration=Debug /p:Platform="Any CPU"
set output=.\ShinraMeterV
set source=.
set variant=Debug
rmdir /Q /S "%output%"
md "%output%
md "%output%\resources"
md "%output%\resources\config"

copy "%source%\DamageMeter.Sniffing\bin\%variant%\*" "%output%\"
copy "%source%\DamageMeter.UI\bin\%variant%\*" "%output%\"
copy "%source%\Randomizer\bin\%variant%\Randomizer.exe" "%output%\"
copy "%source%\Randomizer\bin\%variant%\Randomizer.exe.config" "%output%\"
copy "%source%\ReadmeUser.txt" "%output%\readme.txt"
copy "%source%\add_firewall_exception.bat" "%output%\add_firewall_exception.bat"
xcopy "%source%\resources" "%output%\resources\" /E
del "%output%\*.xml"
del "%output%\*.vshost*"
del "%output%\resources\logs\*"
del "%output%\resources\config\*.xml"