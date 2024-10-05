CODENAME=$(lsb_release -cs)

if [ $(uname -m) != "x86_64" ] || [ -z "$CODENAME" ]; then
    return 1
fi

cp -f .github/workflows/install-ubuntu-arm64.sources /etc/apt/sources.list.d/ubuntu.sources
sed -i "s|CODENAME|$CODENAME|g" /etc/apt/sources.list.d/ubuntu.sources

dpkg --add-architecture arm64

apt-get update
apt-get install -y llvm zlib1g-dev:arm64 binutils-aarch64-linux-gnu gcc-aarch64-linux-gnu

apt-get install qemu-user
