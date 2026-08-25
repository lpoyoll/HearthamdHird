# Upstreams and provenance

Hearth & Hird is developed from two public Valheim mod codebases. They are not
combined as GitHub forks: VikingSettlements is the source-history foundation;
Kuku's Village is a separately licensed reference upstream.

## VikingSettlements

- Repository: <https://github.com/abjumb/VikingSettlements>
- Role: primary code and history foundation
- Foundation commit: `839fc0e9b2c84215521d6acce37a91af89563993`
- Licence: MIT
- Copyright notice: Copyright (c) 2026 abjumb

The root `LICENSE` is the VikingSettlements MIT licence and remains applicable
to the derivative code.

## Kuku's Village

- Repository: <https://github.com/kuchuk-borom-debbarma/KukusVillagerModV2_Valheim>
- Role: design and implementation reference for beds, work posts, defence posts,
  recruitment and villager job flow
- Reference commit: `731752b986d12efd27ec9c34c6ef14f757bbee61`
- Licence: Do What The Fuck You Want To Public License, version 2

No Kuku source has been copied in the human-settler-foundation change. If code
is ported later, the commit, original file and material changes must be recorded
in `THIRD_PARTY_NOTICES.md` in the same commit.

## Suggested local remotes

```bash
git remote add upstream-viking https://github.com/abjumb/VikingSettlements.git
git remote add upstream-kuku https://github.com/kuchuk-borom-debbarma/KukusVillagerModV2_Valheim.git
git fetch upstream-viking
git fetch upstream-kuku
```

These remotes are for review and selective ports. Do not merge either upstream
wholesale into a feature branch.

