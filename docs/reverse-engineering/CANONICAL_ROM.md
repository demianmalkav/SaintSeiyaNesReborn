# Canonical ROM

Target: `Saint Seiya - Ougon Densetsu Kanketsu Hen (Japan).nes`

## iNES geometry

- File size: 262,160 bytes
- Header: 16 bytes (`4E 45 53 1A 08 10 10 00 00 00 00 00 00 00 00 00`)
- PRG ROM: 128 KiB = 8 × 16 KiB
- CHR ROM: 128 KiB = 16 × 8 KiB
- Mapper: 1 / MMC1
- Trainer: no
- Battery-backed RAM: no
- Header mirroring bit: horizontal (runtime mirroring is mapper-controlled)

## Hashes

Complete iNES file:

- SHA-1: `F871D9B3DAFDDCDAD5F2ACD71044292E5169064E`
- MD5: `3B0F17C2B6EFC928B3D3FE9B1A389680`
- SHA-256: `6917B31D7343A9A17170E833BACDBC3B1EBA3E02D11C51C0D44DBE436C9AD43A`
- CRC32: `F8D258A3`

Payload with the 16-byte iNES header removed:

- SHA-1: `985A81E4018297A94249DBEB436C083CFC179AD9`
- MD5: `2C85DEDEFC8DD7B49B80774DE9793506`
- CRC32: `9561798D`

Component CRC32:

- PRG: `BB375ED2`
- CHR: `311FB99F`

The complete-file SHA-1 and MD5 match the known-good Japanese revision documented by TASVideos. The raw ROM is not stored in this repository.
