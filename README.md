# Exit Warp Limiter

A lightweight server-side utility mod for **Vintage Story (.NET 10)**.

This mod filters the destination picking routine of static translocators. It guarantees that a translocator exit will never attempt to generate inside ungenerated chunks, keeping the server completely clear of unexpected background world generation spikes.

### IMPORTANT
Since a huge number of attempts could land outside of already generated land, and each tick will check at most 1 attempt, the resulting translocator activation can take several minutes!

### Mechanics
It patches the translocator's lookup ticker. If the randomly selected target coordinates fall within a chunk column that does not yet exist in the server database, the execution path is halted before `PeekChunkColumn` can trigger terrain generation. The translocator is safely instructed to roll a new set of destination coordinates on the following game tick. 

Long-distance teleports remain completely functional, provided the destination exit lands within previously explored and generated chunks.

### How To Use
- [see mod page](https://mods.vintagestory.at/exitwarplimiter)

## Contribution & Development

Want to contribute code or compile this mod locally? Please review the central [Contributing Guidelines](https://github.com/Furio-s-Mods/.github/blob/main/CONTRIBUTING.md) for environment setup and path management instructions.

## Acknowledgements
* **[Anego Studios](https://anegostudios.com)** - Vintage Story Devs
* **AlteOgre** for the original idea for the [Ilu Ambar Server](https://www.vintagestory.at/forums/topic/15864-1222euen-ilu-ambar-quenta-y%C3%A1ra-whitelisted-coop-build-pve-focussed-modded-light-rp-new-player-friendly-difficulty-progression-unique/)


## License

[MIT License](LICENSE)

---
