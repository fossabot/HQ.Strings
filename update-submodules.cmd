@ECHO OFF

REM use .gitconfig for this repository
git config --local include.path /.gitconfig

REM update all submodules to use tips of master branches
git submodule foreach git pull origin master

REM recursively update the tips of master branches
git submodule update --recursive --remote