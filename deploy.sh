#!/bin/sh

set -e

VERSION=$1

if [ -z "$1" ]; then
    echo "${RED}Usage: ./deploy.sh VERSION${NC}"
    exit 0
fi

rm -rf ./bin/stage/
rm -rf ./bin/nupkgs/${VERSION}

dotnet publish ./Neon.NetRpc.sln -c Release -o ./bin/stage/Neon.NetRpc -p:Version=${VERSION}
dotnet pack ./Neon.NetRpc.sln -p:Version=${VERSION} --output ./bin/nupkgs/${VERSION}

rm -f ./bin/releases/Neon.NetRpc.${VERSION}.tar.gz
rm -f ./bin/releases/Neon.NetRpc.${VERSION}.zip
mkdir -p ./bin/releases

tar -zcvf ./bin/releases/Neon.NetRpc.${VERSION}.tar.gz ./bin/stage/Neon.NetRpc
zip -r ./bin/releases/Neon.NetRpc.${VERSION}.zip ./bin/stage/Neon.NetRpc

rm -rf ./bin/stage/