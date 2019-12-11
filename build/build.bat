@call setdate
@set version=%_yyyy%.%_mm%%_dd%.%_hour%%_minute%.%_second%
@dotnet pack P:\Atma\Atma.Framework\src -o P:\packages -c Release --include-symbols --include-source /property:Version=%version%
@dotnet pack P:\Atma\Atma.Math\source -o P:\packages -c Release --include-symbols --include-source /property:Version=%version%

@echo ^<package^>  > Package.nuspec
@echo  ^<metadata^> >> Package.nuspec
@echo    ^<id^>Atma.Framework^</id^> >> Package.nuspec
@echo    ^<version^>%version%^</version^> >> Package.nuspec
@echo    ^<authors^>xposure^</authors^> >> Package.nuspec
@echo    ^<owners^>xposure^</owners^> >> Package.nuspec
@echo    ^<requireLicenseAcceptance^>false^</requireLicenseAcceptance^> >> Package.nuspec
@echo    ^<description^>Meta package of all the core components Atma framework.^</description^> >> Package.nuspec
@echo    ^<copyright^>Copyright 2019^</copyright^> >> Package.nuspec
@echo    ^<dependencies^> >> Package.nuspec
@echo      ^<dependency id="Atma.Common" version="%version%" /^> >> Package.nuspec
@echo      ^<dependency id="Atma.Memory" version="%version%" /^> >> Package.nuspec
@echo      ^<dependency id="Atma.Entities" version="%version%" /^> >> Package.nuspec
@echo      ^<dependency id="Atma.Systems" version="%version%" /^> >> Package.nuspec
@echo      ^<dependency id="Atma.Math" version="%version%" /^> >> Package.nuspec
@echo    ^</dependencies^> >> Package.nuspec
@echo  ^</metadata^> >> Package.nuspec
@echo ^</package^> >> Package.nuspec

@nuget pack Package.nuspec -OutputDirectory P:\packages -Symbols

@dotnet add ..\Game.Engine package Atma.Framework --source P:\Packages
@dotnet add ..\Game.Logic package Atma.Framework --source P:\Packages