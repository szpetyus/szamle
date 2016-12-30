cd bin
"c:\Program Files\7-Zip\7z.exe" a ..\publish\szamle-x64.zip x64\Release\* -xr!*vshost* -xr!*.pdb
"c:\Program Files\7-Zip\7z.exe" a ..\publish\szamle-x86.zip x86\Release\* -xr!*vshost* -xr!*.pdb
cd ..
