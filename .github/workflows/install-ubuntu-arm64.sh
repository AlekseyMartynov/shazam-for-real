CODENAME=$(lsb_release -cs)

if [ $(uname -m) != "x86_64" ] || [ -z "$CODENAME" ]; then
    return 1
fi

sed -i 's|^deb |deb [arch=amd64] |g' /etc/apt/sources.list

cat >> /etc/apt/sources.list <<EOF
deb [arch=arm64] http://ports.ubuntu.com/ubuntu-ports/ $CODENAME main restricted
deb [arch=arm64] http://ports.ubuntu.com/ubuntu-ports/ $CODENAME-updates main restricted
EOF

dpkg --add-architecture arm64

apt-get update
apt-get install -y llvm zlib1g-dev:arm64 binutils-aarch64-linux-gnu gcc-aarch64-linux-gnu
