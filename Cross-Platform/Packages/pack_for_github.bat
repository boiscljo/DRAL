@echo off
set /p release=Release ID:
mkdir github-%release%
tar -czvf github-%release%/DRAL-%release%-portable-core3.tar.gz core3
tar -czvf github-%release%/DRAL-%release%-win64.tar.gz win64
tar -czvf github-%release%/DRAL-%release%-linux64.tar.gz linux64