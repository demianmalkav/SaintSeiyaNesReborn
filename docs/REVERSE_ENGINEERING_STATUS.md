# Estado global de ingeniería inversa

> Documento de orientación global, no operativo. El único `NEXT` autoritativo vive en `docs/PROJECT_STATE.md`.

## Mapa vigente

| Subsistema | Madurez | Estado resumido |
|---|---|---|
| ROM / boot / MMC1 / bancos | HIGH | Target, vectores, mapper, PRG mapping y contratos de interrupción establecidos. |
| Máquina global `$00/$01` | **HIGH / CLOSED** | Namespace completo: 59 valores producidos, 197 sin productor. |
| Front-end/title/password | **HIGH / CLOSED** | Modal, attract, codec y overlays ejecutables cerrados. |
| Plataforma / exits / reload / narrativa | **HIGH / CLOSED** | Main path, exits, `$70-$89`, `$8F` y terminal final cerrados. |
| Plataforma: main-thread frame / late objects | **HIGH / CLOSED** | Producers, player, auxiliares, primary A/B y late objects compuestos. |
| Boss primitives / stage contexts | **HIGH / CLOSED** | Recursos, daño, dodge, técnicas y contextos materiales `$00-$0A`; `$0B` estructural. |
| Platform maps / CHR / metasprites | **HIGH / CLOSED** | Kits `$00-$11`, routing CHR estático/dinámico y visual resources cerrados. |
| Platform / global presentation | **HIGH / CLOSED-SEMANTIC** | State-$20 NMI, palette/CHR, HUD y coverage NMI global con cero gaps materiales. |
| RNG / pseudo-random source `$065F/$0660` | **HIGH / CLOSED** | PR #163: `$E0AC`, bank-sensitive `$94F0,X`, seeds/resets y consumer masks/parity cerrados. |
| Audio scheduler `$DB9C/$DBB6/$0440+` | **HIGH / CLOSED** | PR #165: ocho slots, cue loader/preemption, `$04F0/$04EF`, `$8B50+` arbitration y ownership APU cerrados. |
| Texto/localización | **MEDIUM / ACTIVE** | Motor/corpus de 251 mensajes cerrado estáticamente; falta contrato runtime JP/ES con catálogo privado externo. |
| Auditoría integral ORIGINAL SPEC | PENDING | Después de cerrar runtime text/content integration. |
| REBORN | EARLY | Congelado hasta cierre integral de ORIGINAL SPEC. |

## Último checkpoint técnico — PR #165

La arquitectura del scheduler de audio queda cerrada sin incorporar payloads originales de música/SFX.

```text
reset               $DB9C -> $4015/$04F0/$04EF=0; 8 slots +0=FF
records              $0440 + $15*N, N=0..7
cue loader           $DBB6 -> $DC0E + 4*A
channel selector     slot[+1] & 3
channel bits         01,02,04,08
clear masks          0E,0D,0B,07
frame scheduler      bank0 $8B50+
frame arbitration    first active slot per channel wins
4015 shadow          $04F0
visual request latch $04EF via stream command A5 -> $8904/$8AF1
```

Scheduler-owned APU routing:

```text
DB9E                 $4015 reset
8DBC / 8E06          $4015 enable/disable shadow writes
8E4D / 8E9E          $4000 + channel base
8DCD                 $4001 + channel base
8DD1 / 8F0E          $4002 + channel base
8DEA                 $4003 + channel base
channel bases         0,4,8,12
```

Verification before state update:

```text
PR    #165
head  10482ab54cefaca61adbeaf14102176a3e87f5b6
CI    Original Spec #624 SUCCESS
      ORIGINAL SPEC tests #420 SUCCESS
```

Artifacts: `CanonicalAudioScheduler.cs`, self-test fixtures, `audit_canonical_audio_scheduler.py`, and `CANONICAL_AUDIO_SCHEDULER_DB9C_DBB6.md`. Original note streams, envelopes, instrument data, music and SFX payloads are not committed.

## Frontera operativa actual

La siguiente frontera global es **runtime text/content integration**.

Evidence already frozen for entry:

```text
message IDs              0..250 (251 total)
entrypoints               E7B3/E7B7 -> 066A
                          E7C3/E7C7 -> 066B
variant byte              0672
pointer table             bank6 A47B
message storage           CHR4K 15 || CHR4K 17
terminator/control        FF / 01 / A4 / 3B / 3C
```

`TEXT_ENGINE.md` and `extract_japanese_script.py` already close physical extraction/codec behavior. `LOCALIZATION.md` fixes immutable public IDs `MSG_000..MSG_250`; full JP/ES content stays private. The active work is therefore the runtime-facing request/catalog contract, not re-extraction and not full soundtrack content.

## No reabrir sin evidencia nueva

- global `$00/$01` reachability;
- global NMI presentation — #161;
- battle/event coverage — #149 and stage PRs;
- platform main-thread / exits / narrative;
- maps / CHR / visual definitions — #151/#156;
- state-$20 NMI — #154;
- HUD `$9D69-$9EED` — #159;
- canonical RNG `$E0AC/$065F/$0660` — #163;
- audio scheduler `$DB9C/$DBB6/$0440+` — #165;
- promoted boss damage/resources/dodge/technique boundaries.

## Áreas abiertas

1. runtime text/content integration con catálogo privado JP/ES;
2. auditoría integral final de ORIGINAL SPEC;
3. full audio content queda opcional/separado y no bloquea la arquitectura ya cerrada salvo que la auditoría detecte un requisito material;
4. REBORN sólo después del cierre integral.
