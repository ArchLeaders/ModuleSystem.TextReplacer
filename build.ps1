$vesion = Read-Host -Prompt "Version"
$platforms = @("linux", "win", "osx")
$architectures = @("x64", "arm64")

foreach ($platform in $platforms){
    foreach ($architecture in $architectures){
        $runtime = $platform + "-" + $architecture;
        dotnet publish -o "bin/build/$runtime" -c Release -r $runtime --no-self-contained /p:PublishSingleFile=true /p:Version=$vesion
        
        if ($platform -eq "win") {
            Move-Item -Path "bin/build/$runtime/TextReplacer.exe" -Destination "bin/build/TextReplacer-$runtime.exe"
        }
        else {
            Move-Item -Path "bin/build/$runtime/TextReplacer" -Destination "bin/build/TextReplacer-$runtime"
        }

    }
}